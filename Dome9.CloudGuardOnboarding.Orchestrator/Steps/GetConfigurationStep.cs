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
        private readonly OnboardingAction _action;
        public ConfigurationResponseModel Configuration { get; set; }

        public GetConfigurationStep(string onboardingId, string version, OnboardingAction action)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _onboardingId = onboardingId;
            _version = version;
            _action = action;
        }

        public override async Task Execute()
        {
            try
            {
                Console.WriteLine($"[INFO] About to get configuration. version={_version}");
                await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Getting configuration from CloudGuard", _action));
                Configuration = await _retryAndBackoffService.RunAsync(() => _apiProvider.GetConfiguration(new ConfigurationRequestModel(_onboardingId, _version)));
                Console.WriteLine($"[INFO] Got configuration successfully, {nameof(Configuration)}=[{Configuration}]");
                await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Successfully got configuration from CloudGuard", _action));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INFO] Failed to got configuration, error={ex}");
                await StatusHelper.TryUpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.ERROR, "Failed to get configuration", _action));
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
