namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class AccountModel
    {
        public string OnboardingId { get; set; }
        public string AwsAccountId { get; set; }
        public string AwsAccountName { get; set; }

        /// <summary>
        /// Only for role-based onboarding (aws standard), will remain null otherwise.
        /// </summary>
        public string LambdaRoleArn { get; set; }

        /// <summary>
        /// Only for user-based onboarding (e.g. gov, cn), will remain null otherwise.
        /// </summary>
        public ApiCredentials AwsUserApiCredentials { get; set; }

        /// <summary>
        /// Only for user-based onboarding (e.g. gov, cn), will remain null otherwise.
        /// </summary>
        public ApiCredentials LambdaUserCredentials { get; set; }

        public string RootStackId { get; set; }

        public string AwsRegion { get; set; }

        public AccountModel(
            string onboardingId,
            string awsAccountId,
            string awsAccountName,
            string awsRegion,
            string lambdaRoleArn,
            string rootStackId,
            ApiCredentials awsUserApiCredentials,
            ApiCredentials lambdaUserCredentials)
        {
            OnboardingId = onboardingId;
            AwsAccountId = awsAccountId;
            AwsRegion = awsRegion;
            AwsAccountName = awsAccountName;
            LambdaRoleArn = lambdaRoleArn;
            AwsUserApiCredentials = awsUserApiCredentials;
            LambdaUserCredentials = lambdaUserCredentials;
            RootStackId = rootStackId;
        }
    }
}
