using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Retry
{
    public interface IRetryAndBackoffService
    {
        Task RunAsync(Func<Task> operation, int maxTryCount = 5);
        Task<TResult> RunAsync<TResult>(Func<Task<TResult>> operation, int maxTryCount = 5);
    }
}
