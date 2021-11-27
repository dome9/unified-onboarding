using System;
using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PostureStackWrapper : StackWrapperBase
    {
        public PostureStackWrapper(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base (apiProvider, retryAndBackoffService)
        { 
        }

        protected override Enums.Feature Feature => Enums.Feature.ContinuousCompliance;        

        protected override Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig)
        {
            if(!(onboardingStackConfig is PostureStackConfig))
            {
                throw new ArgumentException($"{nameof(onboardingStackConfig)} is not of type {nameof(PostureStackConfig)}");
            }

            PostureStackConfig postureStackConfig = onboardingStackConfig as PostureStackConfig;
            return new Dictionary<string, string>
            {
                { "CloudGuardAwsAccountId",  postureStackConfig.CloudGuardAwsAccountId },
                { "RoleExternalTrustSecret", postureStackConfig.RoleExternalTrustSecret }             
            };
        }
    }
}