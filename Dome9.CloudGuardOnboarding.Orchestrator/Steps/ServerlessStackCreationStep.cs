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


        public ServerlessStackCreationStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, string awsAccountId, string onboardingId, string serverlessStackS3Url, string serverlessStackName)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _awsAccountId = awsAccountId;
            _onboardingId = onboardingId;

            var capabilities = new List<string> { "CAPABILITY_IAM", "CAPABILITY_NAMED_IAM", "CAPABILITY_AUTO_EXPAND" };
            _awsStackWrapper = new ServerlessStackWrapper(apiProvider, retryAndBackoffService);
            _stackConfig = new ServerlessStackConfig(serverlessStackS3Url, serverlessStackName, capabilities, onboardingId, 30);

        }
        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }

        public async override Task Execute()
        {
            Console.WriteLine($"[INFO] About to add serverless protection");
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Adding serverless protection", Enums.Feature.ServerlessProtection)));
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(_onboardingId, "Creating serverless protection stack", Enums.Feature.ServerlessProtection)));
            Console.WriteLine($"[INFO][{nameof(ServerlessStackCreationStep)}.{nameof(Execute)}] RunStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig);
            Console.WriteLine($"[INFO][{nameof(ServerlessStackCreationStep)}.{nameof(Execute)}] RunStackAsync finished");
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(_onboardingId, "Created serverless protection stack successfully", Enums.Feature.ServerlessProtection)));
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.ACTIVE, "Added serverless protection successfully", Enums.Feature.ServerlessProtection)));
            Console.WriteLine($"[INFO] Successfully added serverless protection");
        }

        public override Task Rollback()
        {
            //TODO: delete cloud account
            return Task.CompletedTask;
        }
    }
}
