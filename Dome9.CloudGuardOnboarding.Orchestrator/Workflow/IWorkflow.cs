using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public interface IWorkflow
    {
        Task RunAsync(CloudFormationRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler);
    }
}
