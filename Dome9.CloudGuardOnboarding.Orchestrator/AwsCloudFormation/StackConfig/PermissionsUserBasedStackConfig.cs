
namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PermissionsUserBasedStackConfig : OnboardingStackConfig
    {
        public PermissionsUserBasedStackConfig(
            string templateS3Url,
            string stackName,
            string onboardingId,
            string uniqueSuffix,
            int executionTimeoutMinutes)
            : base(onboardingId, templateS3Url, stackName, uniqueSuffix, executionTimeoutMinutes)
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()}";
        }
    }
}