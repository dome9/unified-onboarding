using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig
{
    public class InteligenceStackConfig : OnboardingStackConfig
    {
        public string CloudtrailS3BucketName { get; set; }
        public string CloudGuardRoleName { get; set; }
        public InteligenceStackConfig
            (string templateS3Url,
            string stackName,
            string onboardingId,
            string cloudtrailS3BucketName,
            string cloudGuardRoleName,
            int executionTimeoutMinutes
            )
            : base(onboardingId, templateS3Url, stackName, executionTimeoutMinutes)
        {
            CloudtrailS3BucketName = cloudtrailS3BucketName;
            CloudGuardRoleName = cloudGuardRoleName;

        }
    }

}
