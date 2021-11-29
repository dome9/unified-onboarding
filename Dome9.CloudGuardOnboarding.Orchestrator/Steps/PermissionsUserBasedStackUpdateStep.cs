using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PermissionsUserBasedStackUpdateStep : StepBase
    {
        private readonly PermissionsUserBasedStackWrapper _awsStackWrapper;
        private readonly OnboardingStackConfig _stackConfig;

        public PermissionsUserBasedStackUpdateStep(
            ICloudGuardApiWrapper apiProvider,
            IRetryAndBackoffService retryAndBackoffService,
            string stackName,
            string region,
            string cftS3BucketName,
            string templateS3Path,
            string onboardingId,
            string awsPartition,
            int stackExecutionTimeoutMinutes = 35)
        {
            var s3Url = $"https://{cftS3BucketName}.s3.{region}.{GetDomain(awsPartition)}/{templateS3Path}";
            _awsStackWrapper = new PermissionsUserBasedStackWrapper(apiProvider, retryAndBackoffService);
            _stackConfig = new PermissionsUserBasedStackConfig(s3Url, stackName, onboardingId, stackExecutionTimeoutMinutes);
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PermissionsUserBasedStackCreationStep)}.{nameof(Execute)}] RunUpdateStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig, StackOperation.Update);
            Console.WriteLine($"[INFO][{nameof(PermissionsUserBasedStackCreationStep)}.{nameof(Execute)}] RunUpdateStackAsync finished");
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

        private string GetDomain(string awsPartition)
        {
            /*
               from CFT AllowedValues:
              - aws
              - aws-us-gov
              - aws-cn
             */
            switch (awsPartition)
            {
                case "aws":
                    throw new OnboardingException($"{nameof(PermissionsUserBasedStackCreationStep)} is not valid for roles based onboarding required by partition '{awsPartition}'", Enums.Feature.Permissions);
                case "aws-us-gov":
                    return "amazonaws.com";
                case "aws-cn":
                    return "amazonaws.com.cn";
                default:
                    throw new OnboardingException($"Unsupported partition '{awsPartition}'", Enums.Feature.Permissions);
            }
        }
    }
}
