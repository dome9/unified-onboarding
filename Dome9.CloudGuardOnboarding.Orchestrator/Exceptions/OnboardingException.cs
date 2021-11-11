using System;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingException : Exception
    {
        public Enums.Feature Feature { get; private set; }
        public OnboardingException(string message, Enums.Feature feature) : base(message)
        {
            Feature = feature;
        }
    }
}
