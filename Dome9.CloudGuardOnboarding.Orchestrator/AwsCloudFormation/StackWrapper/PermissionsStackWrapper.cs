﻿using System;
using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PermissionsStackWrapper : StackWrapperBase
    {
        public PermissionsStackWrapper(StackOperation stackOperation) : base(stackOperation)
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
                { "UniqueSuffix", permissionsStackConfig.UniqueSuffix },             
                { "UseAwsReadOnlyPolicy", permissionsStackConfig.UseAwsReadOnlyPolicy }             
            };
        }
    }
}