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
                    PostureStackName = $"YoavTestOnboardingStack-V6-{DateTime.Now.ToFileTimeUtc()}",
                    PostureTemplateS3Url= "https://unified-onboarding.s3.us-east-2.amazonaws.com/cft/posture_readonly_cft.yml",
                    OnboardingId = "1e744ee9-c597-4173-bf28-a6f56f7c00be",
                    CloudGuardAwsAccountId = "370030827932",     //AWS account which the dome9 website is running under
                    ApiBaseUrl = "https://api.370030827932.dev.falconetix.com",
                    RoleExternalTrustSecret = "d9ExtTrusSecret123",
                    ServerlessProtectionEnabled = "False",
                    CloudGuardApiKeyId = "<api key id of dome9 website>",
                    CloudGuardApiKeySecret = "<api key secret of dome9 website>"
                };

            var api = new CloudGuardApiWrapper();// CloudGuardApProviderMock();

            await new OnboardingWorkflow(api).RunAsync(onboardingRequest, null);
        }
    }
}   
