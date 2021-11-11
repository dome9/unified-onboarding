using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ServerlessStackConfig : OnboardingStackConfig
    {
        public ServerlessStackConfig(
            string templateS3Url, 
            string stackName, 
            List<string> capabilities, 
            string onboardingId, 
            int executionTimeoutMinutes)
            : base(onboardingId, templateS3Url, stackName, capabilities, executionTimeoutMinutes)
        {
        }
    }
}
