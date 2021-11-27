using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class DeleteStackWorkflow : IWorkflow
    {
        private const int DELETE_TIMEOUT_MINUTES = 15;  //max lambda execution duration
        private class AccountStacks
        {            
            internal static Dictionary<Enums.Feature, string> FeatureStacks
            {
                get => new Dictionary<Enums.Feature, string>()
                {
                    { Enums.Feature.Intelligence, "CloudGuard-Onboarding-Intelligence"},
                    { Enums.Feature.ServerlessProtection, "CloudGuard-Onboarding-Serverless"},
                    { Enums.Feature.ContinuousCompliance, "CloudGuard-Onboarding-Posture"}
                };
            }
        }

        /// <summary>
        /// Delete all "child" stacks (e.g. posture, serverless, intelligence).
        /// This will be invoked if the users deletes the stack manually from the aws cloudformation console UI,
        /// or if CloudGuard backend deletes the onblarding stack (assuming it has permissions to do so).
        /// </summary>
        /// <param name="request"></param>
        /// <param name="customResourceResponseHandler"></param>
        /// <returns></returns>
        public async Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {           
            using (var cfnWrapper = CloudFormationWrapper.Get())
            {
                try
                {
                    var tasks = AccountStacks.FeatureStacks.Select(f => Task.Run(async () => { await cfnWrapper.DeleteStackAsync(f.Key, f.Value, DELETE_TIMEOUT_MINUTES); }));
                    
                    // wait until all the delete tasks are finished
                    await Task.WhenAll(tasks);
                }
                catch (AggregateException ex)
                {
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
                finally
                {
                    await customResourceResponseHandler.PostbackSuccess();
                }
            }
        }
    }
}
