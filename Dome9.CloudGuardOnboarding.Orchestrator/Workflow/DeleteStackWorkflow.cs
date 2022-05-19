using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Dome9.CloudGuardOnboarding.Orchestrator.Steps;
using RegionEndpoint = Amazon.RegionEndpoint;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class DeleteStackWorkflow : IWorkflow
    {
        // allow 3 minutes for postback of custom resource "success"
        private const int FLOW_TIMEOUT_UNTIL_POSTBACK_MINUTES = 12;
        private const int DELETE_TIMEOUT_MINUTES = 10;
        private const int MAX_PARALLEL_AWS_CLIENT_REQUESTS = 3;

        private readonly OnboardingType _onboardingType;
        private static readonly string[] intelligenceUnsupportedRegions = new[] {
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
        public DeleteStackWorkflow(bool isUserBased)
        {
            _onboardingType = isUserBased ? OnboardingType.UserBased : OnboardingType.RoleBased;
        }

        public async Task RunAsync(CloudFormationRequest cloudFormationRequest, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            var request = cloudFormationRequest.ResourceProperties;
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                var cfnWrapper = CloudFormationWrapper.Get();
                try
                {
                    tokenSource.CancelAfter(TimeSpan.FromMinutes(FLOW_TIMEOUT_UNTIL_POSTBACK_MINUTES));
                    var stack = await cfnWrapper.GetStackDescriptionAsync(Enums.Feature.None, "CloudGuard-Onboarding" + request.UniqueSuffix);
                    var stackVersion = stack.Parameters.FirstOrDefault(p => p.ParameterKey == nameof(OnboardingRequest.Version))?.ParameterValue;
                    if (stackVersion != request.Version)
                    {
                        Console.WriteLine($"[{nameof(DeleteStackWorkflow)}.{nameof(RunAsync)}] Request version is different then the stack version, this is because of an update, will not delete chile stacks. RequestVersion={request.Version}, StackVewrsion={stackVersion}.");
                        return;
                    }

                    var stacks = ChildStacksConfig.GetSupportedFeaturesStackNames(_onboardingType);
                    if (_onboardingType == OnboardingType.RoleBased)
                    {
                        var tasks = new List<Task>();
                        tasks.Add(Task.Run(async () => { await DeleteStackIfExistsAsync(Enums.Feature.ServerlessProtection, stacks[Enums.Feature.ServerlessProtection] + request.UniqueSuffix, cfnWrapper); }, tokenSource.Token));
                        tasks.Add(Task.Run(async () => {
                            await DeleteIntelligenceStackIfExistsAsync(Enums.Feature.Intelligence, stacks[Enums.Feature.Intelligence] + request.UniqueSuffix, request.OnboardingId);
                            await DeleteStackIfExistsAsync(Enums.Feature.Permissions, stacks[Enums.Feature.Permissions] + request.UniqueSuffix, cfnWrapper);
                        }, tokenSource.Token));
                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        await DeleteStackIfExistsAsync(Enums.Feature.Permissions, stacks[Enums.Feature.Permissions] + request.UniqueSuffix, cfnWrapper);
                    }
                    Console.WriteLine($"[{nameof(DeleteStackWorkflow)}.{nameof(RunAsync)}][INFO] All delete tasks are complete");
                }
                catch (AggregateException ex)
                {
                    Console.WriteLine($"[{nameof(DeleteStackWorkflow)}.{nameof(RunAsync)}][ERROR] Caught AggregateException={ex}");

                    List<string> failedStacksNames = new List<string>();
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        if (innerEx is OnboardingDeleteStackException)
                        {
                            var e = innerEx as OnboardingDeleteStackException;
                            failedStacksNames.Add(e?.StackName);
                            Console.WriteLine($"[{nameof(DeleteStackWorkflow)}.{nameof(RunAsync)}][ERROR] Feature='{e?.Feature}', StackName='{e?.StackName}', Ex={e?.Message}");
                        }
                        else
                        {
                            Console.WriteLine($"[{nameof(DeleteStackWorkflow)}.{nameof(RunAsync)}][ERROR] Ex={innerEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{nameof(DeleteStackWorkflow)}.{nameof(RunAsync)}][ERROR] Caught Exception={ex}");
                }
                finally
                {
                    await customResourceResponseHandler.PostbackSuccess();
                }
            }
        }

        private async Task DeleteStackIfExistsAsync(Enums.Feature feature, string stackName, ICloudFormationWrapper cfnWrapper)
        {
            Console.WriteLine($"[INFO][{nameof(DeleteStackWorkflow)}.{nameof(DeleteStackIfExistsAsync)}] will check if stack exists, Feature='{feature}', StackName='{stackName}'");

            if (await cfnWrapper.IsStackExist(feature, stackName))
            {
                await cfnWrapper.DeleteStackAsync(feature, stackName, (s, sm) => Console.WriteLine($"Status={s}, StatusMessage={sm}"), DELETE_TIMEOUT_MINUTES);
            }
            else
            {
                Console.WriteLine($"[INFO][{nameof(DeleteStackWorkflow)}.{nameof(DeleteStackIfExistsAsync)}] stack does not exist, and will not be deleted, Feature='{feature}', StackName='{stackName}' ");
            }
        }

        private async Task DeleteIntelligenceStackIfExistsAsync(Enums.Feature feature, string stackName, string onboardingId)
        {
            Console.WriteLine($"[INFO][{nameof(DeleteStackWorkflow)}.{nameof(DeleteIntelligenceStackIfExistsAsync)}] will check if stack exists, Feature='{feature}', StackName='{stackName}', OnboardingId='{onboardingId}'");
            var allRegion = RegionEndpoint.EnumerableAllRegions.Select(t => t.SystemName);
            var searchRegions = allRegion.Except(intelligenceUnsupportedRegions);
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
                            var cfnRegionWrapper = CloudFormationWrapper.Get(region);
                            if (await cfnRegionWrapper.IsStackExist(feature, stackName))
                            {
                                await cfnRegionWrapper.DeleteStackAsync(feature, stackName, (s, sm) => Console.WriteLine($"Status={s}, StatusMessage={sm}, StackName={stackName}, action=Delete, OnboardingId='{onboardingId}'"), DELETE_TIMEOUT_MINUTES);
                            }
                            else
                            {
                                Console.WriteLine($"[INFO][{nameof(DeleteStackWorkflow)}.{nameof(DeleteStackIfExistsAsync)}] stack does not exist, and will not be deleted, Feature='{feature}', StackName='{stackName}', region='{region}', action=Delete, OnboardingId='{onboardingId}' ");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[INFO][{nameof(DeleteStackWorkflow)}.{nameof(DeleteIntelligenceStackIfExistsAsync)}] failed to delete intelligence stack in region='{region}', StackName='{stackName}', action=Delete, OnboardingId='{onboardingId}' error= {ex}");
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }
        }

    }
}
