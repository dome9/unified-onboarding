﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Amazon.CloudTrail;
using Amazon.CloudTrail.Model;
using Amazon.S3;
using Amazon.S3.Model;
using RegionEndpoint = Amazon.RegionEndpoint;
using Falconetix.Model.Entities.Cloud.Trail;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackWrapper;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig;

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
        private List<string> _capabilities;
        private readonly string unSupportedRegions = "ap-east-1,af-south-1,eu-south-1,me-south-1,cn-north-1,us-gov-east-1,cn-northwest-1,us-gov-west-1,us-isob-east-1,us-iso-east-1,us-iso-west-1";
        private readonly InteligenceStackConfig _stackConfig;
        private readonly int _throttlerMaxCount = 3;
        private readonly string _snsTopicArn;
        private readonly string _s3Url;

        public IntelligenceCloudTrailStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, 
            string cftS3Buckets, string region, string awsAccountId, string OnboardingId, string roleName, string cloudGuardAwsAccountId, 
            string intelligenceTemplateS3Url, string stackName, string snsTopicArn)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _awsAccountId = awsAccountId;
            _onboardingId = OnboardingId;
            _cloudGuardAwsAccountId = cloudGuardAwsAccountId;
            _cloudGuardRoleName = roleName.Contains("readonly") ? "CloudGuard-Connect-RO-role" : "CloudGuard-Connect-RW-role";
            _awsStackWrapper = new IntelligenceStackWrapper(apiProvider, retryAndBackoffService);
            _intelligenceTemplateS3Url = intelligenceTemplateS3Url;
            _intelligenceStackName = stackName;
            _capabilities = new List<string> { "CAPABILITY_IAM", "CAPABILITY_NAMED_IAM", "CAPABILITY_AUTO_EXPAND" };
            _s3Url = $"https://{cftS3Buckets}.s3.{region}.amazonaws.com/{intelligenceTemplateS3Url}";
            _stackConfig = new InteligenceStackConfig(_s3Url, _intelligenceStackName, _capabilities, _onboardingId, "", _cloudGuardRoleName, 30);
            _snsTopicArn = snsTopicArn;
        }

        public async Task<AwsCloudTrail> ChoseBucketDetails(List<AwsCloudTrail> trailDetails)
        {
            try
            {
                List<AwsCloudTrail> withoutSubscription =
                    trailDetails.Where(c => c.BuckethasSubscribtions == false).ToList();
                if (withoutSubscription.Count == 0)
                {
                    throw new Exception($"[Error] [{nameof(ChoseBucketDetails)}] Event Notification is already configured on S3 Bucket(s) with CloudTrail.");
                }               

                List<AwsCloudTrail> onlyGlobals = withoutSubscription.Where(c => c.IsMultiRegionTrail == true).ToList();
                if (onlyGlobals.Count > 0) { return onlyGlobals[0]; }

                if (withoutSubscription.Count > 1)
                {
                    await TryUpdateStatus(_stackConfig.OnboardingId, $"{withoutSubscription[0].S3BucketName} was onboarded. Additional S3 Bucket(s) with CloudTrail were found.", Enums.Feature.Intelligence);
                }
                return withoutSubscription[0];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(ChoseBucketDetails)} failed. Error={ex}]");
                throw ex;
            }
        }

        public async Task<List<AwsCloudTrail>> FindAccountCloudTrails()
        {
            var allRegion = RegionEndpoint.EnumerableAllRegions.Select(t => t.SystemName).ToList();
            var searchRegions = allRegion.Except(unSupportedRegions.Split(',')).ToList();            
            var trails = new List<AwsCloudTrail>();
            var tasks = new List<Task>();
            using (SemaphoreSlim throttler = new SemaphoreSlim(_throttlerMaxCount))
            {
                foreach (var region in searchRegions)
                {
                    await throttler.WaitAsync();
                    tasks.Add(Task.Run(async () => {
                        try
                        {                                                        
                            var awsCloudTrailClient = new AmazonCloudTrailClient(RegionEndpoint.GetBySystemName(region));                           
                            var request = new DescribeTrailsRequest()
                            {
                                IncludeShadowTrails = false,
                                TrailNameList = new List<string>()
                            };
                            var trailsDirectlly = await awsCloudTrailClient.DescribeTrailsAsync(request);
                            trails.AddRange(trailsDirectlly.TrailList.Select(t => new AwsCloudTrail { HomeRegion = t.HomeRegion, S3BucketName = t.S3BucketName, IsMultiRegionTrail = t.IsMultiRegionTrail, TrailArn = t.TrailARN, ExternalId = _awsAccountId }).ToList());
                        }
                        catch (Exception ex)
                        {                                                        
                            Console.WriteLine($"[Error] [{nameof(FindAccountCloudTrails)} failed for region: {region}. Error={ex}]");
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            }            
            trails.RemoveAll(item => item == null);
            trails = trails.GroupBy(x => x.TrailArn).Select(x => x.First()).ToList();
            if (!trails.Any())
            {
                await TryUpdateStatus(_stackConfig.OnboardingId, "CloudTrail is not enabled", Enums.Feature.Intelligence);
                throw new Exception("CloudTrail is not enabled");
            }
            return trails;
        }

        public async Task<List<AwsCloudTrail>> FindCloudTrailsStorageDetails(List<AwsCloudTrail> trails)
        {
            var tasks = new List<Task>();
            using (SemaphoreSlim throttler = new SemaphoreSlim(_throttlerMaxCount))
            {
                foreach (var trail in trails)
                {
                    await throttler.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using (var bucketClient = new AmazonS3Client())
                            {                                
                                var s3Region = await bucketClient.GetBucketLocationAsync(new GetBucketLocationRequest() { BucketName = trail.S3BucketName });
                                trail.BucketRegion = s3Region.Location.Value;
                                using var bucketClientWithRegion = new AmazonS3Client(RegionEndpoint.GetBySystemName(s3Region.Location.Value));
                                var s3Subscriptions = await bucketClientWithRegion.GetBucketNotificationAsync(new GetBucketNotificationRequest() { BucketName = trail.S3BucketName });
                                trail.BuckethasSubscribtions = s3Subscriptions?.TopicConfigurations?.Count > 0 ? true : false;
                            }                           
                        }
                        finally
                        {
                            throttler.Release();
                        }                       
                    }));
                }
                await Task.WhenAll(tasks);
            }            
            return trails;
        }

        public TopicConfiguration GenerateS3BucketFiltersList(string id, string topic, List<AwsS3FilterRule> ruleList)
        {
            return new TopicConfiguration()
            {
                Id = id,
                Topic = topic,
                Events = new List<EventType> { new EventType(EventType.ObjectCreatedPut) },
                Filter = new Filter { S3KeyFilter = new S3KeyFilter { FilterRules = ruleList?.Select(rule => new FilterRule { Name = rule.Name, Value = rule.Value }).ToList() } }
            };
        }

        public async Task<AwsCloudTrail> ChooseCloudTrailToOnboaredIntelligence()
        {
            var trails = await FindAccountCloudTrails();
            var trailsWithBucketDetails = await FindCloudTrailsStorageDetails(trails);
            return await ChoseBucketDetails(trailsWithBucketDetails);
        }

        public async Task PutIntelligenceSubscriptionInBucket(AwsCloudTrail chosenCloudTrail, string topic)
        {
            var bucketClient = new AmazonS3Client(RegionEndpoint.GetBySystemName(chosenCloudTrail.BucketRegion));
            var S3BucketTopicPrefix = $"AWSLogs/{chosenCloudTrail.ExternalId}/CloudTrail";
            var currentRule = new List<AwsS3FilterRule>
            {
                new AwsS3FilterRule("prefix", S3BucketTopicPrefix)
            };
            var existingTopics = new List<TopicConfiguration> { GenerateS3BucketFiltersList($"cloudtrail_{chosenCloudTrail.ExternalId}", topic, currentRule) };
            Console.WriteLine("start puting notification to bucket");
            await bucketClient.PutBucketNotificationAsync(new PutBucketNotificationRequest() { BucketName = chosenCloudTrail.S3BucketName, TopicConfigurations = existingTopics });
        }

        public async override Task Execute()
        {
            // find all account cloud trails and get bucket name to subscribe
            var chosenCloudTrail = await ChooseCloudTrailToOnboaredIntelligence();

            // adding subscribtion to bucket
            var topic = _snsTopicArn.Replace("REGION", chosenCloudTrail.HomeRegion);
            await PutIntelligenceSubscriptionInBucket(chosenCloudTrail, topic);

            // create Intelligence policy and attached to dome9 role                                   
            _stackConfig.CloudtrailS3BucketName = chosenCloudTrail.S3BucketName;
            await _awsStackWrapper.RunStackAsync(_stackConfig);           

            // enable Intelligence account in Dome9
            await _retryAndBackoffService.RunAsync(() => _apiProvider.OnboardIntelligence(new MagellanOnboardingModel { BucketName = chosenCloudTrail.S3BucketName, CloudAccountId = _awsAccountId, IsUnifiedOnboarding = true }));

            Console.WriteLine($"[INFO] finish LIntelligence step..");

        }

        public override async Task Rollback()
        {          
            try
            {
                Console.WriteLine($"[INFO][{nameof(IntelligenceCloudTrailStep)}.{nameof(Rollback)}] DeleteStackAsync starting");
                await TryUpdateStatus(_stackConfig.OnboardingId, "Deleting Intelligence stack", Enums.Feature.Intelligence);

                // stack may not have been created, try to delete but do not throw in case of failure
                await _awsStackWrapper.DeleteStackAsync(_stackConfig);
                await TryUpdateStatus(_stackConfig.OnboardingId, "Deleted Intelligence stack", Enums.Feature.Intelligence);
                Console.WriteLine($"[INFO][{nameof(IntelligenceCloudTrailStep)}.{nameof(Rollback)}] DeleteStackAsync finished");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rollback failed, however stack may not have been created. Check exception to verify. Error:{ex}");
                await TryUpdateStatusError(_stackConfig.OnboardingId, "Rollback Intelligence stack failed", Enums.Feature.Intelligence);
            }
        }

        public override Task Cleanup()
        {
            // TODO: delete resources if necessary
            return Task.CompletedTask;
        }
    }
}