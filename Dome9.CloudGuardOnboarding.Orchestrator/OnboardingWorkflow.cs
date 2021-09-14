using Dome9.CloudGuardOnboarding.Orchestrator.Steps;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingWorkflow
    {        
        private static ICloudGuardApiWrapper _apiProvider;
        private static readonly ConcurrentStack<IStep> Steps = new ConcurrentStack<IStep>();
        private static string _lastError = null;

        public OnboardingWorkflow(ICloudGuardApiWrapper apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public async Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            try
            {
                Console.WriteLine($"[INFO] Starting onboarding workflow - OnboardingId: {request?.OnboardingId}");

                // 1. create new service account and delete initial service account with exposed credentials (stack url containing the credentials could be passed around)               
                var replaceServiceAccountStep = new ReplaceServiceAccountStep(_apiProvider, new ServiceAccount(request.CloudGuardApiKeyId, request.CloudGuardApiKeySecret, request.ApiBaseUrl), request.OnboardingId);
                Steps.Push(replaceServiceAccountStep);
                await replaceServiceAccountStep.Execute();
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Replaced service account successfully", Status.PENDING));
                Console.WriteLine($"[INFO] Replaced service account successfully");

                // 2.  validate onboarding id
                Console.WriteLine($"[INFO] About to validate onboarding id");
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Validating onboarding id", Status.PENDING));
                await _apiProvider.ValidateOnboardingId(request.OnboardingId);
                Console.WriteLine($"[INFO] Validated onboarding id successfully");
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Validated onboarding id successfully", Status.PENDING));

                // 3. run the Posture stack (create cross account role for Cloud Guard)
                Console.WriteLine($"[INFO] About to create posture stack");
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Creating posture stack", Status.PENDING, Feature.ContinuousCompliance));
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Feature.ContinuousCompliance, "Creating posture stack"));
                var stackCreateStep = new PostureStackCreationStep(_apiProvider, request.PostureStackName, request.PostureTemplateS3Url, request.CloudGuardAwsAccountId, request.RoleExternalTrustSecret, request.OnboardingId);
                Steps.Push(stackCreateStep);
                await stackCreateStep.Execute();                
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Feature.ContinuousCompliance, "Posture stack created successfully"));
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Posture stack created successfully", Status.PENDING));
                Console.WriteLine($"[INFO] Posture stack created successfully");

                // 4. complete onboarding to dome9 - create cloud account
                Console.WriteLine($"[INFO] About to post onboarding request to create cloud account");
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Creating cloud account", Status.PENDING, Feature.ContinuousCompliance));
                string accountName = await AwsCredentialUtils.GetAwsAccountNameAsync(request.AwsAccountId);
                await _apiProvider.OnboardAccount(new AccountModel(request.OnboardingId, request.AwsAccountId, accountName));
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Cloud account created successfully", Status.ACTIVE, Feature.ContinuousCompliance));
                Console.WriteLine($"[INFO] Successfully posted onboarding request. Cloud account created successfully");


                // 5. TODO: create Serverless protection account if enabled
                // The below line is a stub, should be according to config if enabled
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Serverless protection disabled", Status.INACTIVE, Feature.ServerlessProtection));

                // 6. Delete the service account
                Console.WriteLine($"[INFO] About to delete service account");
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, "Deleting service account", Status.ACTIVE));
                await _apiProvider.DeleteServiceAccount(new CredentialsModel { OnboardingId = request.OnboardingId });
                // can't write to dynamo anymore since we just deleted the service account 
                Console.WriteLine($"[INFO] Deleted service account");

                // 7. Write cloudformation lambda custom resoource reponse back to S3 to signal Stack created succesfully.
                Console.WriteLine($"[INFO] About to postback custom resource response success");
                await customResourceResponseHandler?.PostbackSuccess();
                Console.WriteLine($"[INFO] Custom resource response successful");
                
                Console.WriteLine($"[INFO] Finished onboarding workflow successfully - OnboardingId: {request?.OnboardingId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed onboarding process. Error={ex}");
                await TryPostCustomResourceFailureResultToS3(customResourceResponseHandler, ex.ToString());
                await TryUpdateStatusFailureInDynamo(request.OnboardingId, ex.ToString());
                TryRollback();
                throw;
            }
            finally
            {
                TryCleanUpResources();
            }
        }

        private async Task TryPostCustomResourceFailureResultToS3(LambdaCustomResourceResponseHandler customResourceResponseHandler, string error)
        {
            try
            {
                await customResourceResponseHandler.PostbackFailure(error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] during {nameof(TryPostCustomResourceFailureResultToS3)}. Error={ex}");
            }
            
        }

        private async Task TryUpdateStatusFailureInDynamo(string onboardingId, string error)
        {
            try
            {
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(onboardingId, $"Onboarding failed with following error: '{error}'", Status.ERROR));
                // TODO: add something like
                // await _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Status.ERROR.ToString(), Feature.ContinuousCompliance, false));
                // after we pass the Feature that failed to this method 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] during {nameof(TryPostCustomResourceFailureResultToS3)}. Error={ex}");
            }

        }

        private void TryCleanUpResources()
        {
            if (Steps == null | Steps.IsEmpty)
            {
                return;
            }

            // if we had a rollback, steps stack should be empty anyway
            while (Steps.TryPop(out var step))
            {
                try
                {
                    step.Cleanup();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] during resource cleanup. Error={ex}");                    
                }
            }
        }

        private void TryRollback()
        {
            if (Steps == null | Steps.IsEmpty)
            {
                return;
            }

            // if we had a rollback, steps stack should be empty anyway
            while (Steps.TryPop(out var step))
            {
                try
                {
                    step.Rollback();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] during rollback. Error={ex}");
                }
            }
        }
    }
}