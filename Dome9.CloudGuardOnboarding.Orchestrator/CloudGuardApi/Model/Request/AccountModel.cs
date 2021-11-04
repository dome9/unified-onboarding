namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class AccountModel
    {
        public string OnboardingId { get; set; }
        public string AwsAccountId { get; set; }
        public string AwsAccountName { get; set; }

        /// <summary>
        /// Only for user-based onboarding (e.g. gov, cn), will remain null otherwise.
        /// </summary>
        public ApiCredentials AwsUserApiCredentials { get; set; }

        public AccountModel()
        {
        }

        public AccountModel(string onboardingId, string awsAccountId, string awsAccountName, ApiCredentials awsUserApiCredentials = null)
        {
            OnboardingId = onboardingId;
            AwsAccountId = awsAccountId;
            AwsAccountName = awsAccountName;
            AwsUserApiCredentials = awsUserApiCredentials;
        }
    }
}
