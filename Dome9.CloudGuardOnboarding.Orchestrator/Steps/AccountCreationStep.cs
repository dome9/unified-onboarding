using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class AccountCreationStep : StepBase
    {
        private readonly string _awsAccountId;
        private readonly string _onboardingId;
        private readonly ApiCredentials _apiCredentials;

        public AccountCreationStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, string awsAccountId, string onboardingId, ApiCredentials apiCredentials = null)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _awsAccountId = awsAccountId;
            _onboardingId = onboardingId;
            _apiCredentials = apiCredentials;
        }
        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }

        public async override Task Execute()
        {
            Console.WriteLine($"[INFO] About to post onboarding request to create cloud account");
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Creating cloud account", Enums.Feature.None)));

            string accountName = await AwsCredentialUtils.GetAwsAccountNameAsync(_awsAccountId);
            await _retryAndBackoffService.RunAsync(() => _apiProvider.OnboardAccount(new AccountModel(_onboardingId, _awsAccountId, accountName, _apiCredentials)));

            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Cloud account created successfully", Enums.Feature.None)));
            Console.WriteLine($"[INFO] Successfully posted onboarding request. Cloud account created successfully.");
        }

        public override Task Rollback()
        {
            //TODO: delete serverless account
            return Task.CompletedTask;
        }
    }
}
