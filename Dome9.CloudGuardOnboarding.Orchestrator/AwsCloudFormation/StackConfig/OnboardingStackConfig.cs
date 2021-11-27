﻿using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingStackConfig : StackConfig
    { 
        public OnboardingStackConfig(
            string onboardingId, 
            string templateS3Url, 
            string stackName, 
            int executionTimeoutMinutes) 
            : base(templateS3Url, stackName, executionTimeoutMinutes)
        {
            OnboardingId = onboardingId;
        }

        public string OnboardingId { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(OnboardingId)}='{OnboardingId}'";
        }
    }
}
