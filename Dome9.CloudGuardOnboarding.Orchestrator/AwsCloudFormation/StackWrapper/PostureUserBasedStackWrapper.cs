using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    class PostureUserBasedStackWrapper : StackWrapperBase
    {
        public PostureUserBasedStackWrapper(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base(apiProvider, retryAndBackoffService)
        {
        }

        protected override Enums.Feature Feature => Enums.Feature.ContinuousCompliance;

        protected override Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig)
        {
            Console.WriteLine($"[INFO] [GetParameters] {onboardingStackConfig.GetType().Name}=[{onboardingStackConfig}]");
            if(!(onboardingStackConfig is PostureUserBasedStackConfig))
            {
                throw new ArgumentException($"{nameof(onboardingStackConfig)} is not of type {nameof(PostureUserBasedStackConfig)}");
            }

            var postureStackConfig = onboardingStackConfig as PostureUserBasedStackConfig;
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