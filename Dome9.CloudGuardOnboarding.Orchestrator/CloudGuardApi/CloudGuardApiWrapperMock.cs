﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi
{
    public class CloudGuardApiWrapperMock : ICloudGuardApiWrapper
    {
        public Task CreatePosturePolicies(string onboardingId)
        {
            return Task.CompletedTask;
        }

        public Task DeleteServiceAccount(CredentialsModel model)
        {
            return Task.CompletedTask;
        }

        public Task<ConfigurationResponseModel> GetConfiguration(ConfigurationRequestModel model)
        {
            return new Task<ConfigurationResponseModel>(() => new ConfigurationResponseModel() { PermissionsStackName = "Mock" });
        }

        public Task OnboardAccount(AccountModel model)
        {
            return Task.CompletedTask;
        }

        public Task OnboardIntelligence(IntelligenceOnboardingModel data)
        {
            throw new NotImplementedException();
        }
        
        public Task<bool> IsDome9AccountAlreadySubscribedToCloudtrail(AwsGetLogDestinationModel data)
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

        public Task SwitchManagedMode(SwitchManagedModeRequestModel model)
        {
            throw new NotImplementedException();
        }

        public Task UpdateOnboardingStatus(StatusModel model)
        {
            return Task.CompletedTask;
        }

        public Task UpdateOnboardingVersion(string onboardingId, string version)
        {
            throw new NotImplementedException();
        }

        public Task UpdateIntelligenceRegion(string onboardingId, string region)
        {
            throw new NotImplementedException();
        }

        public Task ValidateOnboardingId(string onboardingId)
        {
            return Task.CompletedTask;
        }
    }
}