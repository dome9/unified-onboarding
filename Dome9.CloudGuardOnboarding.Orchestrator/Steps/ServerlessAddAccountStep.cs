﻿using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
      public class ServerlessAddAccountStep : StepBase
    {
        private readonly string _awsAccountId;
        private readonly string _onboardingId;

        public ServerlessAddAccountStep(string awsAccountId, string onboardingId)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
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
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.ServerlessProtection, Enums.Status.PENDING, "Adding Serverless protection"));
            string accountName = await AwsCredentialUtils.GetAwsAccountNameAsync(_awsAccountId);
            await _retryAndBackoffService.RunAsync(() => _apiProvider.ServerlessAddAccount(new ServelessAddAccountModel(_awsAccountId)));
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.ServerlessProtection, Enums.Status.ACTIVE, "Serverless added successfully"));
            Console.WriteLine($"[INFO] Serverless add account api call executed successfully");
        }

        public override Task Rollback()
        {
            //TODO: delete cloud account
            return Task.CompletedTask;
        }
    }
}
