using System;
using System.Collections.Generic;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig;

namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackWrapper
{
    public class IntelligenceStackWrapper : StackWrapperBase
    {
        public IntelligenceStackWrapper(StackOperation stackOperation, string region) : base(stackOperation, region)
        {
        }
        
        public IntelligenceStackWrapper(StackOperation stackOperation) : base(stackOperation)
        {
        }

        protected override Enums.Feature Feature => Enums.Feature.Intelligence;

        protected override Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig)
        {
            if (!(onboardingStackConfig is IntelligenceStackConfig))
            {
                throw new ArgumentException($"{nameof(onboardingStackConfig)} is not of type {nameof(IntelligenceStackConfig)}");
            }

            IntelligenceStackConfig inelligenceStackConfig = onboardingStackConfig as IntelligenceStackConfig;
            return new Dictionary<string, string>
            {
                { "CloudtrailS3BucketName",  inelligenceStackConfig.CloudtrailS3BucketName},
                { "CloudGuardRoleName", inelligenceStackConfig.CloudGuardRoleName },
                { "UniqueSuffix", inelligenceStackConfig.UniqueSuffix },
                {"IntelligenceSubscriptionEndpoint", inelligenceStackConfig.IntelligenceSubscriptionEndpoint },
                {"IntelligenceAwsAccountId", inelligenceStackConfig.IntelligenceAwsAccountId },
                {"CloudTrailKmsArn", inelligenceStackConfig.CloudTrailKmsArn }
            };
        }
    }
}
