namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig
{
    public class IntelligenceStackConfig : OnboardingStackConfig
    {
        public string CloudtrailS3BucketName { get; set; }
        public string CloudGuardRoleName { get; set; }
        public string IntelligenceSubscriptionEndpoint { get; set; }
        public string IntelligenceAwsAccountId { get; set; }
        public string CloudTrailKmsArn { get; set; }
        
        public IntelligenceStackConfig(
            string templateS3Url,
            string stackName,
            string onboardingId,
            string cloudtrailS3BucketName,
            string intelligenceSubscriptionEndpoint,
            string intelligenceAwsAccountId,
            string cloudGuardRoleName,
            string uniqueSuffix,
            int executionTimeoutMinutes,
            string cloudTrailKmsArn)
            : base(onboardingId, templateS3Url, stackName, uniqueSuffix, executionTimeoutMinutes)
        {
            CloudtrailS3BucketName = cloudtrailS3BucketName;
            CloudGuardRoleName = cloudGuardRoleName;
            IntelligenceSubscriptionEndpoint = intelligenceSubscriptionEndpoint;
            IntelligenceAwsAccountId = intelligenceAwsAccountId;
            CloudTrailKmsArn = cloudTrailKmsArn;
        }
    }
}
