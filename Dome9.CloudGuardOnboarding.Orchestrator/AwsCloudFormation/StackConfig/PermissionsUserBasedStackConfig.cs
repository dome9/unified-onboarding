using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PermissionsUserBasedStackConfig : OnboardingStackConfig
    {
        public PermissionsUserBasedStackConfig(
            string templateS3Url,
            string stackName,
            string onboardingId,        
            int executionTimeoutMinutes)
            : base(onboardingId, templateS3Url, stackName, executionTimeoutMinutes)
        {
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}