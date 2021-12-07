namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingRequest
    {
        public string ServiceToken { get; set; }
        public string OnboardingId { get; set; }
        public string ApiBaseUrl { get; set; }
        public string CloudGuardApiKeyId { get; set; }
        public string CloudGuardApiKeySecret { get; set; }
        public string AwsAccountId { get; set; } 
        public string S3BucketName { get; set; }
        public string AwsAccountRegion {  get; set; }
        public string AwsPartition { get; set; }
        public string Version { get; set; }
        public string OnboardingStackModifyRoleArn { get; set; }
        public string EnableRemoteStackModify { get; set; }
        public string RootStackId { get; set; }

        public override string ToString()
        {
            return $"OnboardingRequest: [OnboardingId={OnboardingId}], [AwsAccountId={AwsAccountId}], [ApiBaseUrl={ApiBaseUrl}], " +
                $"[CloudGuardApiKeyId={CloudGuardApiKeyId.MaskChars(3)}], [CloudGuardApiKeySecret={CloudGuardApiKeySecret.MaskChars(0)}]," +
                $"[S3BucketName={S3BucketName}], [AwsAccountRegion={AwsAccountRegion}], [AwsPartition={AwsPartition}]" +
                $"[OnboardingStackModifyRoleArn={OnboardingStackModifyRoleArn}], [EnableRemoteStackModify={EnableRemoteStackModify}]" +
                $"[RootStackId={RootStackId}, Version={Version}]";
        }
    }

    public class CloudFormationRequest
    {
        public string RequestType { get; set; }
        public string ResponseURL { get; set; }
        public string StackId { get; set; }
        public string RequestId { get; set; }
        public string ResourceType { get; set; }
        public string LogicalResourceId { get; set; }
        public OnboardingRequest ResourceProperties { get; set; }
        public override string ToString()
        {
            return $"CloudFormationRequest: [RequestType={RequestType}], [ResponseURL={ResponseURL}], [StackId={StackId}], [RequestId={RequestId}], [ResourceType={ResourceType}], [LogicalResourceId={LogicalResourceId}] >> Nested ResourceProperties: {ResourceProperties}";
        }
    }   
}
