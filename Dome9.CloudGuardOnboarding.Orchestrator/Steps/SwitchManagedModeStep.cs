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
            var managedStr = _request.IsManaged ? "from managed to non managed" : "from non managed to managed";
            try
            {
                Console.WriteLine($"[INFO] About to switch managed mode {managedStr}");
                await StatusHelper.UpdateStatusAsync(new StatusModel(_request.OnboardingId, Enums.Feature.None, Enums.Status.PENDING, $"About to switch {managedStr}", OnboardingAction.Update));
                await _retryAndBackoffService.RunAsync(() => _apiProvider.SwitchManagedMode(_request));
                await StatusHelper.UpdateStatusAsync(new StatusModel(_request.OnboardingId, Enums.Feature.None, Enums.Status.PENDING, $"Switched {managedStr} successfully", OnboardingAction.Update));
                Console.WriteLine($"[INFO] Switched managed mode successfully {managedStr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to Switched managed mode {managedStr}. error={ex}");
                await StatusHelper.UpdateStatusAsync(new StatusModel(_request.OnboardingId, Enums.Feature.None, Enums.Status.ERROR, $"Failed to switch {managedStr}", OnboardingAction.Update));
                throw;
            }
        }

        public override Task Rollback()
        {
            throw new NotImplementedException();
        }
    }
}
