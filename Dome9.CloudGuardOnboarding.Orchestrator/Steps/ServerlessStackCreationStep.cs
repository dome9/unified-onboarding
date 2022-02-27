using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
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

        public ServerlessStackCreationStep(string cftS3Buckets, string region,
            string onboardingId, string templateS3Path, string serverlessStackName, string uniqueSuffix,
            string cloudGuardAwsAccountId, string serverlessStage, string serverlessRegion)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _onboardingId = onboardingId;
            _awsStackWrapper = new ServerlessStackWrapper(StackOperation.Create);
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
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.ServerlessProtection, Enums.Status.PENDING, "Adding serverless protection"));
            Console.WriteLine($"[INFO][{nameof(ServerlessStackCreationStep)}.{nameof(Execute)}] RunStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig);
            Console.WriteLine($"[INFO][{nameof(ServerlessStackCreationStep)}.{nameof(Execute)}] RunStackAsync finished");
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.ServerlessProtection, Enums.Status.ACTIVE, "Added serverless protection successfully"));
            Console.WriteLine($"[INFO] Successfully added serverless protection");
        }

        public override async Task Rollback()
        {
            Console.WriteLine($"[INFO][{nameof(ServerlessStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync starting");
            // stack may not have been created, try to delete but do not throw in case of failure
            await _awsStackWrapper.DeleteStackAsync(_stackConfig, true);
            Console.WriteLine($"[INFO][{nameof(ServerlessStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync finished");
        }
    }
}
