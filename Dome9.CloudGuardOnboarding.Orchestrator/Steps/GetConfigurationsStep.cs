using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Response;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class GetConfigurationsStep : StepBase
    {
        private readonly string _onboardingId;
        public ConfigurationsResponseModel Configurations { get; set; }

        public GetConfigurationsStep
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
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Getting configurations from CloudGuard", Enums.Feature.ContinuousCompliance)));
                Configurations = await _retryAndBackoffService.RunAsync(() => _apiProvider.GetConfigurations(new ConfigurationsRequestModel(_onboardingId)));
                Console.WriteLine($"[INFO] Got configurations successfully");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Validated onboarding id successfully", Enums.Feature.ContinuousCompliance)));
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
