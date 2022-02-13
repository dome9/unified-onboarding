using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    /// <summary>
    /// Represents onboarding workflow steps that can have rollback or cleanup actions
    /// </summary>
    public interface IStep
    {
        Task Execute();
        Task Rollback();
        Task Cleanup();
    }

    public abstract class StepBase : IStep
    {
        protected IRetryAndBackoffService _retryAndBackoffService;
        protected ICloudGuardApiWrapper _apiProvider;

        public abstract Task Cleanup();
        public abstract Task Execute();
        public abstract Task Rollback();

        public async Task TryUpdateStatusError(string onboardingId, string error, Enums.Feature feature = Enums.Feature.None)
        {
            // just try to report status, but don't propegate error in case of status update failure
            try
            {
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(onboardingId, feature, Enums.Status.ERROR, error, null, null, null)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{this.GetType().Name}.{nameof(TryUpdateStatusError)}]Could not update error status. Error={ex}");
            }
        }

        public async Task TryUpdateStatus(string onboardingId, string msg, Enums.Status status, Enums.Feature feature)
        {
            // try to report status msg
            try
            {
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(onboardingId, feature, status, msg, null, null, null)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{this.GetType().Name}.{nameof(TryUpdateStatusError)}] Could not update msg status. Error={ex}");
            }
        }

        public async Task TryUpdateStatusWarning(string onboardingId, string msg, Enums.Feature feature = Enums.Feature.None)
        {
            // try to report status msg
            try
            {
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(onboardingId, feature, Enums.Status.WARNING, msg, null, null, null)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{this.GetType().Name}.{nameof(TryUpdateStatusError)}] Could not update msg status. Error={ex}");
            }
        }
    }
}
