using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackWrapper
{
    public class IntelligenceStackWrapper : StackWrapperBase
    {
        public IntelligenceStackWrapper(StackOperation stackOperation) : base(stackOperation)
        {

        }

        protected override Enums.Feature Feature => Enums.Feature.Intelligence;

        protected override Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig)
        {
            if (!(onboardingStackConfig is InteligenceStackConfig))
            {
                throw new ArgumentException($"{nameof(onboardingStackConfig)} is not of type {nameof(InteligenceStackConfig)}");
            }

            InteligenceStackConfig inelligenceStackConfig = onboardingStackConfig as InteligenceStackConfig;
            return new Dictionary<string, string>
            {
                {"CloudtrailS3BucketName",  inelligenceStackConfig.CloudtrailS3BucketName},
                {"CloudGuardRoleName", inelligenceStackConfig.CloudGuardRoleName },
                {"UniqueSuffix", inelligenceStackConfig.UniqueSuffix }
            };
        }
    }

}
