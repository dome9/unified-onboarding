using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class GetConfigurationStep : StepBase
    {
        private readonly string _onboardingId;
        public ConfigurationsResponseModel Configuration { get; set; }

        public GetConfigurationStep
        (
            ICloudGuardApiWrapper apiProvider,
            IRetryAndBackoffService retryAndBackoffService,
            string onboardingId
        )
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _onboardingId = onboardingId;
        }

        public override async Task Execute()
        {
            try
            {
                Console.WriteLine($"[INFO] About to get configurations");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Getting configurations from CloudGuard", Enums.Feature.None)));
                Configuration = await _retryAndBackoffService.RunAsync(() => _apiProvider.GetConfiguration(new ConfigurationsRequestModel(_onboardingId)));
                Console.WriteLine($"[INFO] Got configurations successfully");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Successfully got configurations from CloudGuard", Enums.Feature.None)));
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
