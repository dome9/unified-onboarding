namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig
{
    public class IntelligenceStackConfig : OnboardingStackConfig
    {
        public string CloudtrailS3BucketName { get; set; }
        public string CloudGuardRoleName { get; set; }
        public IntelligenceStackConfig(
            string templateS3Url,
            string stackName,
            string onboardingId,
            string cloudtrailS3BucketName,
            string cloudGuardRoleName, 
            string uniqueSuffix,
            int executionTimeoutMinutes)
            : base(onboardingId, templateS3Url, stackName, uniqueSuffix, executionTimeoutMinutes)
        {
            CloudtrailS3BucketName = cloudtrailS3BucketName;
            CloudGuardRoleName = cloudGuardRoleName;
        }
    }
}
