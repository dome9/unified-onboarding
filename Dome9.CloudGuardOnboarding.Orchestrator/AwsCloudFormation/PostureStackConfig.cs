using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PostureStackConfig : StackConfig
    {
        public string OnboardingId { get; set; }
        public string CloudGuardAwsAccountId { get; set; }
        public string RoleExternalTrustSecret { get; set; }

        public PostureStackConfig(string templateS3Url, string stackName, List<string> capabilities, 
            string onboardingId, string cloudGuardAwsAccountId, string cloudGuardExternalTrustSecret, int executionTimeoutMinutes = 5)
            : base(templateS3Url, stackName, capabilities, executionTimeoutMinutes)
        {
            OnboardingId = onboardingId;
            CloudGuardAwsAccountId = cloudGuardAwsAccountId;
            RoleExternalTrustSecret = cloudGuardExternalTrustSecret;
        }

        public PostureStackConfig() { }
    }
}