using System;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.Steps;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class UserBasedOnboardingWorkflow : OnboardingWorkflowBase
    {
        public UserBasedOnboardingWorkflow(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base(apiProvider, retryAndBackoffService) { }

        public override async Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException($"{nameof(OnboardingRequest)} {nameof(request)} is null");
                }

                if (string.IsNullOrWhiteSpace(request.OnboardingId))
                {
                    throw new ArgumentException($"{nameof(request.OnboardingId)} is null or whitespace");
                }

                // 0. set the credentials step - will delete the service account on rollback in case of workflow error
                var initServiceAccountStep = new InitServiceAccountStep(_apiProvider, _retryAndBackoffService, request.OnboardingId, request.CloudGuardApiKeyId, request.CloudGuardApiKeySecret, request.ApiBaseUrl);
                await ExecuteStep(initServiceAccountStep);

                Console.WriteLine($"[INFO] Executing onboarding process - OnboardingId: {request?.OnboardingId}");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Enums.Feature.None, Enums.Status.PENDING, "Starting onboarding workflow", null, null, null)));

                // 1. create new service account and delete initial service account with exposed credentials (stack url containing the credentials could be passed around)               
                var replaceServiceAccountStep = new ReplaceServiceAccountStep(_apiProvider, _retryAndBackoffService, initServiceAccountStep.ServiceAccount, request.OnboardingId);
                await ExecuteStep(replaceServiceAccountStep);
                initServiceAccountStep.ServiceAccount = replaceServiceAccountStep.ServiceAccount;

                // 2.  validate onboarding id
                await ExecuteStep(new ValidateOnboardingStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));

                // 3. get configuration from API
                var configurationStep = await ExecuteStep(new GetConfigurationStep(_apiProvider, _retryAndBackoffService, request.OnboardingId, request.Version));
                var configuration = (configurationStep as GetConfigurationStep).Configuration;
                configuration.SetStackNameSuffix(request.UniqueSuffix);

                // 4. run the Permissions stack (create cross account user for CloudGuard)
                var userBasedPermissionsStep = new PermissionsUserBasedStackCreationStep(_apiProvider, _retryAndBackoffService, configuration.PermissionsStackName, request.AwsAccountRegion, request.S3BucketName, configuration.PermissionsTemplateS3Path, request.OnboardingId, request.AwsPartition, request.EnableRemoteStackModify, request.UniqueSuffix);
                await ExecuteStep(userBasedPermissionsStep);
                Console.WriteLine($"[INFO] userBasedPermissionsStep.AwsUserCredentials ApiKeyId='{userBasedPermissionsStep.AwsUserCredentials?.ApiKeyId?.MaskChars(4)}', ApiKeySecret='{userBasedPermissionsStep.AwsUserCredentials?.ApiKeySecret?.MaskChars(0)}'");
                Console.WriteLine($"[INFO] userBasedPermissionsStep.LambdaUserCredentials ApiKeyId='{userBasedPermissionsStep.StackModifyUserCredentials?.ApiKeyId?.MaskChars(4)}', ApiKeySecret='{userBasedPermissionsStep.StackModifyUserCredentials?.ApiKeySecret?.MaskChars(0)}'");

                // 5. complete onboarding - create cloud account, rulesets, serverless account if selected
                await ExecuteStep(new AccountCreationStep(_apiProvider, _retryAndBackoffService, request.AwsAccountId, request.AwsAccountRegion, request.OnboardingId, null, request.RootStackId, userBasedPermissionsStep.AwsUserCredentials, userBasedPermissionsStep.StackModifyUserCredentials, null));

                // 6. create Posture policies - create cloud account, rulesets, serverless account if selected
                await ExecuteStep(new CreatePosturePoliciesStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));

                // 7. Delete the service account
                await ExecuteStep(new DeleteServiceAccountStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));

                // 8. Write cloudformation lambda custom resoource reponse back to S3 to signal Stack created succesfully.
                Console.WriteLine($"[INFO] About to postback custom resource response success");
                await _retryAndBackoffService.RunAsync(() => customResourceResponseHandler?.PostbackSuccess(), 3);
                Console.WriteLine($"[INFO] Custom resource response successful");

                Console.WriteLine($"[INFO] Finished onboarding workflow successfully - OnboardingId: {request?.OnboardingId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed onboarding process. Error={ex}");
                await TryUpdateStatusFailureInDynamo(request.OnboardingId, ex.ToString(), ex is OnboardingException ? (ex as OnboardingException).Feature : Enums.Feature.None);
                await TryRollback();
                await TryPostCustomResourceFailureResultToS3(customResourceResponseHandler, ex.ToString());
                throw;
            }
            finally
            {
                await TryCleanUpResources();
            }
        }
    }
}