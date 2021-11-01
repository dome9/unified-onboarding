using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Response;
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
        Task ServerlessAddAccount(ServelessAddAccountModel model);
        Task<ConfigurationsResponseModel> GetConfigurations(ConfigurationsRequestModel model);
        Task OnboardIntelligence(MagellanOnboardingModel data);
    }
}
