using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class InitServiceAccountStep : StepBase
    {
        private readonly string _onboardingId;
        private readonly string _cloudGuardApiKeyId;
        private readonly string _cloudGuardApiKeySecret;
        private readonly string _apiBaseUrl;
        
        public ServiceAccount ServiceAccount { get; set; }

        public InitServiceAccountStep(
            string onboardingId,
            string cloudGuardApiKeyId,
            string cloudGuardApiKeySecret,
            string apiBaseUrl)
        {
            _onboardingId = onboardingId;
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _cloudGuardApiKeyId = cloudGuardApiKeyId;
            _cloudGuardApiKeySecret = cloudGuardApiKeySecret;
            _apiBaseUrl = apiBaseUrl;
        }

        public override Task Cleanup()
        {
            return Task.CompletedTask;
        }

        public override Task Execute()
        {
            ServiceAccount = new ServiceAccount(_cloudGuardApiKeyId, _cloudGuardApiKeySecret, _apiBaseUrl);
            _apiProvider.SetLocalCredentials(ServiceAccount);
            return Task.CompletedTask;
        }

        public override async Task Rollback()
        {
            try
            {
                Console.WriteLine($"[INFO] [{nameof(InitServiceAccountStep)}.{nameof(Rollback)}] About to delete service account");
                // must let all the statuses get posted before we delete the service account
                await _retryAndBackoffService.RunAsync(() => _apiProvider.DeleteServiceAccount(new CredentialsModel { OnboardingId = _onboardingId }));
                // can't write to dynamo anymore since we just deleted the service account 
                Console.WriteLine($"[INFO] [{nameof(InitServiceAccountStep)}.{nameof(Rollback)}] Deleted service account");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INFO] [{nameof(InitServiceAccountStep)}.{nameof(Rollback)}] Failed to delete service account. error={ex}");
            }
        }
    }
}
