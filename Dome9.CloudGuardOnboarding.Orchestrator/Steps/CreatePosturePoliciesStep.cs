using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class CreatePosturePoliciesStep : StepBase
    {
        private readonly string _onboardingId;

        public CreatePosturePoliciesStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, string onboardingId)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _onboardingId = onboardingId;
        }
        public override Task Cleanup()
        {
            return Task.CompletedTask;
        }

        public async override Task Execute()
        {
            try
            {
                Console.WriteLine($"[INFO] About to create posture policies");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.Posture, Enums.Status.PENDING, "Creating Posture policies", null, null, null)));

                await _retryAndBackoffService.RunAsync(() => _apiProvider.CreatePosturePolicies(_onboardingId));

                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.Posture, Enums.Status.ACTIVE, "Posture policies created successfully", null, null, null)));

                Console.WriteLine($"[INFO] Posture policies created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create posture policies. error={ex}");
            }
        }

        public override Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
