using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PermissionsStackConfig : OnboardingStackConfig
    {
        public string CloudGuardAwsAccountId { get; set; }
        public string RoleExternalTrustSecret { get; set; }

        public PermissionsStackConfig(
            string templateS3Url,
            string stackName,
            string onboardingId,
            string cloudGuardAwsAccountId,
            string cloudGuardExternalTrustSecret,
            string uniqueSuffix,
            int executionTimeoutMinutes)
            : base(onboardingId, templateS3Url, stackName, uniqueSuffix, executionTimeoutMinutes)
        {
            CloudGuardAwsAccountId = cloudGuardAwsAccountId;
            RoleExternalTrustSecret = cloudGuardExternalTrustSecret;
        }
        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(RoleExternalTrustSecret)}='{RoleExternalTrustSecret}', {nameof(CloudGuardAwsAccountId)}='{CloudGuardAwsAccountId}'";
        }
    }
}