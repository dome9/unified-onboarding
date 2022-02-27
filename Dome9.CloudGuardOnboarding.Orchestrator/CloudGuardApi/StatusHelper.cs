using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi
{
    public class StatusHelper
    {
        public static async Task UpdateStatusAsync(StatusModel status)
        {
            var retryService = RetryAndBackoffServiceFactory.Get();
            var apiWrapper = CloudGuardApiWrapperFactory.Get();
            await retryService.RunAsync(() => apiWrapper.UpdateOnboardingStatus(status));
        }

        public static async Task TryUpdateStatusAsync(StatusModel status)
        {
            try
            {
                await UpdateStatusAsync(status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(TryUpdateStatusAsync)}] failed. Error={ex}");
            }
            
        }
    }
}
