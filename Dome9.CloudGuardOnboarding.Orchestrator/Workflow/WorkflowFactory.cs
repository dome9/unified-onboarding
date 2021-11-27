using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class WorkflowFactory
    {
        public static IWorkflow Create(CloudFormationRequest cloudFormationRequest)
        {
            if (cloudFormationRequest.RequestType.ToLower().Equals("delete"))
            {
                return new DeleteStackWorkflow();
            }
            else if (cloudFormationRequest.RequestType.ToLower().Equals("update"))
            {
                return new UpdateStackWorkflow();
            }


            if (cloudFormationRequest.IsUserBased())
            {
                return new UserBasedOnboardingWorkflow(
                    new CloudGuardApiWrapper(),
                    new RetryAndBackoffService(new SimpleExponentialRetryIntervalProvider()));
            }

            return new OnboardingWorkflow(
                new CloudGuardApiWrapper(),
                new RetryAndBackoffService(new SimpleExponentialRetryIntervalProvider()));
        }
        
    }
}
