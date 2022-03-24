using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class ValidateOnboardingStep : StepBase
    {       
        private readonly string _onboardingId;

        public ValidateOnboardingStep(string onboardingId)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
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
                await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Validating onboarding id"));
                await _retryAndBackoffService.RunAsync(() => _apiProvider.ValidateOnboardingId(_onboardingId));
                Console.WriteLine($"[INFO] Validated onboarding id successfully");
                await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Validated onboarding id successfully"));
            }
            catch (Exception ex)
            {
                string message = "Failed to validate onboarding";
                Console.WriteLine($"[ERROR] [{nameof(ValidateOnboardingStep)}.{nameof(Execute)}] {message}. Error={ex}");
                throw new OnboardingException(message, Enums.Feature.None);
            }        
        }

        public override Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
