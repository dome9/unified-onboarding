using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingDeleteStackException : OnboardingException
    {
        public string StackName { get; set; }
        public OnboardingDeleteStackException(string message, string stackName, Enums.Feature feature) : base(message, feature)
        {
        }
    }
}
