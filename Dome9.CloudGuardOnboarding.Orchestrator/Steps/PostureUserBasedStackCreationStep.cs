﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PostureUserBasedStackCreationStep : StepBase
    {
        private readonly PostureUserBasedStackWrapper _awsStackWrapper;
        private readonly OnboardingStackConfig _stackConfig;
        private readonly bool _enableRemoteStackModify = false;

        public ApiCredentials AwsUserCredentials { get; private set; }
        public ApiCredentials LambdaUserCredentials { get; private set; }

        public PostureUserBasedStackCreationStep(
            ICloudGuardApiWrapper apiProvider,
            IRetryAndBackoffService retryAndBackoffService,
            string stackName,
            string region,
            string cftS3BucketName,
            string templateS3Path,
            string onboardingId,
            string awsPartition,
            string enableRemoteStackModify,
            int stackExecutionTimeoutMinutes = 35)
        {
            var s3Url = $"https://{cftS3BucketName}.s3.{region}.{GetDomain(awsPartition)}/{templateS3Path}";
            _awsStackWrapper = new PostureUserBasedStackWrapper(apiProvider, retryAndBackoffService);
            _stackConfig = new PostureUserBasedStackConfig(
                s3Url, 
                stackName, 
                onboardingId,               
                stackExecutionTimeoutMinutes);

            bool.TryParse(enableRemoteStackModify, out _enableRemoteStackModify);
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PostureUserBasedStackCreationStep)}.{nameof(Execute)}] RunStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig);

            await SetUserCredentials();

            Console.WriteLine($"[INFO][{nameof(PostureUserBasedStackCreationStep)}.{nameof(Execute)}] RunStackAsync finished");
        }

        private async Task SetUserCredentials()
        {
            AwsUserCredentials = await _awsStackWrapper.GetAwsUserCredentials();

            if (_enableRemoteStackModify)
            {
                LambdaUserCredentials = await _awsStackWrapper.GetStackModifyUserCredentials();
            }
        }

        public override async Task Rollback()
        {
            Console.WriteLine($"[INFO][{nameof(PostureUserBasedStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync starting");
            await _awsStackWrapper.DeleteStackAsync(_stackConfig, true);
            Console.WriteLine($"[INFO][{nameof(PostureUserBasedStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync finished");
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
                    throw new OnboardingException($"{nameof(PostureUserBasedStackCreationStep)} is not valid for roles based onboarding required by partition '{awsPartition}'", Enums.Feature.ContinuousCompliance);
                case "aws-us-gov":
                    return "amazonaws.com";
                case "aws-cn":
                    return "amazonaws.com.cn";
                default:
                    throw new OnboardingException($"Unsupported partition '{awsPartition}'", Enums.Feature.ContinuousCompliance);
            }
        }
    }
}