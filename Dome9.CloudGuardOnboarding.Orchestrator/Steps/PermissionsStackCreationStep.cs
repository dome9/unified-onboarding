using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PermissionsStackCreationStep : StepBase
    {
        private readonly PermissionsStackWrapper _awsStackWrapper;
        private readonly PermissionsStackConfig _stackConfig;
        public string CrossAccountRoleArn {get;set;}

        public PermissionsStackCreationStep(
            ICloudGuardApiWrapper apiProvider,
            IRetryAndBackoffService retryAndBackoffService,
            string cftS3Buckets,
            string region,
            string stackName,
            string templateS3Path,
            string cloudGuardAwsAccountId,
            string cloudGuardExternalTrustId,
            string onboardingId,
            string uniqueSuffix,
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
                uniqueSuffix,
                stackExecutionTimeoutMinutes);
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PermissionsStackCreationStep)}.{nameof(Execute)}] RunCreateStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig, StackOperation.Create);
            Console.WriteLine($"[INFO][{nameof(PermissionsStackCreationStep)}.{nameof(Execute)}] RunCreateStackAsync finished");

            var stack = await _awsStackWrapper.DescribeStackAsync(Enums.Feature.Permissions, _stackConfig.StackName);
            CrossAccountRoleArn = stack.Outputs.FirstOrDefault(o => o.OutputKey == "CrossAccountRoleArn").OutputValue;
        }

        public override async Task Rollback()
        {
            Console.WriteLine($"[INFO][{nameof(PermissionsStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync starting");
            await _awsStackWrapper.DeleteStackAsync(_stackConfig, true);
            Console.WriteLine($"[INFO][{nameof(PermissionsStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync finished");

        }

        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }
    }
}