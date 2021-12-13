using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class ValidateOnboardingStep : StepBase
    {       
        private readonly string _onboardingId;

        public ValidateOnboardingStep
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

        public override Task Cleanup()
        {
            return Task.CompletedTask;
        }

        public override async Task Execute()
        {
            try
            {
                Console.WriteLine($"[INFO] About to validate onboarding id");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Validating onboarding id",  null, null, null)));
                await _retryAndBackoffService.RunAsync(() => _apiProvider.ValidateOnboardingId(_onboardingId));
                Console.WriteLine($"[INFO] Validated onboarding id successfully");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Validated onboarding id successfully", null, null, null)));
            }
            catch (Exception ex)
            {
                await TryUpdateStatusError(_onboardingId, ex.Message, Enums.Feature.Permissions);
                throw;
            }        
        }

        public override Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
