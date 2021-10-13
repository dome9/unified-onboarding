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
                    PostureStackName = $"TestOnboardingStack-{DateTime.Now.ToFileTimeUtc()}",
                    PostureTemplateS3Url= "https://unified-onboarding.s3.us-east-2.amazonaws.com/cft/posture_readonly_cft.yml",
                    OnboardingId = "a88252fa-bd17-42d6-96a6-e850611c9f82",
                    CloudGuardAwsAccountId = "1234567890",  
                    ApiBaseUrl = "https://api.1234567890.dev.falconetix.com",
                    RoleExternalTrustSecret = "d9ExtTrusSecret123",
                    ServerlessProtectionEnabled = "True",
                    CloudGuardApiKeyId = "******",
                    CloudGuardApiKeySecret = "******"
                };

            var api = new CloudGuardApiWrapper();
            var retry = new RetryAndBackoffService(new SimpleExponentialRetryIntervalProvider());
            await new OnboardingWorkflow(api, retry).RunAsync(onboardingRequest, null);
        }
    }
}   
