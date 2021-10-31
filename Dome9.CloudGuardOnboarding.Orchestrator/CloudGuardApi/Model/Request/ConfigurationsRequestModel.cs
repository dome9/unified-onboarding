using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request
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
