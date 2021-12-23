using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    /// <summary>
    /// Singleton that executes operations on an AWS CloudFormation client
    /// </summary>
    public class CloudFormationWrapper : ICloudFormationWrapper
    {
        private readonly AmazonCloudFormationClient _client;
        private static ICloudFormationWrapper _cfnWrapper = null;
        private static readonly object _instanceLock = new object();
        private const int STATUS_POLLING_INTERVAL_MILLISECONDS = 500;
        private bool _disposed = false;

        private CloudFormationWrapper()
        {
            _client = new AmazonCloudFormationClient();
        }

        public static ICloudFormationWrapper Get()
        {
            lock (_instanceLock)
            {
                return _cfnWrapper ??= new CloudFormationWrapper();
            }
        }

        public async Task<string> CreateStackAsync(
            Enums.Feature feature,
            string stackTemplateS3Url,
            string stackName,
            List<string> capabilities,
            Dictionary<string, string> parameters,
            Action<string, string> statusUpdate,
            int executionTimeoutMinutes)
        {

            var requestParameters = parameters?.Select(p => new Parameter
            {
                ParameterKey = p.Key,
                ParameterValue = p.Value
            }).ToList();

            var request = new CreateStackRequest
            {
                StackName = stackName,
                TemplateURL = stackTemplateS3Url,
                TimeoutInMinutes = executionTimeoutMinutes,
                Parameters = requestParameters,
                Capabilities = capabilities
            };

            try
            {
                if (await IsStackExist(feature, stackName))
                {
                    throw new Exception($"Stack {stackName} alredy exist");
                }

                var response = await _client.CreateStackAsync(request);
                if (response?.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Stack stackSummary = await PollUntilStackStatusFinal(feature, response.StackId, statusUpdate, executionTimeoutMinutes);

                    if (stackSummary == null || !stackSummary.StackStatus.IsFinal() || stackSummary.StackStatus.IsError())
                    {
                        throw new Exception($"Invalid stack status: {stackSummary?.ToDetailedString() ?? $"Unable to get stack summary. Feature='{feature}' StackName='{stackName}', StackTemplateS3Url='{stackTemplateS3Url}'."}");
                    }

                    Console.WriteLine($"[INFO] [{nameof(CreateStackAsync)}] Success in creating stack. StackSummary=[{stackSummary.ToDetailedString()}]. StackTemplateS3Url='{stackTemplateS3Url}'.");
                    return response.StackId;
                }
                else
                {
                    throw new Exception($"Failed to create stack. Feature='{feature}', StackName='{stackName}', StackTemplateS3Url='{stackTemplateS3Url}',  HttpStatusCode='{response.HttpStatusCode}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(CreateStackAsync)}] {ex}");
                throw new OnboardingException(ex.Message, feature);
            }
        }

        public async Task UpdateStackAsync(
            Enums.Feature feature,
            string stackTemplateS3Url,
            string stackName,
            List<string> capabilities,
            Dictionary<string, string> parameters,
            int executionTimeoutMinutes)
        {
            Console.WriteLine($"[INFO] [{nameof(UpdateStackAsync)}] starting with params {nameof(feature)}='{feature}'" +
                $", {nameof(stackTemplateS3Url)}='{stackTemplateS3Url}'" +
                $", {nameof(stackName)}='{stackName}', {nameof(parameters)}='{string.Join(", ", parameters ?? new Dictionary<string, string>())}'" +
                $", {nameof(capabilities)}='{string.Join(", ", capabilities ?? new List<string>())}'" +
                $", {nameof(executionTimeoutMinutes)}='{executionTimeoutMinutes}'");

            try
            {
                if (!await IsStackExist(feature, stackName))
                {
                    Console.WriteLine($"[INFO] [{nameof(UpdateStackAsync)}] stack not found, probably stack not exist, will skip stack update. Feature={feature}, StackName={stackName}.");
                }

                var changeSetId = await CreateChangeSet(feature, stackTemplateS3Url, stackName, capabilities, parameters);

                var changesetCreatedState = await PollUntilChangeSetStatusFinal(feature, changeSetId, (s) => Console.WriteLine(s), executionTimeoutMinutes);

                if (changesetCreatedState.ChangeSetStatus != null && changesetCreatedState.ChangeSetStatus == "FAILED")
                {
                    const string NO_CHANGES_STATUS_REASON = "The submitted information didn't contain changes. Submit different information to create a change set.";
                    if (changesetCreatedState.StatusReason == NO_CHANGES_STATUS_REASON)
                    {
                        Console.WriteLine($"[INFO] [{nameof(UpdateStackAsync)}] No changes detected, nothing to be applied.");
                        await DeleteChangeSet(stackName, changeSetId);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] [{nameof(UpdateStackAsync)}] Failed to create change set for stack '{stackName}', Reason={changesetCreatedState.StatusReason}");
                        throw new OnboardingException($"Failed to create change set for stack '{stackName}'", feature);
                    }
                }

                if (changesetCreatedState.ChangeSetStatus == null || !changesetCreatedState.ChangeSetStatus.IsFinal() || !changesetCreatedState.ChangeSetStatus.IsSuccess())
                {
                    Console.WriteLine($"[ERROR] [{nameof(UpdateStackAsync)}] Failed to create change set for stack '{stackName}', ChangeSetStatus={changesetCreatedState.ChangeSetStatus}, Reason={changesetCreatedState.StatusReason}");
                    throw new OnboardingException($"Failed to create change set for stack '{stackName}'", feature);
                }

                Console.WriteLine($"[INFO] [{nameof(UpdateStackAsync)}] Success in creating changeset. Feature={feature}, {nameof(ChangeSetStatus)}='{changesetCreatedState.ChangeSetStatus}' Will proceed to when ready to execute.");


                // wait until ok to start execution
                var changesetReadyState = await PollUntilExecutionStatusFinal(feature, changeSetId, (s) => Console.WriteLine(s), executionTimeoutMinutes);

                if (changesetReadyState == null || !changesetReadyState.ExecutionStatus.IsReady())
                {
                    Console.WriteLine($"[ERROR] [{nameof(IsStackExist)}] Can not excute ChangeSet. Feature={feature}, StackName={stackName}, ChangeSetId={changeSetId}, ChangeSetStatus={changesetReadyState.ChangeSetStatus}, ChangeSetExecutionStatus={changesetReadyState.ExecutionStatus}");
                    throw new OnboardingException($"Can not excute ChangeSet for stack '{stackName}'", feature);
                }

                if (!changesetReadyState.HasChanges.HasValue || changesetReadyState.HasChanges == false)
                {
                    Console.WriteLine($"[INFO] [{nameof(UpdateStackAsync)}] No changes detected, nothing to be applied.");
                    return;
                }

                var changeSetExecutionStatus = await ExecuteChangeSet(feature, stackName, changeSetId, executionTimeoutMinutes);
                if (changeSetExecutionStatus == null || changeSetExecutionStatus.ExecutionStatus != "EXECUTE_COMPLETE")
                {
                    Console.WriteLine($"[ERROR] [{nameof(UpdateStackAsync)}] Failed to excute ChangeSet. Feature={feature}, StackName={stackName}, ChangeSetId={changeSetId}, ChangeSetStatus={changesetReadyState.ChangeSetStatus}, ChangeSetExecutionStatus={changesetReadyState.ExecutionStatus}");
                    throw new OnboardingException($"Failed to excute ChangeSet for stack '{stackName}'", feature);
                }
                Console.WriteLine($"[INFO] [{nameof(UpdateStackAsync)}] Success in updating stack. Feature={feature}, Stack={stackName}.");

            }
            catch (OnboardingException)
            {
                throw;
            }
            catch (Exception ex)
            
            {
                Console.WriteLine($"[ERROR] [{nameof(UpdateStackAsync)}] Failed to update stack '{stackName}'. Error={ex}");
                throw new OnboardingUpdateStackException($"Failed to update stack '{stackName}'", stackName, feature);
            }
        }

        public async Task<string> GetStackTemplateAsync(Enums.Feature feature, string stackName)
        {
            var request = new GetTemplateRequest
            {
                StackName = stackName,
            };

            try
            {
                var response = await _client.GetTemplateAsync(request);
                return response.TemplateBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetStackTemplateAsync)}] Failed to get stack template for stack '{stackName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature);
            }
        }

        public async Task<bool> IsStackExist(Enums.Feature feature, string stackName)
        {
            try
            {
                var nextToken = "";
                while (nextToken != null)
                {
                    var listRequest = new ListStacksRequest
                    {
                        NextToken = string.IsNullOrWhiteSpace(nextToken) ? null : nextToken
                    };

                    var response = await _client.ListStacksAsync(listRequest);

                    var stack = response.StackSummaries.FirstOrDefault(s => s.StackName == stackName && s.StackStatus != "DELETE_COMPLETE");
                    if (stack != null)
                    {
                        return true;
                    }

                    nextToken = response.NextToken;
                }
                return false;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(IsStackExist)}] Failed to check if stach '{stackName}' exist. Error={ex}");
                throw new OnboardingException($"Failed to check if stack '{stackName}' exist", feature); ;
            }
        }

        public async Task<CloudGuardChangeSetStatus> GetChangeSetStatusAsync(Enums.Feature feature, string changeSetName)
        {
            try
            {
                bool? hasChanges = null;
                string reason = null;
                string executionStatus = null;
                var nextToken = "";
                ChangeSetStatus status = null;
                while (status == null && nextToken != null)
                {
                    var describeChangeSetRequest = new DescribeChangeSetRequest
                    {
                        ChangeSetName = changeSetName,
                        NextToken = string.IsNullOrWhiteSpace(nextToken) ? null : nextToken,
                    };

                    var response = await _client.DescribeChangeSetAsync(describeChangeSetRequest);

                    status = response.Status;
                    reason = response.StatusReason;
                    hasChanges = response.Changes?.Any();
                    executionStatus = response.ExecutionStatus;

                    nextToken = response.NextToken;
                }

                return new CloudGuardChangeSetStatus { ChangeSetStatus = status, HasChanges = hasChanges, StatusReason = reason, ExecutionStatus = executionStatus };

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetChangeSetStatusAsync)}] Failed to get changset description for changeset '{changeSetName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature); ;
            }
        }

        public async Task DeleteStackAsync(Enums.Feature feature, string stackName, Action<string, string> statusUpdate, int executionTimeoutMinutes)
        {
            var request = new DeleteStackRequest
            {
                StackName = stackName,
            };

            try
            {
                if (!await IsStackExist(feature, stackName))
                {
                    Console.WriteLine($"[INFO] [{nameof(DeleteStackAsync)}] stack not found, probably stack not exist, will skip stack deletion. Feature={feature}, StackName={stackName}.");
                    return;
                }

                var response = await _client.DeleteStackAsync(request);
                if (response?.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    await PollUntilStackIsDeleted(feature, stackName, statusUpdate, executionTimeoutMinutes);
                    Console.WriteLine($"[INFO] [{nameof(DeleteStackAsync)}] Success in deleting stack. Feature={feature}, StackName={stackName}.");
                }
                else
                {
                    throw new Exception($"Failed to delete stack. Feature={feature}, Stackname='{stackName}' HttpStatusCode='{response?.HttpStatusCode}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {nameof(DeleteStackAsync)} {ex}");
                throw new OnboardingDeleteStackException($"Failed to delete stack '{stackName}'", stackName, feature);
            }
        }

        public async Task<Stack> GetStackDescriptionAsync(Enums.Feature feature, string stackName, bool filterDeleted = true)
        {
            try
            {
                var nextToken = "";
                Stack stackDesc = null;
                while (stackDesc == null && nextToken != null)
                {
                    var describeTasksRequest = new DescribeStacksRequest
                    {
                        StackName = stackName,
                        NextToken = string.IsNullOrWhiteSpace(nextToken) ? null : nextToken
                    };

                    var response = await _client.DescribeStacksAsync(describeTasksRequest);

                    // find the first stack not deleted
                    stackDesc = response.Stacks.FirstOrDefault(s => filterDeleted ? s.StackStatus != "DELETE_COMPLETE" : true);
                    Console.WriteLine($"[DEBUG] [{nameof(GetStackDescriptionAsync)}] got {response.Stacks?.Count ?? 0} stacks from DescribeStacksAsync query, first stack with {nameof(stackDesc.StackName)}='{stackDesc?.StackName}', {nameof(stackDesc.StackId)}='{stackDesc?.StackId}', {nameof(nextToken)}='{nextToken}'");
                    nextToken = response.NextToken;
                }

                return stackDesc;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetStackDescriptionAsync)}] Failed to get stack description for stack '{stackName}'. Error={ex}");
                throw new OnboardingException($"Failed to get stack description for stack '{stackName}'", feature); ;
            }
        }



        private async Task<string> CreateChangeSet(Enums.Feature feature, string stackTemplateS3Url, string stackName, List<string> capabilities, Dictionary<string, string> parameters)
        {
            try
            {
                var requestParameters = parameters.Select(p => new Parameter
                {
                    ParameterKey = p.Key,
                    ParameterValue = p.Value
                }).ToList();

                var changeSetRequest = new CreateChangeSetRequest()
                {
                    StackName = stackName,
                    ChangeSetName = $"CloudGuard-Update-{Guid.NewGuid()}",
                    TemplateURL = stackTemplateS3Url,
                    Parameters = requestParameters,
                    Capabilities = capabilities,
                };

                var changeSetCreateResponse = await _client.CreateChangeSetAsync(changeSetRequest);

                if (changeSetCreateResponse?.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Failed to create changeset for stack. Feature={feature}, Stackname='{stackName}' HttpStatusCode='{changeSetCreateResponse?.HttpStatusCode}'.");

                }
                return changeSetCreateResponse.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(IsStackExist)}] Failed to create change set for stack '{stackName}', stackTemplateS3Url={stackTemplateS3Url}. Error={ex}");
                throw new OnboardingException($"Failed to create change set for stack '{stackName}'", feature);
            }
        }

        private async Task DeleteChangeSet(string stackName, string changeSetId)
        {
            try
            {
                var deleteChangeSetResponse = await _client.DeleteChangeSetAsync(
                    new DeleteChangeSetRequest()
                    {
                        ChangeSetName = changeSetId,
                        StackName = stackName
                    });
            }
            catch (Exception)
            {
                Console.WriteLine($"[WARN] could not delete changeset with ChangeSet Id='{changeSetId}', StackName='{stackName}'");
            }
        }
        
        private async Task<CloudGuardChangeSetStatus> ExecuteChangeSet(Enums.Feature feature, string stackName, string changeSetId, int executionTimeoutMinutes)
        {
            var executeChangeSetRequest = new ExecuteChangeSetRequest()
            {
                ChangeSetName = changeSetId,
                StackName = stackName,
            };

            var execChangeSetResponse = await _client.ExecuteChangeSetAsync(executeChangeSetRequest);
            if (execChangeSetResponse?.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return await PollUntilExecutionStatusFinal(feature, changeSetId, (s) => Console.WriteLine(s), executionTimeoutMinutes, true);
            }
            else
            {
                throw new Exception($"Failed to update stack. Feature={feature}, Stackname='{stackName}' HttpStatusCode='{execChangeSetResponse?.HttpStatusCode}'.");
            }
        }

        private async Task<CloudGuardChangeSetStatus> PollUntilChangeSetStatusFinal(Enums.Feature feature, string stackName, Action<string> statusUpdate, int executionTimeoutMinutes)
        {
            int statusPollCount = 0;
            ChangeSetStatus changeSetStatus;
            CloudGuardChangeSetStatus changeSetInfo = new CloudGuardChangeSetStatus();

            do
            {
                changeSetInfo = await GetChangeSetStatusAsync(feature, stackName);
                changeSetStatus = changeSetInfo.ChangeSetStatus;
                if (changeSetStatus == null || !changeSetStatus.IsFinal())
                {
                    Console.WriteLine($"[INFO] Waiting {STATUS_POLLING_INTERVAL_MILLISECONDS}ms to poll change set status again, {nameof(ChangeSetStatus)}={changeSetStatus}");
                    statusUpdate($"{nameof(ChangeSetStatus)}='{changeSetStatus}'");
                    await Task.Delay(STATUS_POLLING_INTERVAL_MILLISECONDS);
                }

                if (STATUS_POLLING_INTERVAL_MILLISECONDS * ++statusPollCount > executionTimeoutMinutes * 60 * 1000)
                {
                    Console.WriteLine($"[WARN] [{nameof(PollUntilChangeSetStatusFinal)}] Execution timeout exceeded. Feature={feature}, StackName={stackName}, ExecutionTimeoutMinutes={executionTimeoutMinutes}.");
                    break;
                }
            }
            while (changeSetStatus == null || !changeSetStatus.IsFinal());

            return changeSetInfo;
        }

        private async Task<CloudGuardChangeSetStatus> PollUntilExecutionStatusFinal(Enums.Feature feature, string changeSetId, Action<string> statusUpdate, int executionTimeoutMinutes, bool checkForSuccess = false)
        {
            int statusPollCount = 0;
            ExecutionStatus executionStatus;
            CloudGuardChangeSetStatus changeSetInfo;
            var isExecutionSuccess = false;

            do
            {
                changeSetInfo = await GetChangeSetStatusAsync(feature, changeSetId);
                executionStatus = changeSetInfo.ExecutionStatus;
                isExecutionSuccess = checkForSuccess ? executionStatus.IsSuccess() : true;
                Console.WriteLine($"[INFO] isExecutionSuccess={isExecutionSuccess}, checkForSuccess={checkForSuccess}.");
                if (executionStatus == null || !executionStatus.IsFinal() || !isExecutionSuccess)
                {
                    Console.WriteLine($"[INFO] Waiting {STATUS_POLLING_INTERVAL_MILLISECONDS}ms to poll change set status again, {nameof(ExecutionStatus)}:{executionStatus}");
                    statusUpdate($"{nameof(ChangeSetStatus)}: '{executionStatus}'");
                    await Task.Delay(STATUS_POLLING_INTERVAL_MILLISECONDS);
                }

                if (STATUS_POLLING_INTERVAL_MILLISECONDS * ++statusPollCount > executionTimeoutMinutes * 60 * 1000)
                {
                    Console.WriteLine($"[WARN] [{nameof(PollUntilExecutionStatusFinal)}] Execution timeout exceeded. Feature={feature}, StackName={changeSetId}, ExecutionTimeoutMinutes={executionTimeoutMinutes}.");
                    break;
                }
            }
            while (executionStatus == null || !executionStatus.IsFinal() || !isExecutionSuccess);

            return changeSetInfo;
        }

        private async Task<Stack> PollUntilStackStatusFinal(Enums.Feature feature, string stackName, Action<string, string> statusUpdate, int executionTimeoutMinutes)
        {
            int statusPollCount = 0;
            Stack stackDesc;
            do
            {
                stackDesc = await GetStackDescriptionAsync(feature, stackName);
                if (stackDesc == null || !stackDesc.StackStatus.IsFinal())
                {
                    Console.WriteLine($"[INFO] Waiting {STATUS_POLLING_INTERVAL_MILLISECONDS}ms to poll stack status again, {stackDesc?.ToDetailedString()}");
                    statusUpdate(stackDesc.StackStatus, stackDesc.StackStatusReason);
                    await Task.Delay(STATUS_POLLING_INTERVAL_MILLISECONDS);
                }

                if (STATUS_POLLING_INTERVAL_MILLISECONDS * ++statusPollCount > executionTimeoutMinutes * 60 * 1000)
                {
                    Console.WriteLine($"[WARN] [{nameof(PollUntilStackStatusFinal)}] Execution timeout exceeded. Feature={feature}, StackName={stackName}, ExecutionTimeoutMinutes={executionTimeoutMinutes}.");
                    break;
                }
            }
            while (stackDesc == null || !stackDesc.StackStatus.IsFinal());

            // update the final status
            if(stackDesc != null)
            {
                statusUpdate(stackDesc.StackStatus, stackDesc.StackStatusReason);
            }

            return stackDesc;
        }

        private async Task PollUntilStackIsDeleted(Enums.Feature feature, string stackName, Action<string, string> statusUpdate, int executionTimeoutMinutes)
        {
            int statusPollCount = 0;
            Stack stackDesc = null;
            var stackStatusReason = "";
            try
            {
                do
                {
                    if (!await IsStackExist(feature, stackName))
                    {
                        statusUpdate("DELETE_COMPLETE", stackStatusReason);
                        return;
                    }
                    stackDesc = await GetStackDescriptionAsync(feature, stackName, false);
                    if (stackDesc == null || !stackDesc.StackStatus.IsFinal())
                    {
                        Console.WriteLine($"[INFO] [{nameof(PollUntilStackStatusFinal)}] Waiting {STATUS_POLLING_INTERVAL_MILLISECONDS}ms to poll stack status again, {stackDesc?.ToDetailedString()}");
                        stackStatusReason = stackDesc.StackStatusReason;
                        statusUpdate(stackDesc.StackStatus, stackStatusReason);
                        await Task.Delay(STATUS_POLLING_INTERVAL_MILLISECONDS);
                    }

                    if (STATUS_POLLING_INTERVAL_MILLISECONDS * ++statusPollCount > executionTimeoutMinutes * 60 * 1000)
                    {
                        Console.WriteLine($"[WARN] [{nameof(PollUntilStackStatusFinal)}] Execution timeout exceeded. Feature={feature}, StackName={stackName}, ExecutionTimeoutMinutes={executionTimeoutMinutes}.");
                        break;
                    }
                }
                while (stackDesc == null || !stackDesc.StackStatus.IsFinal());


                if (stackDesc == null || !stackDesc.StackStatus.IsFinal() || stackDesc.StackStatus.IsError())
                {
                    throw new Exception($"Invalid stack status: {stackDesc?.ToDetailedString() ?? $"Unable to get stack summary. Feature={feature}, StackName={stackName}"}.");
                }
            }
            catch (Exception ex)
            {
                if (await IsStackExist(feature, stackName))
                {
                    throw ex;
                }
            }
            statusUpdate("DELETE_COMPLETE", stackStatusReason);
        }

        #region User Based Secrets

        public async Task<ApiCredentials> GetCredentialsFromSecretsManager(string resourceName)
        {
            const string tagKey = "aws:cloudformation:logical-id";
            string tagValue = resourceName;
            const string accessKeyId = "ACCESS_KEY";
            const string accessKeySecret = "SECRET_KEY";

            try
            {
                var secrets = await SecretsManagerListSecrets();
                if (!(secrets.Any()))
                {
                    throw new OnboardingException("No secrets found in secrets manager", Enums.Feature.Permissions);
                }

                var secretEntry = secrets.FirstOrDefault(s => s.Tags.Any(t => t.Key.Equals(tagKey) && t.Value.Equals(tagValue)));

                var secretString = await SecretsManagerGetSecretValue(secretEntry?.ARN);
                if (string.IsNullOrWhiteSpace(secretString))
                {
                    throw new OnboardingException($"Secret not found in secrets manager for secret entry with ARN '{secretEntry?.ARN}'", Enums.Feature.Permissions);
                }

                var secretDict = JsonSerializer.Deserialize<Dictionary<string, string>>(secretString);
                var credentials = new ApiCredentials { ApiKeyId = secretDict[accessKeyId], ApiKeySecret = secretDict[accessKeySecret] };
                return credentials;
            }
            catch (OnboardingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetCredentialsFromSecretsManager)}] Failed. Error={ex}");
                throw new OnboardingException(ex.Message, Enums.Feature.Permissions);
            }
        }



        private async Task<List<SecretListEntry>> SecretsManagerListSecrets()
        {
            using (var client = new AmazonSecretsManagerClient())
            {
                var response = await client.ListSecretsAsync(
                    new ListSecretsRequest
                    {
                        Filters = new List<Filter>
                        {
                            new Filter
                            {
                                Key = "tag-key",
                                Values = new List<string>{ "aws:cloudformation:logical-id" }
                            }
                        }
                    });

                Console.WriteLine($"[INFO] [{nameof(SecretsManagerListSecrets)}] ResponseHttpStatusCode='{response?.HttpStatusCode}'");

                return response?.SecretList;
            }
        }

        private async Task<string> SecretsManagerGetSecretValue(string secretId)
        {
            if (string.IsNullOrWhiteSpace(secretId))
            {
                throw new ArgumentException("secretId (ARN) is null or empty");
            }

            var client = new AmazonSecretsManagerClient();
            var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId });

            return response?.SecretString;
        }

        #endregion


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}