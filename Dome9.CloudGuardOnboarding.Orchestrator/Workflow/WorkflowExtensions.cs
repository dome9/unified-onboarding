
namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class WorkflowExtensions
    {
        public static readonly string RoleOnboardingType = "Role";
        public static readonly string UserOnboardingType = "User";
        
        public static bool IsUserBased(this CloudFormationRequest cloudFormationRequest)
        {
            return cloudFormationRequest.ResourceProperties.OnboardingType == UserOnboardingType;
        }
    }
}
