using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PermissionsStackCreationStep : StepBase
    {
        private readonly PermissionsStackWrapper _awsStackWrapper;
        private readonly PermissionsStackConfig _stackConfig;
        public string CrossAccountRoleArn {get;set;}

        public PermissionsStackCreationStep(
            string cftS3Buckets,
            string region,
            string stackName,
            string templateS3Path,
            string cloudGuardAwsAccountId,
            string cloudGuardExternalTrustId,
            string onboardingId,
            string uniqueSuffix,
            string awsPartition,
            int stackExecutionTimeoutMinutes = 35)
        {
            _awsStackWrapper = new PermissionsStackWrapper(StackOperation.Create);
            var s3Url = GetS3Url(cftS3Buckets, region, templateS3Path, awsPartition);
            _stackConfig = new PermissionsStackConfig(
                s3Url, 
                stackName, 
                onboardingId, 
                cloudGuardAwsAccountId, 
                cloudGuardExternalTrustId,
                uniqueSuffix,
                stackExecutionTimeoutMinutes);
        }

        private string GetS3Url(string cftS3Buckets, string region, string templateS3Path, string awsPartitionName)
        {
            var awsPartition = AwsPartition.GetPartition(awsPartitionName);
            return $"https://{cftS3Buckets}.s3.{region}.{awsPartition.Domain}/{templateS3Path}";
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PermissionsStackCreationStep)}.{nameof(Execute)}] RunCreateStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig);
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