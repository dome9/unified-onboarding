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
        private readonly string _cloudGuardAwsAccountId;
        private readonly string _cloudGuardRoleName;
        private readonly string _intelligenceTemplateS3Url;
        private readonly string _intelligenceStackName;
        private readonly IntelligenceStackWrapper _awsStackWrapper;
        private readonly IntelligenceStackConfig _stackConfig;
        private readonly string _snsTopicArn;
        private readonly string _s3Url;
        private readonly List<long> _rulesetsIds;
        private readonly StackOperation _stackOperation;

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
            StackOperation stackOperation = StackOperation.Create)
        {
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _awsAccountId = awsAccountId;
            _onboardingId = OnboardingId;
            _cloudGuardAwsAccountId = cloudGuardAwsAccountId;
            _cloudGuardRoleName = roleName.Contains("readonly") ? "CloudGuard-Connect-RO-role" : "CloudGuard-Connect-RW-role";
            _cloudGuardRoleName += uniqueSuffix;
            _awsStackWrapper = new IntelligenceStackWrapper(StackOperation.Create);
            _intelligenceTemplateS3Url = intelligenceTemplateS3Url;
            _intelligenceStackName = stackName;
            _s3Url = $"https://{cftS3Buckets}.s3.{region}.amazonaws.com/{intelligenceTemplateS3Url}";
            _stackConfig = new IntelligenceStackConfig(_s3Url, _intelligenceStackName,_onboardingId, "", _cloudGuardRoleName, uniqueSuffix, 30);
            _snsTopicArn = snsTopicArn;
            _rulesetsIds = rulesetsIds;
            _stackOperation = stackOperation;
        }

        public async override Task Execute()
        {
            Console.WriteLine($"[INFO] [{nameof(IntelligenceCloudTrailStep)}.{nameof(Execute)}] Starting Intelligence step.");

            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.Intelligence, Enums.Status.PENDING, "Adding Intelligence"));

            // Choose and subscribe bucket to Intelligence
            string chosenCloudTrailS3BucketName = await IntelligenceBucketHelper.SubscribeBucket(_snsTopicArn, _awsAccountId);

            // create Intelligence policy and attached to dome9 role                                   
            _stackConfig.CloudtrailS3BucketName = chosenCloudTrailS3BucketName;
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.Intelligence, Enums.Status.PENDING, "Creating Intelligence stack"));
            await _awsStackWrapper.RunStackAsync(_stackConfig);
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.Intelligence, Enums.Status.PENDING, "Created Intelligence stack successfully"));

            // enable Intelligence account in Dome9
            await _retryAndBackoffService.RunAsync(() => _apiProvider.OnboardIntelligence(new IntelligenceOnboardingModel { BucketName = chosenCloudTrailS3BucketName, CloudAccountId = _awsAccountId, IsUnifiedOnboarding = true, RulesetsIds = _rulesetsIds }));

            Console.WriteLine($"[INFO] [{nameof(IntelligenceCloudTrailStep)}.{nameof(Execute)}] Finished Intelligence step.");
            await StatusHelper.UpdateStatusAsync(new StatusModel(_onboardingId, Enums.Feature.Intelligence, Enums.Status.ACTIVE, "Added Intelligence successfully"));
        }

        public override async Task Rollback()
        {
            Console.WriteLine($"[INFO][{nameof(IntelligenceCloudTrailStep)}.{nameof(Rollback)}] DeleteStackAsync starting");
            // stack may not have been created, try to delete but do not throw in case of failure
            await _awsStackWrapper.DeleteStackAsync(_stackConfig, true);
            Console.WriteLine($"[INFO][{nameof(IntelligenceCloudTrailStep)}.{nameof(Rollback)}] DeleteStackAsync finished");
        }

        public override Task Cleanup()
        {
            return Task.CompletedTask;
        }
    }
}
