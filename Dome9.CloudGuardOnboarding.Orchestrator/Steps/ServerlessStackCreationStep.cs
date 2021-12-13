using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class ServerlessStackCreationStep : StepBase
    {
        private readonly string _awsAccountId;
        private readonly string _onboardingId;
        private readonly ServerlessStackWrapper _awsStackWrapper;
        private readonly ServerlessStackConfig _stackConfig;
        private readonly StackOperation _stackOperation;

        public ServerlessStackCreationStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, string awsAccountId, string onboardingId, string serverlessStackS3Url, string serverlessStackName, string uniqueSuffix, StackOperation stackOperation = StackOperation.Create)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _awsAccountId = awsAccountId;
            _onboardingId = onboardingId;
            _awsStackWrapper = new ServerlessStackWrapper(apiProvider, retryAndBackoffService);
            _stackConfig = new ServerlessStackConfig(serverlessStackS3Url, serverlessStackName, onboardingId, uniqueSuffix, 30);
            _stackOperation = stackOperation;

        }
        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }

        public async override Task Execute()
        {
            Console.WriteLine($"[INFO] About to add serverless protection");
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.ServerlessProtection, Enums.Status.PENDING, "Adding serverless protection", null, null, null)));
            Console.WriteLine($"[INFO][{nameof(ServerlessStackCreationStep)}.{nameof(Execute)}] RunStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig, _stackOperation);
            Console.WriteLine($"[INFO][{nameof(ServerlessStackCreationStep)}.{nameof(Execute)}] RunStackAsync finished");
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(_onboardingId, Enums.Feature.ServerlessProtection, Enums.Status.ACTIVE, "Added serverless protection successfully", null, null, null)));
            Console.WriteLine($"[INFO] Successfully added serverless protection");
        }

        public override Task Rollback()
        {
            //TODO: delete cloud account
            return Task.CompletedTask;
        }
    }
}
