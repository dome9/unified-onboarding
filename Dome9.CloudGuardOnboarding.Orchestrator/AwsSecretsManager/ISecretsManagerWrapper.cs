using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsSecretsManager
{
    public interface ISecretsManagerWrapper
    {
        Task<ApiCredentials> GetCredentialsFromSecretsManager(string key);
    }
}
