using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class AccountModel
    {
        public string OnboardingId { get; set; }
        public string AwsAccountId { get; set; }
        public string AwsAccountName { get; set; }

        public AccountModel()
        {
        }

        public AccountModel(string onboardingId, string awsAccountId, string awsAccountName)
        {
            OnboardingId = onboardingId;
            AwsAccountId = awsAccountId;
            AwsAccountName = awsAccountName;
        }
    }
}
