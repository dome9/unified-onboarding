using Amazon;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Amazon.CloudTrail;
using Amazon.CloudTrail.Model;
using Amazon.S3.Model;
using RegionEndpoint = Amazon.RegionEndpoint;
using System.Linq;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Intelligence
{
    public static class IntelligenceBucketHelper
    {
        private static readonly string[] UnsupportedRegions = new[] {
            "ap-east-1" ,
            "af-south-1" ,
            "eu-south-1" ,
            "me-south-1" ,
            "cn-north-1" ,
            "us-gov-east-1" ,
            "cn-northwest-1" ,
            "us-gov-west-1" ,
            "us-isob-east-1," ,
            "us-iso-east-1" ,
            "us-iso-west-1" 
        };

        private const int MAX_PARALLEL_AWS_CLIENT_REQUESTS = 3;
        
        public static async Task PutIntelligenceSubscriptionInBucket(AwsCloudTrail chosenCloudTrail, string topicArn, string s3IntelligenceBucketTopicPrefix, string suffix)
        {
            var bucketClient = new AmazonS3Client(RegionEndpoint.GetBySystemName(chosenCloudTrail.BucketRegion));
            var chosenPerfix = ChooseOptimizedPrefix(chosenCloudTrail, s3IntelligenceBucketTopicPrefix);
            var currentRule = new List<AwsS3FilterRule> { new AwsS3FilterRule("prefix", chosenPerfix) };
            var topicConfiguration = new List<TopicConfiguration> { GenerateS3BucketFiltersList($"cloudtrail_{chosenCloudTrail.ExternalId}_{suffix}", topicArn, currentRule) };
            if (chosenCloudTrail.BucketTopicConfiguration.Any())
            {
                // so we will not delete existing topic configuration on topic
                topicConfiguration.AddRange(chosenCloudTrail.BucketTopicConfiguration); 
            }

            Console.WriteLine($"[INFO] [{nameof(PutIntelligenceSubscriptionInBucket)}] Start putting notification to bucket='{chosenCloudTrail.S3BucketName}'");
            await bucketClient.PutBucketNotificationAsync(new PutBucketNotificationRequest() { BucketName = chosenCloudTrail.S3BucketName, TopicConfigurations = topicConfiguration, QueueConfigurations = chosenCloudTrail.BucketQueueConfiguration, LambdaFunctionConfigurations = chosenCloudTrail.BucketLambdaConfiguration});
            Console.WriteLine($"[INFO] [{nameof(PutIntelligenceSubscriptionInBucket)}] Finished putting notification to bucket='{chosenCloudTrail.S3BucketName}'");
            
        }

        public static async Task<AwsCloudTrail> ChooseCloudTrailToOnboaredIntelligence(string awsAccountId, AwsS3.AwsS3 awsS3, string s3IntelligenceBucketTopicPrefix, string onboardingId)
        {
            var trails = await FindAccountCloudTrails(awsAccountId);
            var trailsWithBucketDetails = await FindCloudTrailsStorageDetails(trails, awsS3);
            return await ChoseBucketDetails(trailsWithBucketDetails, s3IntelligenceBucketTopicPrefix, onboardingId);
        }
        
        private static async Task<AwsCloudTrail> ChoseBucketDetails(List<AwsCloudTrail> trailDetails, string s3IntelligenceBucketTopicPrefix, string onboardingId)
        {
            try
            {
                List<AwsCloudTrail> availableCloudTrailBucketsForOnboarding = GetAvailableCloudTrailBucketsForOnboarding(trailDetails, s3IntelligenceBucketTopicPrefix);

                if (!availableCloudTrailBucketsForOnboarding.Any())
                {
                    Console.WriteLine($"[ERROR] [{nameof(ChoseBucketDetails)}] Failed. Event Notification is already configured on S3 Bucket(s) with CloudTrail.");
                    throw new OnboardingException("Event Notification is already configured on S3 Bucket(s) with CloudTrail, cannot configure new notification.", Enums.Feature.Intelligence);
                }

                var onlyGlobals = availableCloudTrailBucketsForOnboarding.Where(c => c.IsMultiRegionTrail);
                if (onlyGlobals.Any())
                {
                    return onlyGlobals.First();
                }

                if (availableCloudTrailBucketsForOnboarding.Count > 1)
                {
                    Console.WriteLine($"[WARN] [{nameof(ChoseBucketDetails)}] Bucket '{availableCloudTrailBucketsForOnboarding.First()?.S3BucketName}' was onboarded. Additional S3 Bucket(s) with CloudTrail were found.");
                    await StatusHelper.UpdateStatusAsync(new StatusModel(onboardingId, Enums.Feature.Intelligence, Enums.Status.WARNING, $"[WARN] [{nameof(ChoseBucketDetails)}] Bucket '{availableCloudTrailBucketsForOnboarding.First()?.S3BucketName}' was onboarded. Additional S3 Bucket(s) with CloudTrail were found."));
                }
                return availableCloudTrailBucketsForOnboarding[0];
            }
            catch (Exception ex)
            {
                if(ex is OnboardingException)
                {
                    throw;
                }

                Console.WriteLine($"[ERROR] [{nameof(ChoseBucketDetails)}] failed. Error={ex}");
                throw new OnboardingException("Failed to choose a bucket for trail", Enums.Feature.Intelligence);
            }
        }
        
        private static async Task<List<AwsCloudTrail>> FindAccountCloudTrails(string awsAccountId)
        {
            var allRegion = RegionEndpoint.EnumerableAllRegions.Select(t => t.SystemName);
            var searchRegions = allRegion.Except(UnsupportedRegions);
            var trails = new List<AwsCloudTrail>();
            var tasks = new List<Task>();
            using (SemaphoreSlim throttler = new SemaphoreSlim(MAX_PARALLEL_AWS_CLIENT_REQUESTS))
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
                            var trailDescriptions = await awsCloudTrailClient.DescribeTrailsAsync(request);
                            trails.AddRange(trailDescriptions.TrailList.Select(t =>
                                                                                new AwsCloudTrail
                                                                                {
                                                                                    HomeRegion = t.HomeRegion,
                                                                                    S3BucketName = t.S3BucketName,
                                                                                    IsMultiRegionTrail = t.IsMultiRegionTrail,
                                                                                    TrailArn = t.TrailARN,
                                                                                    ExternalId = awsAccountId,
                                                                                    KmsKeyArn = t.KmsKeyId
                                                                                }));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[WARN] [{nameof(FindAccountCloudTrails)}] failed to get CloudTrail for region '{region}'. Error={ex}");
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
                string error = "CloudTrail bucket not found. Please enable CloudTrail on any bucket and onboard Intelligence Cloud Activity from the environment manually.";
                Console.WriteLine($"[WARN] [{nameof(FindAccountCloudTrails)}] Failed. {error}");
                throw new OnboardingException(error, Enums.Feature.Intelligence);
            }

            return trails;
        }

        private static async Task<List<AwsCloudTrail>> FindCloudTrailsStorageDetails(List<AwsCloudTrail> trails, AwsS3.AwsS3 awsS3)
        {
            var tasks = new List<Task>();
            using (SemaphoreSlim throttler = new SemaphoreSlim(MAX_PARALLEL_AWS_CLIENT_REQUESTS))
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
                                trail.BucketRegion = s3RegionRes.Location.Value == "" ? "us-east-1" : s3RegionRes.Location.Value;
                                Console.WriteLine($"[INFO] [{nameof(FindCloudTrailsStorageDetails)}] s3Region={trail.BucketRegion}, trail=[{trail}]");
                                using var bucketClientWithRegion = new AmazonS3Client(RegionEndpoint.GetBySystemName(trail.BucketRegion));
                                var s3Subscriptions = await bucketClientWithRegion.GetBucketNotificationAsync(new GetBucketNotificationRequest() { BucketName = trail.S3BucketName });
                                trail.BucketIsAccessible = true;
                                trail.BucketEventNotification = s3Subscriptions?.TopicConfigurations?.Select(topic => awsS3.CreateS3EventNotifications(topic)).ToList();
                                trail.BucketTopicConfiguration = s3Subscriptions?.TopicConfigurations;
                                trail.BucketLambdaConfiguration = s3Subscriptions?.LambdaFunctionConfigurations;
                                trail.BucketQueueConfiguration = s3Subscriptions?.QueueConfigurations;
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            string additionalDetails = string.Empty;

                            if (ex is AmazonS3Exception && (ex as AmazonS3Exception).StatusCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                additionalDetails = $", missing access permissions to S3 Bucket with CloudTrail='{trail.S3BucketName}'";
                            }

                            Console.WriteLine($"[WARN] [{nameof(FindCloudTrailsStorageDetails)}] Failed to get cloud trail bucket, trail=[{trail}]{additionalDetails}, Error={ex}");
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }

            trails = trails.Where(c => c.BucketIsAccessible).ToList();

            if (!trails.Any())
            {
                string error = "Could not find any S3 bucket with access permissions";
                Console.WriteLine($"[WARN] [{nameof(FindCloudTrailsStorageDetails)}] Failed. {error}");
                throw new OnboardingException(error, Enums.Feature.Intelligence);
            }
            return trails;
        }
        
        private static TopicConfiguration GenerateS3BucketFiltersList(string id, string topic, List<AwsS3FilterRule> ruleList)
        {
            return new TopicConfiguration()
            {
                Id = id,
                Topic = topic,
                Events = new List<EventType> { new EventType(EventType.ObjectCreatedPut) },
                Filter = new Filter { S3KeyFilter = new S3KeyFilter { FilterRules = ruleList?.Select(rule => new FilterRule { Name = rule.Name, Value = rule.Value }).ToList() } }
            };
        }

        private static List<AwsCloudTrail> GetAvailableCloudTrailBucketsForOnboarding(List<AwsCloudTrail> trailDetails, string s3IntelligenceBucketTopicPrefix)
        {
            List<AwsCloudTrail> withoutSubscription = new List<AwsCloudTrail>();
            foreach (var trail in trailDetails)
            {
                var isEmptyPrefixSubscriptionTaken = true;
                var isCloudtrailPrefixSubscriptionTaken = true;

                // check if the cloudtrail bucket is available for intelligence onboarding.
                // available means - not have notification of s3:ObjectCreated:Put | s3:ObjectCreated:* with empty prefix or minimum the specific prefix of the cloudtrail logs of the account
                foreach (var notification in trail.BucketEventNotification)
                {
                    if (notification.EventTypes.Any(eventType => eventType == new EventType("s3:ObjectCreated:Put") || eventType == new EventType("s3:ObjectCreated:*")))
                    {
                        if (notification.FilterRules.Any(filterRule => filterRule.Name.ToLower() == "prefix" && (filterRule.Value == "")))
                        {
                            isEmptyPrefixSubscriptionTaken = false;
                        }
                        else if (notification.FilterRules.Any(filterRule => filterRule.Name.ToLower() == "prefix" && 
                        (filterRule.Value.StartsWith(s3IntelligenceBucketTopicPrefix) || s3IntelligenceBucketTopicPrefix.StartsWith(filterRule.Value))))
                        {
                            isCloudtrailPrefixSubscriptionTaken = false;
                        }        
                    }
                }
                
                // order the buckets from the empty prefixed to the specific ones (if there is at all)
                if (isEmptyPrefixSubscriptionTaken)
                {
                    withoutSubscription.Insert(0, trail);
                }
                else if (isCloudtrailPrefixSubscriptionTaken)
                {
                    withoutSubscription.Add(trail);
                }
            }
            return withoutSubscription;
        }

        private static string ChooseOptimizedPrefix(AwsCloudTrail chosenCloudTrail, string s3IntelligenceBucketTopicPrefix)
        {
            foreach (var notification in chosenCloudTrail.BucketEventNotification)
            {
                if (notification.EventTypes.Any(eventType => eventType == new EventType("s3:ObjectCreated:Put") || eventType == new EventType("s3:ObjectCreated:*")))
                {
                    // if we have put or * with same prefix we cant create what we need for intelligence in this bucket
                    var notificationWithIntelligencePrefix = notification.FilterRules.Any(filterRule => filterRule.Name.ToLower() == "prefix" && (filterRule.Value == ""));
                    if (notificationWithIntelligencePrefix)
                    {
                        return s3IntelligenceBucketTopicPrefix;
                    }
                }
            }
            return "";
        }
    }
}
