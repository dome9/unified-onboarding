﻿using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class AccountCreationStep : StepBase
    {
        private readonly string _awsAccountId;
        private readonly string _awsRegion;
        private readonly string _onboardingId;
        private readonly string _labmdaRoleArn;
        private readonly string _rootStackId;
        private readonly ApiCredentials _apiCredentials;
        private readonly ApiCredentials _lambdaApiCredentials;

        public AccountCreationStep(ICloudGuardApiWrapper apiProvider,
            IRetryAndBackoffService retryAndBackoffService,
            string awsAccountId,
            string awsRegion,
            string onboardingId,            
            string labmdaRoleArn,
            string rootStackId,
            ApiCredentials apiCredentials,
            ApiCredentials lambdaApiCredentials)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _awsAccountId = awsAccountId;
            _awsRegion = awsRegion;
            _onboardingId = onboardingId;
            _labmdaRoleArn = labmdaRoleArn;
            _rootStackId = rootStackId;
            _apiCredentials = apiCredentials;
            _lambdaApiCredentials = lambdaApiCredentials;
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

            await _retryAndBackoffService.RunAsync(() => _apiProvider.OnboardAccount(
                new AccountModel(
                    _onboardingId,
                    _awsAccountId,
                    accountName,
                    _awsRegion,                    
                    _labmdaRoleArn,
                    _rootStackId,
                    _apiCredentials,
                    _lambdaApiCredentials)));

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
