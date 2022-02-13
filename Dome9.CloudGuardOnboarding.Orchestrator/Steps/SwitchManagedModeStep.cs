using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class SwitchManagedModeStep : StepBase
    {
        private readonly SwitchManagedModeRequestModel _request;

        private readonly string _onboardingId;
        private readonly bool _isManaged;
        private readonly string _stackModifyRoleArn;
        private readonly ApiCredentials _stackModifyUserCredentials;

        public SwitchManagedModeStep(string onboardingId, bool isManaged, string stackModifyRoleArn, ApiCredentials stackModifyUserCredentials)
        {
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _request = new SwitchManagedModeRequestModel(onboardingId, isManaged, stackModifyRoleArn, stackModifyUserCredentials);
            
        }

        public override Task Cleanup()
        {
            throw new NotImplementedException();
        }

        public override async Task Execute()
        {
            try
            {
                Console.WriteLine($"[INFO] About to switch managed mode");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.SwitchManagedMode(_request));
                Console.WriteLine($"[INFO] Switched managed mode successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to Switched managed mode. error={ex}");
                throw;
            }
        }

        public override Task Rollback()
        {
            throw new NotImplementedException();
        }
    }
}
