
namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingStackConfig : StackConfig
    {         
        public string OnboardingId { get; set; }
        
        public OnboardingStackConfig(
            string onboardingId, 
            string templateS3Url, 
            string stackName,
            string uniqueSuffix,
            int executionTimeoutMinutes) 
            : base(templateS3Url, stackName, uniqueSuffix, executionTimeoutMinutes)
        {
            OnboardingId = onboardingId;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(OnboardingId)}='{OnboardingId}'";
        }
    }
}
