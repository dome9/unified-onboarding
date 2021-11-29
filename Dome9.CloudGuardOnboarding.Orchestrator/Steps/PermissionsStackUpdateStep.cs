using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PermissionsStackUpdateStep : StepBase
    {
        private readonly PermissionsStackWrapper _awsStackWrapper;
        private readonly PermissionsStackConfig _stackConfig;

        public PermissionsStackUpdateStep(
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
            _awsStackWrapper = new PermissionsStackWrapper(apiProvider, retryAndBackoffService);
            var s3Url = $"https://{cftS3Buckets}.s3.{region}.amazonaws.com/{templateS3Path}";
            _stackConfig = new PermissionsStackConfig(
                s3Url,
                stackName,
                onboardingId,
                cloudGuardAwsAccountId,
                cloudGuardExternalTrustId,
                stackExecutionTimeoutMinutes);
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PermissionsStackCreationStep)}.{nameof(Execute)}] RunUpdateStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig, StackOperation.Update);
            Console.WriteLine($"[INFO][{nameof(PermissionsStackCreationStep)}.{nameof(Execute)}] RunUpdateStackAsync finished");
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
