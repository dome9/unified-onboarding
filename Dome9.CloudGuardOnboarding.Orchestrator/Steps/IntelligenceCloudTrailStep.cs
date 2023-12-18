using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackWrapper;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using Dome9.CloudGuardOnboarding.Orchestrator.Intelligence;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class IntelligenceCloudTrailStep : StepBase
    {
        private readonly string _awsAccountId;
        private readonly string _onboardingId;
        private readonly string _cloudGuardRoleName;
        private readonly string _intelligenceStackName;
        private readonly IntelligenceStackConfig _stackConfig;
        private readonly int _throttlerMaxCount = 3;
        private readonly string _s3Url;
        private readonly string _snsTopicName;
        private readonly List<long> _rulesetsIds;
        private readonly StackOperation _stackOperation;
        private readonly string _s3IntelligenceBucketTopicPrefix;
        private static string  _bucketRegion;
        private static string  _uniqueSuffix;
        private static readonly AwsS3.AwsS3 _awsS3 = new AwsS3.AwsS3();

        public IntelligenceCloudTrailStep(
            string cftS3Buckets, 
            string region, 
            string awsAccountId, 
            string OnboardingId, 
            string roleName, 
            string cloudGuardAwsAccountId,
            string intelligenceTemplateS3Url, 
            string stackName, 
            string snsTopicArn, 
            List<long> rulesetsIds, 
            string uniqueSuffix,
            string useAwsReadOnlyPolicy,
            StackOperation stackOperation = StackOperation.Create)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _awsAccountId = awsAccountId;
            _onboardingId = OnboardingId;
            _cloudGuardRoleName = roleName.Contains("readonly") ? "CloudGuard-Connect-RO-role" : "CloudGuard-Connect-RW-role";
            _cloudGuardRoleName += uniqueSuffix;
            _intelligenceStackName = stackName;
            _s3Url = $"https://{cftS3Buckets}.s3.{region}.amazonaws.com/{intelligenceTemplateS3Url}";
            var sqsEndpointAwsAccount = snsTopicArn.Split(":")[4];
            var sqsEndpoint = "arn:aws:sqs:us-east-1:AWSACCOUNT:sns-validator-CloudTrail-input-queue";
            _stackConfig = new IntelligenceStackConfig(_s3Url, _intelligenceStackName,_onboardingId, "", sqsEndpoint.Replace("AWSACCOUNT",sqsEndpointAwsAccount),sqsEndpointAwsAccount,_cloudGuardRoleName, uniqueSuffix, 30, "");
            _rulesetsIds = rulesetsIds;
            _stackOperation = stackOperation;
            _s3IntelligenceBucketTopicPrefix = $"AWSLogs/{awsAccountId}/CloudTrail";
            _snsTopicName = $"Intelligence-Log-Delivery{uniqueSuffix}";
            _bucketRegion = region;
            _uniqueSuffix = uniqueSuffix;
        }

        public async override Task Execute()
        {
            Console.WriteLine($"[INFO] [{nameof(IntelligenceCloudTrailStep)}.{nameof(Execute)}] Starting Intelligence step.");
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.Intelligence, Enums.Status.PENDING, "Adding Intelligence"));
            
            var alredySubscribedViaCentrallizedBucket = await _retryAndBackoffService.RunAsync(() => _apiProvider.IsDome9AccountAlreadySubscribedToCloudtrail( new AwsGetLogDestinationModel(
                _awsAccountId)));
            if (!alredySubscribedViaCentrallizedBucket)
            {
                // choose cloud trail from account (if exist and free)
                var chosenCloudTrail = await IntelligenceBucketHelper.ChooseCloudTrailToOnboaredIntelligence(_awsAccountId, _awsS3, _s3IntelligenceBucketTopicPrefix,_onboardingId);

                // create Intelligence policy and attached to CloudGuard role, sns topic and sns subscription                                  
                _stackConfig.CloudtrailS3BucketName = chosenCloudTrail.S3BucketName;
                _stackConfig.CloudTrailKmsArn = chosenCloudTrail.KmsKeyArn;
                _bucketRegion = chosenCloudTrail.BucketRegion; // case we have error next - rollback delete on relevant region
                var awsStackWrapper = new IntelligenceStackWrapper(StackOperation.Create, _bucketRegion);
                await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.Intelligence, Enums.Status.PENDING, "Creating Intelligence stack"));
                await awsStackWrapper.RunStackAsync(_stackConfig);
                await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.Intelligence, Enums.Status.PENDING, "Created Intelligence stack successfully"));
            
                // adding event notification to bucket
            
                    var topicArn = $"arn:aws:sns:{chosenCloudTrail.BucketRegion}:{_awsAccountId}:{_snsTopicName}";
                    await IntelligenceBucketHelper.PutIntelligenceSubscriptionInBucket(chosenCloudTrail, topicArn, _s3IntelligenceBucketTopicPrefix, _uniqueSuffix);
            

                // enable Intelligence account in CloudGuard
                await _retryAndBackoffService.RunAsync(() => _apiProvider.OnboardIntelligence(new IntelligenceOnboardingModel
                    (chosenCloudTrail.S3BucketName, _awsAccountId, topicArn, new List<string>{_awsAccountId},
                        "CloudTrail",true, _rulesetsIds)
                ));
            
                // update intelligence region stack (maybe will be use on delete/update in the future)
                await _retryAndBackoffService.RunAsync(()=> _apiProvider.UpdateIntelligenceRegion(_onboardingId, _bucketRegion));
            }
            else
            {
                // enable Intelligence to subscribed account in CloudGuard
                await _retryAndBackoffService.RunAsync(() => _apiProvider.OnboardIntelligence(new IntelligenceOnboardingModel
                    (_awsAccountId, "CloudTrail", true, _rulesetsIds, alredySubscribedViaCentrallizedBucket)));
            }

            Console.WriteLine($"[INFO] [{nameof(IntelligenceCloudTrailStep)}.{nameof(Execute)}] Finished Intelligence step.");
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.Intelligence, Enums.Status.ACTIVE, "Added Intelligence successfully"));
        }

        public override async Task Rollback()
        {
            var awsStackWrapper = new IntelligenceStackWrapper(_stackOperation, _bucketRegion);
            Console.WriteLine($"[INFO][{nameof(IntelligenceCloudTrailStep)}.{nameof(Rollback)}] DeleteStackAsync starting");
            // stack may not have been created, try to delete but do not throw in case of failure
            await awsStackWrapper.DeleteStackAsync(_stackConfig, true);
            Console.WriteLine($"[INFO][{nameof(IntelligenceCloudTrailStep)}.{nameof(Rollback)}] DeleteStackAsync finished");
        }

        public override Task Cleanup()
        {
            return Task.CompletedTask;
        }
    }
}
