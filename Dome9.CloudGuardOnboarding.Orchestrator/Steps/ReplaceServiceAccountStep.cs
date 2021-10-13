using System;
using System.Threading.Tasks;


namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class ReplaceServiceAccountStep : StepBase
    {
        private ServiceAccount _serviceAccount;
        private readonly string _onboardingId;

        public ReplaceServiceAccountStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, ServiceAccount serviceAccount, string onboardingId)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _serviceAccount = serviceAccount;
            _onboardingId = onboardingId;
        }
        public override async Task Execute()
        {
            try
            {
                // set initially received account from Lambda funciton
                _apiProvider.SetLocalCredentials(_serviceAccount);

                Console.WriteLine($"[INFO] About to replace service account");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Replacing service account", Enums.Feature.ContinuousCompliance)));

                // get new service account
                try
                {
                    ServiceAccount newServiceAccount = await _apiProvider.ReplaceServiceAccount(new CredentialsModel { OnboardingId = _onboardingId });
                    if (!IsServiceAccountValid(newServiceAccount))
                    {
                        throw new OnboardingException("Created new service account is invalid", Enums.Feature.ContinuousCompliance);
                    }

                    _serviceAccount = newServiceAccount;
                }
                catch (OnboardingException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new OnboardingException($"Failed to create new service account: {ex.Message}", Enums.Feature.ContinuousCompliance);
                }

                // set provider to use new account 
                _apiProvider.SetLocalCredentials(_serviceAccount);

                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Replaced service account successfully", Enums.Feature.ContinuousCompliance)));
                Console.WriteLine($"[INFO] Replaced service account successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to execute {nameof(ReplaceServiceAccountStep)} step. Error={ex}");
                await TryUpdateStatusError(_onboardingId, "Failed to replace service account", Enums.Feature.ContinuousCompliance);
                if(ex is OnboardingException)
                {
                    throw;
                }
               
                throw new OnboardingException(ex.Message, Enums.Feature.ContinuousCompliance);                                                
            }
        }        

        private bool IsServiceAccountValid(ServiceAccount serviceAccount)
        {
            return serviceAccount != null 
                && serviceAccount.ApiCredentials != null 
                && !string.IsNullOrWhiteSpace(serviceAccount.ApiCredentials.ApiKeyId) 
                && !string.IsNullOrWhiteSpace(serviceAccount.ApiCredentials.ApiKeySecret);
        }

        public override Task Rollback()
        {
            return Task.CompletedTask;
        }

        public override Task Cleanup()
        {
            // TODO: delete resources if necessary
            return Task.CompletedTask;
        }
    }
}
