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

                var initialServiceAccount = new ServiceAccount(request.CloudGuardApiKeyId, request.CloudGuardApiKeySecret, request.ApiBaseUrl);
                _apiProvider.SetLocalCredentials(initialServiceAccount);

                Console.WriteLine($"[INFO] Executing onboarding process - OnboardingId: {request?.OnboardingId}");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(request.OnboardingId, Enums.Status.PENDING, "Starting onboarding workflow", Enums.Feature.None)));

                // 1. create new service account and delete initial service account with exposed credentials (stack url containing the credentials could be passed around)               
                await ExecuteStep(new ReplaceServiceAccountStep(_apiProvider, _retryAndBackoffService, initialServiceAccount, request.OnboardingId));

                // 2.  validate onboarding id
                await ExecuteStep(new ValidateOnboardingStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));

                // 3. get configuration from API
                var configurationStep = await ExecuteStep(new GetConfigurationStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));
                var configuration = (configurationStep as GetConfigurationStep).Configuration;

                // 4. run the Posture stack (create cross account user for CloudGuard)
                var userBasedPostureStep = new PostureUserBasedStackCreationStep(_apiProvider, _retryAndBackoffService, configuration.PostureStackName, request.AwsAccountRegion, request.S3BucketName, configuration.PostureTemplateS3Path, request.OnboardingId, request.AwsPartition);
                await ExecuteStep(userBasedPostureStep);
                Console.WriteLine($"[INFO] userBasedPostureStep.AwsUserCredentials ApiKeyId='{userBasedPostureStep.AwsUserCredentials?.ApiKeyId?.MaskChars(4)}', ApiKeySecret='{userBasedPostureStep.AwsUserCredentials?.ApiKeySecret?.MaskChars(0)}'");

                // 5. complete onboarding - create cloud account, rulesets, serverless account if selected
                await ExecuteStep(new AccountCreationStep(_apiProvider, _retryAndBackoffService, request.AwsAccountId, request.OnboardingId, userBasedPostureStep.AwsUserCredentials));

                // 8. Delete the service account
                await ExecuteStep(new DeleteServiceAccountStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));

                // 9. Write cloudformation lambda custom resoource reponse back to S3 to signal Stack created succesfully.
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