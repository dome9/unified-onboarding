using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;

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
        Task ServerlessAddAccount(ServelessAddAccountModel model);
        Task<ConfigurationResponseModel> GetConfiguration(ConfigurationRequestModel model);
        Task OnboardIntelligence(IntelligenceOnboardingModel data);
        Task CreatePosturePolicies(string onboardingId);
    }
}
