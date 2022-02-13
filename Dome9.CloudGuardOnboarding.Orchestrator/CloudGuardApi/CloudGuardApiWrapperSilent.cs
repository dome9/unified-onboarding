using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi
{
    public class CloudGuardApiWrapperSilent : CloudGuardApiWrapperBase
    {
        public CloudGuardApiWrapperSilent() { }

        public CloudGuardApiWrapperSilent(string cloudGuardApiKeyId, string cloudGuardApiKeySecret, string apiBaseUrl)
        {
            var serviceAccount = new ServiceAccount(cloudGuardApiKeyId, cloudGuardApiKeySecret, apiBaseUrl);
            SetLocalCredentials(serviceAccount);
        }

        public override Task UpdateOnboardingStatus(StatusModel model)
        {
            return Task.CompletedTask;
        }
    }
}
