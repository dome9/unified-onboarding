using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PermissionsStackWrapper : StackWrapperBase
    {
        public PermissionsStackWrapper(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base (apiProvider, retryAndBackoffService)
        { 
        }

        protected override Enums.Feature Feature => Enums.Feature.Permissions;        

        protected override Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig)
        {
            if(!(onboardingStackConfig is PermissionsStackConfig))
            {
                throw new ArgumentException($"{nameof(onboardingStackConfig)} is not of type {nameof(PermissionsStackConfig)}");
            }

            PermissionsStackConfig permissionsStackConfig = onboardingStackConfig as PermissionsStackConfig;
            return new Dictionary<string, string>
            {
                { "CloudGuardAwsAccountId",  permissionsStackConfig.CloudGuardAwsAccountId },
                { "RoleExternalTrustSecret", permissionsStackConfig.RoleExternalTrustSecret },           
                { "UniqueSuffix", permissionsStackConfig.UniqueSuffix }             
            };
        }
    }
}