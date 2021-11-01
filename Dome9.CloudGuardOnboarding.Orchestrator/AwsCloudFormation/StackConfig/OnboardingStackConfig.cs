﻿using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingStackConfig : StackConfig
    { 
        public OnboardingStackConfig(
            string onboardingId, 
            string templateS3Url, 
            string stackName, 
            List<string> capabilities, 
            int executionTimeoutMinutes) 
            : base(templateS3Url, stackName, capabilities, executionTimeoutMinutes)
        {
            OnboardingId = onboardingId;
        }

        public string OnboardingId { get; set; }
    }
}