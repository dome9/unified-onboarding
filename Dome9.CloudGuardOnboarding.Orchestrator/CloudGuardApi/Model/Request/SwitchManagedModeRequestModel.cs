using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request
{
    public class SwitchManagedModeRequestModel
    {
        public string OnboardingId { get; set; }
        public bool IsManaged { get; set; }
        public string StackModifyRoleArn { get; set; }
        public ApiCredentials StackModifyUserCredentials { get; set; }

        public SwitchManagedModeRequestModel(string onboardingId, bool isManaged, string stackModifyRoleArn, ApiCredentials stackModifyUserCredentials)
        {
            OnboardingId = onboardingId;
            IsManaged = isManaged;
            StackModifyRoleArn = stackModifyRoleArn;
            StackModifyUserCredentials = stackModifyUserCredentials;
        }
    }
}
