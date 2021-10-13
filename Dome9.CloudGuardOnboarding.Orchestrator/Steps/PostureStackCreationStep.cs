using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PostureStackCreationStep : StepBase
    {
        private readonly PostureStackWrapper _awsStackWrapper;
        private readonly PostureStackConfig _stackConfig;        

        public PostureStackCreationStep(
            ICloudGuardApiWrapper apiProvider,
            IRetryAndBackoffService retryAndBackoffService,
            string stackName,
            string templateS3Url,
            string cloudGuardAwsAccountId,
            string cloudGuardExternalTrustId,
            string onboardingId,
            int stackExecutionTimeoutMinutes = 35)
        {
            // TODO: if there are capablilities that are not unique to the Posture stack, this dictionary should be initialized at PostureConfig base, and only Posture-specific entries added
            // e.g. Capablilitis.Add("WHATEVER_IAM");
            var capabilities = new List<string> { "CAPABILITY_IAM", "CAPABILITY_NAMED_IAM", "CAPABILITY_AUTO_EXPAND" };
            _awsStackWrapper  = new PostureStackWrapper(apiProvider, retryAndBackoffService);
            _stackConfig = new PostureStackConfig(templateS3Url, stackName, capabilities, onboardingId, cloudGuardAwsAccountId, cloudGuardExternalTrustId, stackExecutionTimeoutMinutes);
        }

        public override async Task Execute()
        {
            Console.WriteLine($"[INFO][{nameof(PostureStackCreationStep)}.{nameof(Execute)}] RunStackAsync starting");
            await _awsStackWrapper.RunStackAsync(_stackConfig);
            Console.WriteLine($"[INFO][{nameof(PostureStackCreationStep)}.{nameof(Execute)}] RunStackAsync finished");
        }

        public override async Task Rollback()
        {
            try
            {
                Console.WriteLine($"[INFO][{nameof(PostureStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync starting");
                await TryUpdateStatusError(_stackConfig.OnboardingId, "Deleting Posture stack", Enums.Feature.ContinuousCompliance);

                // stack may not have been created, try to delete but do not throw in case of failure
                await _awsStackWrapper.DeleteStackAsync(_stackConfig);

                await TryUpdateStatusError(_stackConfig.OnboardingId, "Deleted Posture stack", Enums.Feature.ContinuousCompliance);
                Console.WriteLine($"[INFO][{nameof(PostureStackCreationStep)}.{nameof(Rollback)}] DeleteStackAsync finished");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rollback failed, however stack may not have been created. Check exception to verify. Error:{ex}");
                await TryUpdateStatusError(_stackConfig.OnboardingId, "Rollback Posture stack failed", Enums.Feature.ContinuousCompliance);

            }
        }

        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }
    }
}