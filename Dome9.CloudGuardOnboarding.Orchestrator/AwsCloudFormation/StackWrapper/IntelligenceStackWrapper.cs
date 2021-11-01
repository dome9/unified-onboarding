using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig;
using System;
using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackWrapper
{
    public class IntelligenceStackWrapper : StackWrapperBase
    {
        public IntelligenceStackWrapper(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base(apiProvider, retryAndBackoffService)
        {

        }

        protected override Enums.Feature Feature => Enums.Feature.Intelligence;

        protected override Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig)
        {
            if (!(onboardingStackConfig is InteligenceStackConfig))
            {
                throw new ArgumentException("OnboardingStackConfig must be of type LogicStackConfig");
            }

            InteligenceStackConfig logicStackConfig = onboardingStackConfig as InteligenceStackConfig;
            return new Dictionary<string, string>
            {
                {"CloudtrailS3BucketName",  logicStackConfig.CloudtrailS3BucketName},
                {"CloudGuardRoleName", logicStackConfig.CloudGuardRoleName }
            };
        }
    }

}
