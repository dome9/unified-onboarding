namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ConfigurationsRequestModel
    {
        public string OnboardingId { get; set; }

        public ConfigurationsRequestModel(string onboardingId)
        {
            OnboardingId = onboardingId;
        }
    }
}
