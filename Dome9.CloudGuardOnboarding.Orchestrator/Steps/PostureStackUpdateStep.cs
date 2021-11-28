using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PostureStackUpdateStep : StepBase
    {
        private readonly PostureStackWrapper _awsStackWrapper;
        private readonly PostureStackConfig _stackConfig;

        public PostureStackUpdateStep(
            ICloudGuardApiWrapper apiProvider,
            IRetryAndBackoffService retryAndBackoffService,
            string cftS3Buckets,
            string region,
            string stackName,
            string templateS3Path,
            string cloudGuardAwsAccountId,
            string cloudGuardExternalTrustId,
            string onboardingId,
            int stackExecutionTimeoutMinutes = 35)
        {
            _awsStackWrapper = new PostureStackWrapper(apiProvider, retryAndBackoffService);
            var s3Url = $"https://{cftS3Buckets}.s3.{region}.amazonaws.com/{templateS3Path}";
            _stackConfig = new PostureStackConfig(
                s3Url,
                stackName,
                onboardingId,
                cloudGuardAwsAccountId,
                cloudGuardExternalTrustId,
                stackExecutionTimeoutMinutes);
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PostureStackCreationStep)}.{nameof(Execute)}] RunUpdateStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig, StackOperation.Update);
            Console.WriteLine($"[INFO][{nameof(PostureStackCreationStep)}.{nameof(Execute)}] RunUpdateStackAsync finished");
        }

        public override Task Rollback()
        {
            return Task.CompletedTask;
        }

        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }
    }
}
