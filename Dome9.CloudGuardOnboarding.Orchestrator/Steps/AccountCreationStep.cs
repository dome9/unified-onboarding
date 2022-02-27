using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class AccountCreationStep : StepBase
    {
        private readonly string _awsAccountId;
        private readonly string _awsRegion;
        private readonly string _onboardingId;
        private readonly string _stackModifyRoleArn;
        private readonly string _rootStackId;
        private readonly ApiCredentials _apiCredentials;
        private readonly ApiCredentials _stackModifyApiCredentials;
        private readonly string _crossAccountRoleArn;


        public AccountCreationStep(
            string awsAccountId,
            string awsRegion,
            string onboardingId,            
            string stackModifyRoleArn,
            string rootStackId,
            ApiCredentials apiCredentials,
            ApiCredentials stackModifyApiCredentials,
            string crossAccountRoleArn)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _awsAccountId = awsAccountId;
            _awsRegion = awsRegion;
            _onboardingId = onboardingId;
            _stackModifyRoleArn = stackModifyRoleArn;
            _rootStackId = rootStackId;
            _apiCredentials = apiCredentials;
            _stackModifyApiCredentials = stackModifyApiCredentials;
            _crossAccountRoleArn = crossAccountRoleArn;
        }
        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }

        public async override Task Execute()
        {
            Console.WriteLine($"[INFO] About to post onboarding request to create cloud account");
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Creating cloud account"));

            string accountName = await AwsCredentialUtils.GetAwsAccountNameAsync(_awsAccountId);
            await _retryAndBackoffService.RunAsync(() => _apiProvider.OnboardAccount(
                new AccountModel(
                    _onboardingId,
                    _awsAccountId,
                    accountName,
                    _awsRegion,                    
                    _stackModifyRoleArn,
                    _rootStackId,
                    _apiCredentials,
                    _stackModifyApiCredentials,
                    _crossAccountRoleArn)));

            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.None, Enums.Status.PENDING, "Cloud account created successfully"));
            Console.WriteLine($"[INFO] Successfully posted onboarding request. Cloud account created successfully.");
        }

        public override Task Rollback()
        {
            //TODO: delete serverless account
            return Task.CompletedTask;
        }
    }
}
