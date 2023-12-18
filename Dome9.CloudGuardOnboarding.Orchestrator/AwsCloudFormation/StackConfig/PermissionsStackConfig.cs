
namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PermissionsStackConfig : OnboardingStackConfig
    {
        public string CloudGuardAwsAccountId { get; set; }
        public string RoleExternalTrustSecret { get; set; }
        public string UseAwsReadOnlyPolicy { get; set; }

        public PermissionsStackConfig(
            string templateS3Url,
            string stackName,
            string onboardingId,
            string cloudGuardAwsAccountId,
            string cloudGuardExternalTrustSecret,
            string uniqueSuffix,
            string useAwsReadOnlyPolicy,
            int executionTimeoutMinutes)
            : base(onboardingId, templateS3Url, stackName, uniqueSuffix, executionTimeoutMinutes)
        {
            CloudGuardAwsAccountId = cloudGuardAwsAccountId;
            RoleExternalTrustSecret = cloudGuardExternalTrustSecret;
            UseAwsReadOnlyPolicy = useAwsReadOnlyPolicy;
        }
        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(RoleExternalTrustSecret)}='{RoleExternalTrustSecret}', {nameof(CloudGuardAwsAccountId)}='{CloudGuardAwsAccountId}', {nameof(UseAwsReadOnlyPolicy)}='{UseAwsReadOnlyPolicy}'";
        }
    }
}