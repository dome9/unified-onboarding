using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PermissionsUserBasedStackCreationStep : StepBase
    {
        private readonly PermissionsUserBasedStackWrapper _awsStackWrapper;
        private readonly OnboardingStackConfig _stackConfig;
        private readonly bool _enableRemoteStackModify = false;

        public ApiCredentials AwsUserCredentials { get; private set; }
        public ApiCredentials StackModifyUserCredentials { get; private set; }

        public PermissionsUserBasedStackCreationStep(
            ConfigurationResponseModel configuration,
            OnboardingRequest request,
            int stackExecutionTimeoutMinutes = 35)
        {
            var s3Url = $"https://{request.S3BucketName}.s3.{request.AwsAccountRegion}.{GetDomain(request.AwsPartition)}/{configuration.PermissionsTemplateS3Path}";
            _awsStackWrapper = new PermissionsUserBasedStackWrapper(StackOperation.Create);
            _stackConfig = new PermissionsUserBasedStackConfig(
                s3Url, 
                configuration.PermissionsStackName, 
                request.OnboardingId,       
                request.UniqueSuffix,
                stackExecutionTimeoutMinutes);
            bool.TryParse(request.EnableRemoteStackModify, out _enableRemoteStackModify);
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PermissionsUserBasedStackCreationStep)}.{nameof(Execute)}] RunStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig);

            await SetUserCredentials();

            Console.WriteLine($"[INFO][{nameof(PermissionsUserBasedStackCreationStep)}.{nameof(Execute)}] RunStackAsync finished");
        }

        private async Task SetUserCredentials()
        {
            AwsUserCredentials = await _awsStackWrapper.GetAwsUserCredentials();

            if (_enableRemoteStackModify)
            {
                StackModifyUserCredentials = await _awsStackWrapper.GetStackModifyUserCredentials();
            }
        }

        public override async Task Rollback()
        {
            Console.WriteLine($"[INFO][{nameof(PermissionsUserBasedStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync starting");
            await _awsStackWrapper.DeleteStackAsync(_stackConfig, true);
            Console.WriteLine($"[INFO][{nameof(PermissionsUserBasedStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync finished");
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