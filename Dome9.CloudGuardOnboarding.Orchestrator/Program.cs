using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
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
            var cloudForamtionRequst = new CloudFormationRequest
            {
                ResourceProperties = new OnboardingRequest
                {
                    OnboardingId = "",
                    ApiBaseUrl = "",
                    CloudGuardApiKeyId = "",
                    CloudGuardApiKeySecret = "",
                    AwsAccountId = "",
                    S3BucketName = "",
                    AwsAccountRegion = ""
                }
            };

            var api = new CloudGuardApiWrapper();
            var retry = new RetryAndBackoffService(new SimpleExponentialRetryIntervalProvider());
            await new UserBasedOnboardingWorkflow().RunAsync(cloudForamtionRequst, null);



        }
    }
}