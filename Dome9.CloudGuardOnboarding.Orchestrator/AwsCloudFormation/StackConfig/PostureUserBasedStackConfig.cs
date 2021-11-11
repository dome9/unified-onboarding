using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PostureUserBasedStackConfig :  OnboardingStackConfig
    {
        public string AwsPartition { get; set; }

        public PostureUserBasedStackConfig(
            string templateS3Url,
            string stackName,
            List<string> capabilities,
            string onboardingId,
            string awsPartition,
            int executionTimeoutMinutes)
            : base(onboardingId, templateS3Url, stackName, capabilities, executionTimeoutMinutes)
        {
            AwsPartition = awsPartition;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(AwsPartition)}='{AwsPartition}'" ;
        }
    }
}