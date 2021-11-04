using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudGuardApiWrapperMock : ICloudGuardApiWrapper
    {
        public Task DeleteServiceAccount(CredentialsModel model)
        {
            return Task.CompletedTask;
        }

        public Task<ConfigurationsResponseModel> GetConfiguration(ConfigurationsRequestModel model)
        {
            return new Task<ConfigurationsResponseModel>(() => new ConfigurationsResponseModel() { PostureStackName = "Mock" });
        }

        public Task OnboardAccount(AccountModel model)
        {
            return Task.CompletedTask;
        }

        public Task OnboardIntelligence(MagellanOnboardingModel data)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceAccount> ReplaceServiceAccount(CredentialsModel model)
        {
            return new Task<ServiceAccount>(() => new ServiceAccount("fakeKey", "fakeSecret", "www.fake.api.url.com"));
        }

        public Task ServerlessAddAccount(ServelessAddAccountModel model)
        {
            return Task.CompletedTask;
        }

        public void SetLocalCredentials(ServiceAccount cloudGuardServiceAccount)
        {
        }

        public Task UpdateOnboardingStatus(StatusModel model)
        {
            return Task.CompletedTask;
        }

        public Task ValidateOnboardingId(string onboardingId)
        {
            return Task.CompletedTask;
        }
    }
}