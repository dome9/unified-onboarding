using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    internal class CloudGuardApWrapperMock : ICloudGuardApiWrapper
    {
        public Task<ServiceAccount> ReplaceServiceAccount(CredentialsModel model)
        {
            return Task.Run(() => new ServiceAccount("af892a1c-81aa-4730-a029-f01c1e97b00b", "4duzpdbt5rcm8g6r7gxdnqne", "https://secure.dome9.com./v2"));
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
        public Task OnboardAccount(AccountModel model)
        {
            return Task.CompletedTask;
        }

        public Task DeleteServiceAccount(CredentialsModel model)
        {
            return Task.CompletedTask;
        }
    }
}
