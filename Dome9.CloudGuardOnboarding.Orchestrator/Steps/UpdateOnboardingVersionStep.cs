using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class UpdateOnboardingVersionStep : StepBase
    {
        private readonly string _onboardingId;
        private readonly string _version;
        private readonly OnboardingAction _action;

        public UpdateOnboardingVersionStep(string onboardingId, string version, OnboardingAction action)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _onboardingId = onboardingId;
            _version = version;
            _action = action;
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO] About to update onboarding version. version={_version}");
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, $"About to update version", _action));
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingVersion(_onboardingId, _version));
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, $"Updated version successfully", _action));
            Console.WriteLine($"[INFO] updated onboarding version successfully, version={_version}");
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
