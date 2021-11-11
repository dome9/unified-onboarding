using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class WorkflowFactory
    {
        public static IWorkflow Create(bool userBased)
        {
            if (userBased)
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
