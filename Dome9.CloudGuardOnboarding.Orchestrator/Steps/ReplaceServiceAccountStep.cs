using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class ReplaceServiceAccountStep : IStep
    {
        private readonly ICloudGuardApiWrapper _apiProvider;
        private ServiceAccount _serviceAccount;
        private readonly string _onboardingId;

        public ReplaceServiceAccountStep(ICloudGuardApiWrapper apiProvider, ServiceAccount serviceAccount, string onboardingId)
        {
            _apiProvider = apiProvider;
            _serviceAccount = serviceAccount;
            _onboardingId = onboardingId;
        }
        public async Task Execute()
        {
            try
            {
                

                ServiceAccount newServiceAccount = null;
                // set initially received account from Lambda funciton
                _apiProvider.SetLocalCredentials(_serviceAccount);

                Console.WriteLine($"[INFO] About to replace service account");
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, "Replace service account", Status.PENDING));

                // get new service account
                try
                {
                    newServiceAccount = await _apiProvider.ReplaceServiceAccount(new CredentialsModel { OnboardingId = _onboardingId });
                    if (!ServiceAccountIsValid(newServiceAccount))
                    {
                        try
                        {
                            await _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, "Failed to replace service account", Status.PENDING));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] [{nameof(ReplaceServiceAccountStep)}.{nameof(Execute)}]Could not update error status. Error={ex}");
                        }

                        throw new Exception("Created new service account is invalid");
                    }

                    _serviceAccount = newServiceAccount;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to create new service account", ex);
                }

                // set provider to use new account 
                _apiProvider.SetLocalCredentials(_serviceAccount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to execute {nameof(ReplaceServiceAccountStep)} step. Error={ex}");
                throw;                
            }
        }

        private static bool ServiceAccountIsValid(ServiceAccount newServiceAccount)
        {
            return newServiceAccount != null 
                && newServiceAccount.ApiCredentials != null 
                && !string.IsNullOrWhiteSpace(newServiceAccount.ApiCredentials.ApiKeyId) 
                && !string.IsNullOrWhiteSpace(newServiceAccount.ApiCredentials.ApiKeySecret);
        }

        public Task Rollback()
        {
            return Task.CompletedTask;
        }

        public Task Cleanup()
        {
            // TODO: delete resources if necessary
            return Task.CompletedTask;
        }
    }
}
