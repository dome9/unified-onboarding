using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public interface ICloudGuardApiWrapper
    {
        Task<ServiceAccount> ReplaceServiceAccount(CredentialsModel model);
        Task DeleteServiceAccount(CredentialsModel model);
        void SetLocalCredentials(ServiceAccount cloudGuardServiceAccount);
        Task ValidateOnboardingId(string onboardingId);
        Task OnboardAccount(AccountModel model);
        Task UpdateOnboardingStatus(StatusModel model);
    }
}
