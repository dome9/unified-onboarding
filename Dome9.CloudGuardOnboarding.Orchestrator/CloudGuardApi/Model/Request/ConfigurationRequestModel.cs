namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ConfigurationRequestModel
    {
        public string OnboardingId { get; set; }
        public string Version { get; set; }

        public ConfigurationRequestModel(string onboardingId, string version)
        {
            OnboardingId = onboardingId;
            Version = version;
        }
    }
}
