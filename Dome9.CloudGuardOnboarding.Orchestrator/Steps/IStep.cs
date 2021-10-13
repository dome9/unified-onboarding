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
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(onboardingId, Enums.Status.ERROR, error, feature)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{this.GetType().Name}.{nameof(TryUpdateStatusError)}]Could not update error status. Error={ex}");
            }
        }
    }
}
