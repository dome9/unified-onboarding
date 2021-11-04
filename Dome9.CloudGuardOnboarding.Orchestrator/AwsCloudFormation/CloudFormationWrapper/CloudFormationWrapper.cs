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
    public class CloudFormationWrapper : ICloudFormationWrapper, IDisposable
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
            Action<string> statusUpdate,
            int executionTimeoutMinutes)
        {           

            int statusPollCount = 0;

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
                StackSummary stackSummary = null;
                var response = await _client.CreateStackAsync(request);
                do
                {
                    stackSummary = await GetStackSummaryAsync(feature, stackName);
                    if (!stackSummary.StackStatus.IsFinal())
                    {
                        Console.WriteLine($"[INFO] Waiting {STATUS_POLLING_INTERVAL_MILLISECONDS}ms to poll stack status again, {stackSummary.ToDetailedString()}");
                        statusUpdate($"{stackSummary.StackStatus}");
                        await Task.Delay(STATUS_POLLING_INTERVAL_MILLISECONDS);
                    }

                    if(STATUS_POLLING_INTERVAL_MILLISECONDS * ++statusPollCount > executionTimeoutMinutes * 60 * 1000)
                    {
                        Console.WriteLine("[WARNING] Execution timeout exceeded");
                        break;
                    }
                }
                while (stackSummary == null || !stackSummary.StackStatus.IsFinal());
                
                if(stackSummary == null || !stackSummary.StackStatus.IsFinal() || stackSummary.StackStatus.IsError())
                {
                    throw new Exception(stackSummary?.ToDetailedString() ?? "Unable to get stack summary");
                }

                Console.WriteLine($"[INFO] Success, {stackSummary.ToDetailedString()}. StackTemplateS3Url:'{stackTemplateS3Url}'");
                return response.StackId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(CreateStackAsync)}] Failed to create stack. StackName:'{stackName}', StackTemplateS3Url:'{stackTemplateS3Url}', Error={ex}");
                throw new OnboardingException(ex.Message, feature);
            }
        }

        public async Task<string> UpdateStackAsync(
            Enums.Feature feature, 
            string stackTemplateS3Url, 
            string stackName,
            List<string> capabilities, 
            Dictionary<string, string> parameters)
        {
            var requestParameters = parameters.Select(p => new Parameter
            {
                ParameterKey = p.Key,
                ParameterValue = p.Value
            }).ToList();
            
            var request = new UpdateStackRequest
            {
                StackName = stackName,
                TemplateURL = stackTemplateS3Url,
                Parameters = requestParameters,
                Capabilities = capabilities,
            };
            
            try
            {
                var response = await _client.UpdateStackAsync(request);
                return response.StackId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(UpdateStackAsync)}] Failed to update stack '{stackName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature);
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
        
        public async Task<StackSummary> GetStackSummaryAsync(Enums.Feature feature, string stackName)
        {
            try
            {
                var nextToken = "";
                StackSummary stack = null;
                while (stack == null && nextToken != null)
                {
                    var response = await _client.ListStacksAsync();
                    stack = response.StackSummaries.FirstOrDefault(s => s.StackName == stackName);
                    nextToken = response.NextToken;
                }

                return stack;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetStackSummaryAsync)}] Failed to get StackSummary for stack '{stackName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature); ;
            }
        }

        public async Task DeleteStackAsync(Enums.Feature feature, string stackName)
        {
            var request = new DeleteStackRequest
            {
                StackName = stackName,
            };
            
            try
            {
                await _client.DeleteStackAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete stack '{stackName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature); ;
            }
        }

        #region User Based Secrets

        public async Task<ApiCredentials> GetCredentialsFromSecretsManager()
        {
            const string tagKey = "aws:cloudformation:logical-id";
            const string tagValue = "CrossAccountUserCredentialsStored";
            const string accessKeyId = "ACCESS_KEY";
            const string accessKeySecret = "SECRET_KEY";

            try
            {
                var secrets = await SecretsManagerListSecrets();
                if (!(secrets.Any()))
                {
                    throw new OnboardingException("No secrets found in secrets manager", Enums.Feature.ContinuousCompliance);
                }
         
                var secretEntry = secrets.FirstOrDefault(s => s.Tags.Any(t => t.Key.Equals(tagKey) && t.Value.Equals(tagValue)));

                var secretString = await SecretsManagerGetSecretValue(secretEntry?.ARN);
                if (string.IsNullOrWhiteSpace(secretString))
                {
                    throw new OnboardingException($"Secret not found in secrets manager for secret entry with ARN '{secretEntry?.ARN}'", Enums.Feature.ContinuousCompliance);
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
                throw new OnboardingException(ex.Message, Enums.Feature.ContinuousCompliance);
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
            var response = await client.GetSecretValueAsync(new GetSecretValueRequest{ SecretId = secretId });

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