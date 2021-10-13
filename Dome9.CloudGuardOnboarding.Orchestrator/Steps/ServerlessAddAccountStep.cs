using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
      public class ServerlessAddAccountStep : StepBase
    {
        private readonly string _awsAccountId;
        private readonly string _onboardingId;

        public ServerlessAddAccountStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, string awsAccountId, string onboardingId)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _awsAccountId = awsAccountId;
            _onboardingId = onboardingId;
        }
        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }

        public async override Task Execute()
        {
            Console.WriteLine($"[INFO] About to call serverless add account api");
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Adding Serverless protection", Enums.Feature.ServerlessProtection)));
            string accountName = await AwsCredentialUtils.GetAwsAccountNameAsync(_awsAccountId);
            await _retryAndBackoffService.RunAsync(() => _apiProvider.ServerlessAddAccount(new ServelessAddAccountModel(_awsAccountId)));
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.ACTIVE, "Serverless added successfully", Enums.Feature.ServerlessProtection)));
            Console.WriteLine($"[INFO] Serverless add account api call executed successfully");
        }

        public override Task Rollback()
        {
            //TODO: delete cloud account
            return Task.CompletedTask;
        }
    }
}
