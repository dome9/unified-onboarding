using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using Dome9.CloudGuardOnboarding.Orchestrator.Workflow;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class WorkflowFactory
    {
        public static IWorkflow Create(CloudFormationRequest cloudFormationRequest)
        {
            if (cloudFormationRequest.RequestType.ToLower().Equals("delete"))
            {
                return new EmptyWorkflow();
            }

            if (cloudFormationRequest.RequestType.ToLower().Equals("update"))
            {
                if (cloudFormationRequest.ResourceProperties.DeleteInnerResources.ToLower().Equals("true"))
                {
                    return new DeleteStackWorkflow(cloudFormationRequest.IsUserBased());
                }
                CloudGuardApiWrapperFactory.Init(cloudFormationRequest.ResourceProperties.CloudGuardApiKeyId, cloudFormationRequest.ResourceProperties.CloudGuardApiKeySecret, cloudFormationRequest.ResourceProperties.ApiBaseUrl, "silent");
                RetryAndBackoffServiceFactory.Init();
                return new UpdateStackWorkflow(cloudFormationRequest.IsUserBased());
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
