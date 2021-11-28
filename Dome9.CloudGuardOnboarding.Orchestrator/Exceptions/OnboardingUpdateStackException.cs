namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    internal class OnboardingUpdateStackException : OnboardingException
    {
        public string StackName { get; set; }
        public OnboardingUpdateStackException(string message, string stackName, Enums.Feature feature) : base(message, feature)
        {
        }
    }
}