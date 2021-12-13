using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.Steps;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public abstract class OnboardingWorkflowBase : IWorkflow
    {
        protected readonly ICloudGuardApiWrapper _apiProvider;
        protected readonly IRetryAndBackoffService _retryAndBackoffService;
        protected static readonly ConcurrentStack<IStep> Steps = new ConcurrentStack<IStep>();

        public OnboardingWorkflowBase(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
        }

        public abstract Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler);

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

        protected async Task TryUpdateStatusFailureInDynamo(string onboardingId, string error, Enums.Feature feature = Enums.Feature.None)
        {
            try
            {
                // General status error
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(onboardingId, Enums.Feature.None, Enums.Status.ERROR, error, null, null, null)));

                if (feature != Enums.Feature.None)
                {
                    // Feature status error (additional to General) 
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(onboardingId, feature, Enums.Status.ERROR, error, null, null, null)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] during {nameof(TryUpdateStatusFailureInDynamo)}. Error={ex}");
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
