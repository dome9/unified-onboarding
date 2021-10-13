using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class RetryAndBackoffService : IRetryAndBackoffService
    {
        
        private readonly IRetryIntervalProvider _intervalProvider;

        public RetryAndBackoffService(IRetryIntervalProvider intervalProvider)
        {
            _intervalProvider = intervalProvider;            
        }

        public async Task RunAsync(Func<Task> operation, int maxTryCount = 5) 
        {            
            int maxRetryCount = maxTryCount - 1;
            var methodName = operation.Method.Name;

            int i = 0;
            var delayIntervals = _intervalProvider.GetDelayIntervals(maxRetryCount);
            if (delayIntervals?.Count() != maxRetryCount)
            {
                throw new Exception($"Delay interval count should be '{maxRetryCount}', but is '{delayIntervals?.Count()}'");
            }

            while(true)
            {
                try
                {
                    i++;
                    await operation();
                    if (i > 1)
                    {
                        Console.WriteLine($"[WARN][{nameof(RetryAndBackoffService)}.{nameof(RunAsync)}] Execution of {methodName} succeeded only after {i} tries.");
                    }
                    return;
                }
                catch (Exception ex)
                {
                    string severity = i == maxTryCount ? "ERROR" : "WARN";
                    string willTryMore = i == maxTryCount ? string.Empty : $", will try again after delay of {delayIntervals[i - 1].TotalMilliseconds}ms";

                    Console.WriteLine($"[{severity}][{nameof(RetryAndBackoffService)}.{nameof(RunAsync)}] Execution of {methodName} failed on try {i}{willTryMore}. Error={ex}");

                    if (i == maxTryCount)
                    {                        
                        throw;
                    }

                    await Task.Delay(delayIntervals[i-1]);
                }
            }
        }

        public async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> operation, int maxTryCount = 5)
        {
            int maxRetryCount = maxTryCount - 1;
            var methodName = operation.Method.Name;

            int i = 0;
            var delayIntervals = _intervalProvider.GetDelayIntervals(maxRetryCount);
            if (delayIntervals?.Count() != maxRetryCount)
            {
                throw new Exception($"Delay interval count should be '{maxRetryCount}', but is '{delayIntervals?.Count()}'");
            }

            while (true)
            {
                try
                {
                    i++;
                    var result = await operation();
                    if (i > 1)
                    {
                        Console.WriteLine($"[WARN][{nameof(RetryAndBackoffService)}.{nameof(RunAsync)}] Execution of {methodName} succeeded only after {i} tries.");
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    string severity = i == maxTryCount ? "ERROR" : "WARN";
                    string willTryMore = i == maxTryCount ? string.Empty : $", will try again after delay of {delayIntervals[i - 1].TotalMilliseconds}ms";

                    Console.WriteLine($"[{severity}][{nameof(RetryAndBackoffService)}.{nameof(RunAsync)}] Execution of {methodName} failed on try {i}{willTryMore}. Error={ex}");

                    if (i == maxTryCount)
                    {
                        throw;
                    }

                    await Task.Delay(delayIntervals[i - 1]);
                }
            }
        }
    }
}
