using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudGuardApiWrapperMock : ICloudGuardApiWrapper
    {
        public Task DeleteServiceAccount(CredentialsModel model)
        {
            return Task.CompletedTask;
        }

        public Task<ConfigurationResponseModel> GetConfiguration(ConfigurationRequestModel model)
        {
            return new Task<ConfigurationResponseModel>(() => new ConfigurationResponseModel() { PostureStackName = "Mock" });
        }

        public Task OnboardAccount(AccountModel model)
        {
            return Task.CompletedTask;
        }

        public Task OnboardIntelligence(IntelligenceOnboardingModel data)
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