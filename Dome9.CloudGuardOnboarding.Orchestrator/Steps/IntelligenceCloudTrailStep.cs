using System;
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
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;

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
        private readonly string unSupportedRegions = "ap-east-1,af-south-1,eu-south-1,me-south-1,cn-north-1,us-gov-east-1,cn-northwest-1,us-gov-west-1,us-isob-east-1,us-iso-east-1,us-iso-west-1";
        private readonly InteligenceStackConfig _stackConfig;
        private readonly int _throttlerMaxCount = 3;
        private readonly string _snsTopicArn;
        private readonly string _s3Url;
        private readonly List<long> _rulesetsIds;
        private readonly StackOperation _stackOperation;

        public IntelligenceCloudTrailStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService,
            string cftS3Buckets, string region, string awsAccountId, string OnboardingId, string roleName, string cloudGuardAwsAccountId,
            string intelligenceTemplateS3Url, string stackName, string snsTopicArn, List<long> rulesetsIds,
            StackOperation stackOperation = StackOperation.Create)
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
            _s3Url = $"https://{cftS3Buckets}.s3.{region}.amazonaws.com/{intelligenceTemplateS3Url}";
            _stackConfig = new InteligenceStackConfig(_s3Url, _intelligenceStackName,_onboardingId, "", _cloudGuardRoleName, 30);
            _snsTopicArn = snsTopicArn;
            _rulesetsIds = rulesetsIds;
            _stackOperation = stackOperation;
        }

        private async Task<AwsCloudTrail> ChoseBucketDetails(List<AwsCloudTrail> trailDetails)
        {
            try
            {
                List<AwsCloudTrail> withoutSubscription =
                    trailDetails.Where(c => c.BucketHasSubscribtions == false).ToList();
                if (withoutSubscription.Count == 0)
                {
                    Console.WriteLine($"[Error] [{nameof(ChoseBucketDetails)}] Event Notification is already configured on S3 Bucket(s) with CloudTrail.");
                    throw new OnboardingException("CloudTrail S3 bucket already has Intelligence running on it", Enums.Feature.Intelligence);
                }

                List<AwsCloudTrail> onlyGlobals = withoutSubscription.Where(c => c.IsMultiRegionTrail == true).ToList();
                if (onlyGlobals.Count > 0) { return onlyGlobals[0]; }

                if (withoutSubscription.Count > 1)
                {
                    await TryUpdateStatusWarning(_stackConfig.OnboardingId, $"{withoutSubscription[0].S3BucketName} was onboarded. Additional S3 Bucket(s) with CloudTrail were found.", Enums.Feature.Intelligence);
                }
                return withoutSubscription[0];
            }
            catch (OnboardingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(ChoseBucketDetails)} failed. Error={ex}]");
                throw new OnboardingException("Failed to choose a bucket for trail", Enums.Feature.Intelligence);
            }
        }

        private async Task<List<AwsCloudTrail>> FindAccountCloudTrails()
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
                    tasks.Add(Task.Run(async () =>
                    {
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
                            Console.WriteLine($"[Error] [{nameof(FindAccountCloudTrails)} failed to get CloudTrail for region {region}. Error={ex}]");
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
                throw new Exception("CloudTrail bucket not found. Please enable CloudTrail on any bucket and onboard Intelligence Cloud Activity from the environment manually");
            }
            return trails;
        }

        private async Task<List<AwsCloudTrail>> FindCloudTrailsStorageDetails(List<AwsCloudTrail> trails)
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
                                var s3RegionRes = await bucketClient.GetBucketLocationAsync(new GetBucketLocationRequest() { BucketName = trail.S3BucketName });
                                // according to aws sdk documentation if the bucket is located in "us-east-1" than the location returned is an empty string
                                var s3Region = s3RegionRes.Location.Value == "" ? "us-east-1" : s3RegionRes.Location.Value;
                                Console.WriteLine($"[INFO] [{nameof(FindCloudTrailsStorageDetails)}] s3Region={s3Region}, trail={trail}");
                                trail.BucketRegion = s3Region;
                                using var bucketClientWithRegion = new AmazonS3Client(RegionEndpoint.GetBySystemName(s3Region));
                                var s3Subscriptions = await bucketClientWithRegion.GetBucketNotificationAsync(new GetBucketNotificationRequest() { BucketName = trail.S3BucketName });
                                trail.BucketHasSubscribtions = s3Subscriptions?.TopicConfigurations?.Count > 0 ? true : false;
                                trail.BucketIsAccessible = true;
                            }
                        }
                        catch (AmazonS3Exception ex)
                        {
                            if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                Console.WriteLine($"[Error] [{nameof(FindCloudTrailsStorageDetails)} Missing access permissions to S3 Bucket with CloudTrail: {trail.S3BucketName}. error: {ex}]");
                            }
                            else
                            {
                                Console.WriteLine($"[Error] [{nameof(FindCloudTrailsStorageDetails)} Failed to get cloud trail bucket. trail={trail.TrailArn}, Error={ex}");
                                throw new Exception($"Failed to get cloud trail bucket. trail={trail.TrailArn}, Error={ex}");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"[Error] [{nameof(FindCloudTrailsStorageDetails)} Failed to get cloud trail bucket. trail={trail}, Error={e}");
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }

            trails = trails.Where(c => c.BucketIsAccessible == true).ToList();
            if (trails.Count == 0)
            {
                await TryUpdateStatusWarning(_stackConfig.OnboardingId, "Could not find any S3 bucket with access permissions.", Enums.Feature.Intelligence);
                throw new Exception("Could not find any S3 bucket with access permissions.");
            }
            return trails;
        }

        private TopicConfiguration GenerateS3BucketFiltersList(string id, string topic, List<AwsS3FilterRule> ruleList)
        {
            return new TopicConfiguration()
            {
                Id = id,
                Topic = topic,
                Events = new List<EventType> { new EventType(EventType.ObjectCreatedPut) },
                Filter = new Filter { S3KeyFilter = new S3KeyFilter { FilterRules = ruleList?.Select(rule => new FilterRule { Name = rule.Name, Value = rule.Value }).ToList() } }
            };
        }

        private async Task<AwsCloudTrail> ChooseCloudTrailToOnboaredIntelligence()
        {
            var trails = await FindAccountCloudTrails();
            var trailsWithBucketDetails = await FindCloudTrailsStorageDetails(trails);
            return await ChoseBucketDetails(trailsWithBucketDetails);
        }

        private async Task PutIntelligenceSubscriptionInBucket(AwsCloudTrail chosenCloudTrail, string topic)
        {
            var bucketClient = new AmazonS3Client(RegionEndpoint.GetBySystemName(chosenCloudTrail.BucketRegion));
            var S3BucketTopicPrefix = $"AWSLogs/{chosenCloudTrail.ExternalId}/CloudTrail";
            var currentRule = new List<AwsS3FilterRule>
            {
                new AwsS3FilterRule("prefix", S3BucketTopicPrefix)
            };
            var existingTopics = new List<TopicConfiguration> { GenerateS3BucketFiltersList($"cloudtrail_{chosenCloudTrail.ExternalId}", topic, currentRule) };
            Console.WriteLine($"start puting notification to bucket: {chosenCloudTrail.S3BucketName}");
            await bucketClient.PutBucketNotificationAsync(new PutBucketNotificationRequest() { BucketName = chosenCloudTrail.S3BucketName, TopicConfigurations = existingTopics });
        }

        public async override Task Execute()
        {
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.PENDING, "Adding Intelligence", Enums.Feature.Intelligence)));

            // find all account cloud trails and get bucket name to subscribe
            var chosenCloudTrail = await ChooseCloudTrailToOnboaredIntelligence();

            // adding subscribtion to bucket
            var topic = _snsTopicArn.Replace("REGION", chosenCloudTrail.HomeRegion);
            await PutIntelligenceSubscriptionInBucket(chosenCloudTrail, topic);

            // create Intelligence policy and attached to dome9 role                                   
            _stackConfig.CloudtrailS3BucketName = chosenCloudTrail.S3BucketName;
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(_onboardingId, "Creating Intelligence stack", Enums.Feature.Intelligence)));
            await _awsStackWrapper.RunStackAsync(_stackConfig, _stackOperation);
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(_onboardingId, "Created Intelligence stack successfully", Enums.Feature.Intelligence)));

            // enable Intelligence account in Dome9
            await _retryAndBackoffService.RunAsync(() => _apiProvider.OnboardIntelligence(new IntelligenceOnboardingModel { BucketName = chosenCloudTrail.S3BucketName, CloudAccountId = _awsAccountId, IsUnifiedOnboarding = true, RulesetsIds = _rulesetsIds }));

            Console.WriteLine($"[INFO] finish Intelligence step..");
            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.ACTIVE, "Added Intelligence successfully", Enums.Feature.Intelligence)));

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
