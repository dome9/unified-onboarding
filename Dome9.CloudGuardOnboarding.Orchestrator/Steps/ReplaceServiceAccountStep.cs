using System;
using System.Threading.Tasks;


namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class ReplaceServiceAccountStep : StepBase
    {
        public ServiceAccount ServiceAccount { get; private set; }
        private readonly string _onboardingId;

        public ReplaceServiceAccountStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, ServiceAccount serviceAccount, string onboardingId)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            ServiceAccount = serviceAccount;
            _onboardingId = onboardingId;
        }
        public override async Task Execute()
        {
            try
            {
                // set initially received account from Lambda funciton
                _apiProvider.SetLocalCredentials(ServiceAccount);

                Console.WriteLine($"[INFO] About to replace service account");

                // get new service account
                try
                {
                    ServiceAccount newServiceAccount = await _apiProvider.ReplaceServiceAccount(new CredentialsModel { OnboardingId = _onboardingId });
                    if (!IsServiceAccountValid(newServiceAccount))
                    {
                        throw new OnboardingException("Created new service account is invalid", Enums.Feature.None);
                    }

                    ServiceAccount = newServiceAccount;
                }
                catch (OnboardingException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new OnboardingException($"Failed to create new service account: {ex.Message}", Enums.Feature.None);
                }

                // set provider to use new account 
                _apiProvider.SetLocalCredentials(ServiceAccount);

                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Replaced service account successfully", null, null, null)));
                Console.WriteLine($"[INFO] Replaced service account successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to execute {nameof(ReplaceServiceAccountStep)} step. Error={ex}");
                await TryUpdateStatusError(_onboardingId, "Failed to replace service account", Enums.Feature.None);
                if(ex is OnboardingException)
                {
                    throw;
                }
               
                throw new OnboardingException(ex.Message, Enums.Feature.None);                                                
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
