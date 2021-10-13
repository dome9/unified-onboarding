using Dome9.CloudGuardOnboarding.Orchestrator.Steps;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingWorkflow
    {
        private readonly ICloudGuardApiWrapper _apiProvider;
        private readonly IRetryAndBackoffService _retryAndBackoffService;
        private static readonly ConcurrentStack<IStep> Steps = new ConcurrentStack<IStep>();

        public OnboardingWorkflow(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
        }

        public async Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            try
            {
                if(request == null)
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

                //await RetryAndBackoff.RunAsync(() => _apiWrapper.UpdateOnboardingStatus(statusModel), _retryIntervalProvider);
                // 1. create new service account and delete initial service account with exposed credentials (stack url containing the credentials could be passed around)               
                await ExecuteStep(new ReplaceServiceAccountStep(_apiProvider, _retryAndBackoffService, initialServiceAccount, request.OnboardingId));

                // 2.  validate onboarding id
                await ExecuteStep(new ValidateOnboardingStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));                

                // 3. run the Posture stack (create cross account role for Cloud Guard)
                await ExecuteStep(new PostureStackCreationStep(_apiProvider, _retryAndBackoffService, request.PostureStackName, request.PostureTemplateS3Url, request.CloudGuardAwsAccountId, request.RoleExternalTrustSecret, request.OnboardingId));

                // 4. complete onboarding - create cloud account, rulesets, serverless account if selected
                await ExecuteStep(new AccountCreationStep(_apiProvider, _retryAndBackoffService, request.AwsAccountId, request.OnboardingId));


                // serverless - do not fail workflow on exceptions
                try
                {
                    if (bool.TryParse(request.ServerlessProtectionEnabled, out bool serverlessEnabled) && serverlessEnabled)
                    {
                        //// 5. add serverless protection account
                        await ExecuteStep(new ServerlessAddAccountStep(_apiProvider, _retryAndBackoffService, request.AwsAccountId, request.OnboardingId));

                        // 6. create serverless protection stack if enabled
                        await ExecuteStep(new ServerlessStackCreationStep(_apiProvider, _retryAndBackoffService, request.AwsAccountId, request.OnboardingId, request.ServerlessTemplateS3Url, request.ServerlessStackName));
                    }
                    else
                    {
                        await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(request.OnboardingId, Enums.Status.INACTIVE, "Serverless protection disabled", Enums.Feature.ServerlessProtection)));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed handling Serverless protection. Error={ex}"); ;
                }

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

        private async Task<IStep> ExecuteStep(IStep step)
        {
            Steps.Push(step);
            await step.Execute();
            return step;
        }

        private async Task TryPostCustomResourceFailureResultToS3(LambdaCustomResourceResponseHandler customResourceResponseHandler, string error)
        {
            try
            {
                await _retryAndBackoffService.RunAsync(() => customResourceResponseHandler.PostbackFailure(error), 3);
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
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(onboardingId, Enums.Status.ERROR, error)));                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] during {nameof(TryUpdateStatusFailureInDynamo)}. Error={ex}");
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