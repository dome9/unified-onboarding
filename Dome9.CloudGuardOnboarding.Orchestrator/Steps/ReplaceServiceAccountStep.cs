using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Threading.Tasks;


namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class ReplaceServiceAccountStep : StepBase
    {
        public ServiceAccount ServiceAccount { get; private set; }
        private readonly string _onboardingId;
        private readonly OnboardingAction _action;

        public ReplaceServiceAccountStep(ServiceAccount serviceAccount, string onboardingId, OnboardingAction action)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            ServiceAccount = serviceAccount;
            _onboardingId = onboardingId;
            _action = action;
        }
        public override async Task Execute()
        {
            try
            {
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

                await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Replaced service account successfully", _action));
                Console.WriteLine($"[INFO] Replaced service account successfully");

            }
            catch (Exception ex)
            {
                string message = "Failed to replace service account";
                Console.WriteLine($"[ERROR] [{nameof(ReplaceServiceAccountStep)}.{nameof(Execute)}] {message}. Error={ex}");

                if (ex is OnboardingException)
                {
                    throw;
                }
               
                throw new OnboardingException(message, Enums.Feature.None);                                                
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
