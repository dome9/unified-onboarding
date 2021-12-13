using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ServerlessStackConfig : OnboardingStackConfig
    {
        public ServerlessStackConfig(
            string templateS3Url, 
            string stackName, 
            string onboardingId,
            string uniqueSuffix,
            int executionTimeoutMinutes)
            : base(onboardingId, templateS3Url, stackName, uniqueSuffix, executionTimeoutMinutes)
        {
        }
    }
}
