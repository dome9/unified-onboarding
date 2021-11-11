using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class RetryAndBackoffServiceMock : IRetryAndBackoffService
    {
        public RetryAndBackoffServiceMock(IRetryIntervalProvider intervalProvider)
        {
        }

        public Task RunAsync(Func<Task> operation, int maxTryCount = 5)
        {
            return Task.CompletedTask;
        }

        public Task<TResult> RunAsync<TResult>(Func<Task<TResult>> operation, int maxTryCount = 5)
        {
            throw new NotImplementedException();
        }
    }
}
