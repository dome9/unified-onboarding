﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class DeleteStackWorkflow : IWorkflow
    {
        // allow 3 minutes for postback of custom resource "success"
        private const int FLOW_TIMEOUT_UNTIL_POSTBACK_MINUTES = 12;
        private const int DELETE_TIMEOUT_MINUTES = 10;

        private readonly OnboardingType _onboardingType;

        public DeleteStackWorkflow(bool isUserBased)
        {
            _onboardingType = isUserBased ? OnboardingType.UserBased : OnboardingType.RoleBased;
        }

        public async Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                var cfnWrapper = CloudFormationWrapper.Get();
                try
                {
                    tokenSource.CancelAfter(TimeSpan.FromMinutes(FLOW_TIMEOUT_UNTIL_POSTBACK_MINUTES));

                    var stack = await cfnWrapper.GetStackDescriptionAsync(Enums.Feature.None, "CloudGuard-Onboarding");
                    var stackVersion = stack.Parameters.FirstOrDefault(p => p.ParameterKey == nameof(OnboardingRequest.Version))?.ParameterValue;
                    if (stackVersion != request.Version)
                    {
                        Console.WriteLine($"[{nameof(DeleteStackWorkflow)}.{nameof(RunAsync)}] Request version is deifferent then the stack version, this is because of an update, will not delete chile stacks. RequestVersion={request.Version}, StackVewrsion={stackVersion}.");
                        return;
                    }

                    var tasks = ChildStacksConfig.GetSupportedFeaturesStackNames(_onboardingType)
                                    .Select(f => Task.Run(async () => { await DeleteStackIfExistsAsync(f.Key, f.Value, cfnWrapper); }, tokenSource.Token));

                    // wait until all the delete tasks are finished
                    await Task.WhenAll(tasks);
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
                await cfnWrapper.DeleteStackAsync(feature, stackName, DELETE_TIMEOUT_MINUTES);
            }
            else
            {
                Console.WriteLine($"[INFO][{nameof(DeleteStackWorkflow)}.{nameof(DeleteStackIfExistsAsync)}] stack does not exist, and will not be deleted, Feature='{feature}', StackName='{stackName}' ");
            }
        }

    }
}