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

        /// <summary>
        /// Find all account cloud trails, get choose bucket to subscribe and add subscribtion to this bucket
        /// </summary>
        /// <param name="snsTopicArn"></param>
        /// <returns></returns>
        public static async Task<string> SubscribeBucket(string snsTopicArn, string awsAccountId)
        {
            // find all account cloud trails and get bucket name to subscribe
            var chosenCloudTrail = await ChooseCloudTrailToOnboardIntelligence(awsAccountId);

            // adding subscribtion to bucket
            var topic = snsTopicArn.Replace("REGION", chosenCloudTrail.HomeRegion);
            await PutIntelligenceSubscriptionInBucket(chosenCloudTrail, topic);
            return chosenCloudTrail.S3BucketName;
        }

        private static async Task PutIntelligenceSubscriptionInBucket(AwsCloudTrail chosenCloudTrail, string topic)
        {
            var bucketClient = new AmazonS3Client(RegionEndpoint.GetBySystemName(chosenCloudTrail.BucketRegion));
            var S3BucketTopicPrefix = $"AWSLogs/{chosenCloudTrail.ExternalId}/CloudTrail";
            var currentRule = new List<AwsS3FilterRule>
            {
                new AwsS3FilterRule("prefix", S3BucketTopicPrefix)
            };
            var existingTopics = new List<TopicConfiguration> { GenerateS3BucketFiltersList($"cloudtrail_{chosenCloudTrail.ExternalId}", topic, currentRule) };
            
            Console.WriteLine($"[INFO] [{nameof(PutIntelligenceSubscriptionInBucket)}] Start putting notification to bucket='{chosenCloudTrail.S3BucketName}'");
            
            await bucketClient.PutBucketNotificationAsync(new PutBucketNotificationRequest() { BucketName = chosenCloudTrail.S3BucketName, TopicConfigurations = existingTopics });
            
            Console.WriteLine($"[INFO] [{nameof(PutIntelligenceSubscriptionInBucket)}] Finished putting notification to bucket='{chosenCloudTrail.S3BucketName}'");
        }


        private static async Task<AwsCloudTrail> ChooseCloudTrailToOnboardIntelligence(string awsAccountId)
        {
            var trails = await FindAccountCloudTrails(awsAccountId);
            var trailsWithBucketDetails = await FindCloudTrailsStorageDetails(trails);
            return ChooseCloudTrail(trailsWithBucketDetails);
        }

        private static AwsCloudTrail ChooseCloudTrail(List<AwsCloudTrail> cloudTrails)
        {
            try
            {
                var withoutSubscription = cloudTrails.Where(c => c.BucketHasSubscriptions == false);
                if (!withoutSubscription.Any())
                {
                    Console.WriteLine($"[ERROR] [{nameof(ChooseCloudTrail)}] Failed. Event Notification is already configured on S3 Bucket(s) with CloudTrail.");
                    throw new OnboardingException("CloudTrail S3 bucket already has event notification configured, cannot configure new notification.", Enums.Feature.Intelligence);
                }

                var onlyGlobals = withoutSubscription.Where(c => c.IsMultiRegionTrail);
                if (onlyGlobals.Any())
                {
                    return onlyGlobals.First();
                }

                if (withoutSubscription.Any())
                {
                    // TODO: Does this message need to get posted to the CG onboarding status api?
                    // If so, we must add more data to the return value of the above public SubscribeBucket method
                    Console.WriteLine($"[WARN] [{nameof(ChooseCloudTrail)}] Bucket '{withoutSubscription.First()?.S3BucketName}' was onboarded. Additional S3 Bucket(s) with CloudTrail were found.");
                }
                return withoutSubscription.First();
            }            
            catch (Exception ex)
            {
                if(ex is OnboardingException)
                {
                    throw;
                }

                Console.WriteLine($"[ERROR] [{nameof(ChooseCloudTrail)}] failed. Error={ex}");
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
                                                                                    ExternalId = awsAccountId
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

        private static async Task<List<AwsCloudTrail>> FindCloudTrailsStorageDetails(List<AwsCloudTrail> trails)
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
                                var s3Region = s3RegionRes.Location.Value == "" ? "us-east-1" : s3RegionRes.Location.Value;
                                Console.WriteLine($"[INFO] [{nameof(FindCloudTrailsStorageDetails)}] s3Region={s3Region}, trail=[{trail}]");
                                trail.BucketRegion = s3Region;
                                using var bucketClientWithRegion = new AmazonS3Client(RegionEndpoint.GetBySystemName(s3Region));
                                var s3Subscriptions = await bucketClientWithRegion.GetBucketNotificationAsync(new GetBucketNotificationRequest() { BucketName = trail.S3BucketName });
                                trail.BucketHasSubscriptions = s3Subscriptions?.TopicConfigurations?.Any() ?? false;
                                trail.BucketIsAccessible = true;
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

    }
}
