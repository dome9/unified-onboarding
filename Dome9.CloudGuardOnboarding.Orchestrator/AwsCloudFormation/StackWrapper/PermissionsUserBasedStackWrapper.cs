using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    class PermissionsUserBasedStackWrapper : StackWrapperBase
    {
        public PermissionsUserBasedStackWrapper(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base(apiProvider, retryAndBackoffService)
        {
        }

        protected override Enums.Feature Feature => Enums.Feature.Permissions;

        protected override Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig)
        {
            Console.WriteLine($"[INFO] [GetParameters] {onboardingStackConfig.GetType().Name}=[{onboardingStackConfig}]");
            if(!(onboardingStackConfig is PermissionsUserBasedStackConfig))
            {
                throw new ArgumentException($"{nameof(onboardingStackConfig)} is not of type {nameof(PermissionsUserBasedStackConfig)}");
            }

            var permissionsStackConfig = onboardingStackConfig as PermissionsUserBasedStackConfig;
            return new Dictionary<string, string>();
        }

        public async Task<ApiCredentials> GetAwsUserCredentials()
        {
            return await _cfnWrapper.GetCredentialsFromSecretsManager("CrossAccountUserCredentialsStored");
        }

        public async Task<ApiCredentials> GetStackModifyUserCredentials()
        {
            return await _cfnWrapper.GetCredentialsFromSecretsManager("StackModifyCrossAccountUserCredentialsStored");
        }
    }
}