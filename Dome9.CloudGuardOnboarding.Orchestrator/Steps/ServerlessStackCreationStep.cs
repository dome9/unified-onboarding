using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class ServerlessStackCreationStep : StepBase
    {
        private readonly string _onboardingId;
        private readonly ServerlessStackWrapper _awsStackWrapper;
        private readonly ServerlessStackConfig _stackConfig;

        public ServerlessStackCreationStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, 
            string cftS3Buckets, string region,
            string onboardingId, string templateS3Path, string serverlessStackName, string uniqueSuffix,
            string cloudGuardAwsAccountId, string serverlessStage, string serverlessRegion)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _onboardingId = onboardingId;
            _awsStackWrapper = new ServerlessStackWrapper(apiProvider, retryAndBackoffService);
            var s3Url = $"https://{cftS3Buckets}.s3.{region}.amazonaws.com/{templateS3Path}";
            _stackConfig = new ServerlessStackConfig(s3Url, serverlessStackName, onboardingId, uniqueSuffix, 30, cloudGuardAwsAccountId, serverlessStage, serverlessRegion);

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
            await _awsStackWrapper.RunStackAsync(_stackConfig, StackOperation.Create);
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
