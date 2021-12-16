using System;
using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ServerlessStackWrapper : StackWrapperBase
    {
        public ServerlessStackWrapper(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base(apiProvider, retryAndBackoffService)
        {
        }

        protected override Enums.Feature Feature => Enums.Feature.ServerlessProtection;

        protected override Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig)
        {
            Console.WriteLine($"[INFO] [GetParameters] {onboardingStackConfig.GetType().Name}=[{onboardingStackConfig}]");
            if (!(onboardingStackConfig is ServerlessStackConfig))
            {
                throw new ArgumentException($"{nameof(onboardingStackConfig)} is not of type {nameof(ServerlessStackConfig)}");
            }

            var permissionsStackConfig = onboardingStackConfig as ServerlessStackConfig;
            return new Dictionary<string, string>
            {
                { "CloudGuardAwsAccountId", permissionsStackConfig.CloudGuardAwsAccountId },
                { "ServerlessStage", permissionsStackConfig.ServerlessStage },
                { "TimeStamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                { "ServerlessRegion", permissionsStackConfig.ServerlessRegion }
            };
        }
    }
}
