using Dome9.CloudGuardOnboarding.Orchestrator.AwsSecretsManager;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    class PermissionsUserBasedStackWrapper : StackWrapperBase
    {
        private readonly ISecretsManagerWrapper _secretsManagerWrapper;
        public PermissionsUserBasedStackWrapper(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base(apiProvider, retryAndBackoffService)
        {
            _secretsManagerWrapper = SecretsManagerWrapper.Get();
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
            return new Dictionary<string, string>
            {
                { "UniqueSuffix", permissionsStackConfig.UniqueSuffix }
            };
        }

        public async Task<ApiCredentials> GetAwsUserCredentials()
        {
            return await _secretsManagerWrapper.GetCredentialsFromSecretsManager("CrossAccountUserCredentialsStored");
        }

        public async Task<ApiCredentials> GetStackModifyUserCredentials()
        {
            return await _secretsManagerWrapper.GetCredentialsFromSecretsManager("CloudGuardOnboardingStackModifyPermissions");
        }
    }
}