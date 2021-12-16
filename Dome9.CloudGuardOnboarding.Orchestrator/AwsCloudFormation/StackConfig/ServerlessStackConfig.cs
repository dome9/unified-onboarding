using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ServerlessStackConfig : OnboardingStackConfig
    {
        public string CloudGuardAwsAccountId { get; set; }
        public string ServerlessStage { get; set; }
        public string ServerlessRegion { get; set; }

        public ServerlessStackConfig(
            string templateS3Url, 
            string stackName, 
            string onboardingId,
            string uniqueSuffix,
            int executionTimeoutMinutes,
            string cloudGuardAwsAccountId,
            string serverlessStage,
            string serverlessRegion)
            : base(onboardingId, templateS3Url, stackName, uniqueSuffix, executionTimeoutMinutes)
        {
            CloudGuardAwsAccountId = cloudGuardAwsAccountId;
            ServerlessStage = serverlessStage;
            ServerlessRegion = serverlessRegion;
        }
    }
}
