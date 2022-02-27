using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using Dome9.CloudGuardOnboarding.Orchestrator.Steps;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public abstract class OnboardingWorkflowBase : IWorkflow
    {
        protected readonly ICloudGuardApiWrapper _apiProvider;
        protected readonly IRetryAndBackoffService _retryAndBackoffService;
        protected static readonly ConcurrentStack<IStep> Steps = new ConcurrentStack<IStep>();

        public OnboardingWorkflowBase()
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
        }

        public abstract Task RunAsync(CloudFormationRequest cloudFormationRequest, LambdaCustomResourceResponseHandler customResourceResponseHandler);

        protected async Task<IStep> ExecuteStep(IStep step)
        {
            Steps.Push(step);
            await step.Execute();
            return step;
        }

        protected async Task TryPostCustomResourceFailureResultToS3(LambdaCustomResourceResponseHandler customResourceResponseHandler, string error)
        {
            try
            {
                await _retryAndBackoffService.RunAsync(() => customResourceResponseHandler.PostbackFailure(error), 3);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] during {nameof(TryPostCustomResourceFailureResultToS3)}. Error={ex}");
            }

        }

        protected async Task TryCleanUpResources()
        {
            if (Steps == null | Steps.IsEmpty)
            {
                return;
            }

            // if we had a rollback, steps stack should be empty anyway
            while (Steps.TryPop(out var step))
            {
                try
                {
                    await step.Cleanup();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] during resource cleanup. Error={ex}");
                }
            }
        }

        protected async Task TryRollback()
        {
            if (Steps == null | Steps.IsEmpty)
            {
                return;
            }

            // if we had a rollback, steps stack should be empty anyway
            while (Steps.TryPop(out var step))
            {
                try
                {
                    await step.Rollback();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] during rollback. Error={ex}");
                }
            }
        }
    }
}
