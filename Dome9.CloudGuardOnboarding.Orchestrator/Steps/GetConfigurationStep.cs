using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class GetConfigurationStep : StepBase
    {
        private readonly string _onboardingId;
        private readonly string _version;
        public ConfigurationResponseModel Configuration { get; set; }

        public GetConfigurationStep
        (
            ICloudGuardApiWrapper apiProvider,
            IRetryAndBackoffService retryAndBackoffService,
            string onboardingId,
            string version
        )
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _onboardingId = onboardingId;
            _version = version;
        }

        public override async Task Execute()
        {
            try
            {
                Console.WriteLine($"[INFO] About to get configuration. version={_version}");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Getting configuration from CloudGuard", null, null, null)));
                Configuration = await _retryAndBackoffService.RunAsync(() => _apiProvider.GetConfiguration(new ConfigurationRequestModel(_onboardingId, _version)));
                Console.WriteLine($"[INFO] Got configuration successfully, {nameof(Configuration)}=[{Configuration}]");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Successfully got configuration from CloudGuard", null, null, null)));
            }
            catch (Exception ex)
            {
                await TryUpdateStatusError(_onboardingId, ex.Message, Enums.Feature.None);
                throw;
            }
        }

        public override Task Cleanup()
        {
            return Task.CompletedTask;
        }

        public override Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
