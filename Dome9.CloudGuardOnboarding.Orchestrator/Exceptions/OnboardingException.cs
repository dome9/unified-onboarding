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

        public override string ToString()
        {
            return $"{nameof(OnboardingException)} with {nameof(Feature)}='{Feature}'. {base.ToString()}";
        }
    }
}
