namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class AccountModel
    {
        public string OnboardingId { get; set; }
        public string AwsAccountId { get; set; }
        public string AwsAccountName { get; set; }
        public string StackModifyRoleArn { get; set; }
        public ApiCredentials AwsUserApiCredentials { get; set; }
        public ApiCredentials StackModifyUserCredentials { get; set; }
        public string RootStackId { get; set; }
        public string AwsRegion { get; set; }
        public string CrossAccountRoleArn { get; set; }

        public AccountModel(
            string onboardingId,
            string awsAccountId,
            string awsAccountName,
            string awsRegion,
            string stackModifyRoleArn,
            string rootStackId,
            ApiCredentials awsUserApiCredentials,
            ApiCredentials stackModifyUserCredentials,
            string crossAccountRoleArn)
        {
            OnboardingId = onboardingId;
            AwsAccountId = awsAccountId;
            AwsRegion = awsRegion;
            AwsAccountName = awsAccountName;
            StackModifyRoleArn = stackModifyRoleArn;
            AwsUserApiCredentials = awsUserApiCredentials;
            StackModifyUserCredentials = stackModifyUserCredentials;
            RootStackId = rootStackId;
            CrossAccountRoleArn = crossAccountRoleArn;
        }
    }
}
