using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var onboardingRequest =
                new OnboardingRequest
                {
                    OnboardingId = "",
                    ApiBaseUrl = "",
                    CloudGuardApiKeyId = "",
                    CloudGuardApiKeySecret = "",
                    AwsAccountId = "",
                    S3BucketName = "",
                    AwsAccountRegion = ""
                };

            var api = new CloudGuardApiWrapper();
            var retry = new RetryAndBackoffService(new SimpleExponentialRetryIntervalProvider());
            await new UserBasedOnboardingWorkflow(api, retry).RunAsync(onboardingRequest, null);
        }
    }
}