using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public interface IWorkflow
    {
        Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler);
    }
}
