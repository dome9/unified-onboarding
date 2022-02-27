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

    }
}
