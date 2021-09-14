using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class PostureStackCreationStep : IStep
    {
        private readonly PostureStackWrapper _awsStackWrapper;     
        private readonly PostureStackConfig _stackConfig;        

        public PostureStackCreationStep(ICloudGuardApiWrapper apiProvider,
            string stackName,
            string templateS3Url,
            string cloudGuardAwsAccountId,
            string cloudGuardExternalTrustId,
            string onboardingId,
            int stackExecutionTimeoutMinutes = 5)
        {
            // TODO: if there are capablilities that are not unique to the Posture stack, this dictionary should be initialized at PostureConfig base, and only Posture-specific entries added
            // e.g. Capablilitis.Add("WHATEVER_IAM");
            var capabilities = new List<string> { "CAPABILITY_IAM", "CAPABILITY_NAMED_IAM", "CAPABILITY_AUTO_EXPAND" };
            _awsStackWrapper  = new PostureStackWrapper(apiProvider);
            _stackConfig = new PostureStackConfig(templateS3Url, stackName, capabilities, onboardingId, cloudGuardAwsAccountId, cloudGuardExternalTrustId, stackExecutionTimeoutMinutes);
        }

        public async Task Execute()
        {
            await _awsStackWrapper.RunStackAsync(_stackConfig);
        }

        public async Task Rollback()
        {
            try
            {
                // stack may not have been created, try to delete but do not throw in case of failure
                await _awsStackWrapper.DeleteStackAsync(_stackConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rollback failed, however stack may not have been created. Check exception to verify. Error:{ex}");
            }        
        }

        public Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }
    }
}