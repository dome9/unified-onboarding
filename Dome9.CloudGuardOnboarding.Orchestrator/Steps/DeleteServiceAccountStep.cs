using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{   
    public class DeleteServiceAccountStep : StepBase
    {
        private readonly string _onboardingId;

        public DeleteServiceAccountStep(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, string onboardingId)
        {
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
            _onboardingId = onboardingId;
        }
        public override Task Cleanup()
        {
            //do nothing, only rollback should delete anything
            return Task.CompletedTask;
        }

        public async override Task Execute()
        {
            try
            {
                Console.WriteLine($"[INFO] About to delete service account");
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(_onboardingId, Enums.Status.ACTIVE, "Deleting service account")));
                // must let all the statuses get posted before we delete the service account
                await _retryAndBackoffService.RunAsync(() => _apiProvider.DeleteServiceAccount(new CredentialsModel { OnboardingId = _onboardingId }));
                // can't write to dynamo anymore since we just deleted the service account 
                Console.WriteLine($"[INFO] Deleted service account");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete service account. error={ex}");
            }
        }

        public override Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
