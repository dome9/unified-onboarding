﻿using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PermissionsStackUpdateStep : StepBase
    {
        private readonly PermissionsStackWrapper _awsStackWrapper;
        private readonly PermissionsStackConfig _stackConfig;

        public PermissionsStackUpdateStep(
            string cftS3Buckets,
            string region,
            string stackName,
            string templateS3Path,
            string cloudGuardAwsAccountId,
            string cloudGuardExternalTrustId,
            string onboardingId,
            string uniqueSuffix,
            string useAwsReadOnlyPolicy,
            int stackExecutionTimeoutMinutes = 35)
        {
            _awsStackWrapper = new PermissionsStackWrapper(StackOperation.Update);
            var s3Url = $"https://{cftS3Buckets}.s3.{region}.amazonaws.com/{templateS3Path}";
            _stackConfig = new PermissionsStackConfig(
                s3Url,
                stackName,
                onboardingId,
                cloudGuardAwsAccountId,
                cloudGuardExternalTrustId,
                uniqueSuffix,
                useAwsReadOnlyPolicy,
                stackExecutionTimeoutMinutes);
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PermissionsStackCreationStep)}.{nameof(Execute)}] RunUpdateStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig);
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
