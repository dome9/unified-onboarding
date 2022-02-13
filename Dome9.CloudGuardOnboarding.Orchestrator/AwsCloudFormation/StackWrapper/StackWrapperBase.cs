using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public enum StackOperation
    {
        None,
        Create,
        Update,
    }

    public abstract class StackWrapperBase : IDisposable
    {
        protected readonly ICloudFormationWrapper _cfnWrapper;
        protected readonly ICloudGuardApiWrapper _apiProvider;
        protected readonly IRetryAndBackoffService _retryAndBackoffService;

        private readonly List<StackStatus> _nonExistingStatus = new List<StackStatus> { StackStatus.CREATE_FAILED, StackStatus.DELETE_COMPLETE };
        private readonly List<StackStatus> _inProgressStatus = new List<StackStatus> { StackStatus.CREATE_IN_PROGRESS, StackStatus.DELETE_IN_PROGRESS, StackStatus.IMPORT_IN_PROGRESS, StackStatus.REVIEW_IN_PROGRESS, StackStatus.UPDATE_IN_PROGRESS, StackStatus.ROLLBACK_IN_PROGRESS, StackStatus.IMPORT_ROLLBACK_IN_PROGRESS, StackStatus.UPDATE_ROLLBACK_IN_PROGRESS, StackStatus.UPDATE_COMPLETE_CLEANUP_IN_PROGRESS, StackStatus.UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS };
        private readonly List<StackStatus> _readyStatus = new List<StackStatus> { StackStatus.CREATE_COMPLETE, StackStatus.IMPORT_COMPLETE, StackStatus.UPDATE_COMPLETE, StackStatus.ROLLBACK_COMPLETE };
        private bool _disposed;

        public StackWrapperBase(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService)
        {
            _cfnWrapper = CloudFormationWrapper.Get();
            _apiProvider = apiProvider;
            _retryAndBackoffService = retryAndBackoffService;
        }

        protected abstract Enums.Feature Feature { get; }

        /// <summary>
        /// Create or update stack 
        /// </summary>
        /// <param name="stackConfig"></param>
        /// <returns></returns>
        public async Task RunStackAsync(OnboardingStackConfig stackConfig, StackOperation stackOperation)
        {
            Dictionary<string, string> parameters = GetParameters(stackConfig);

            switch (stackOperation)
            {
                case StackOperation.Create:
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.PENDING, "Creating stack", null, null, null)));
                    await _cfnWrapper.CreateStackAsync(Feature, stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters, (status, message) => TryUpdateStackStatus(stackConfig.OnboardingId, status, message), stackConfig.ExecutionTimeoutMinutes);
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.ACTIVE , "Created stack successfully", null, null , null)));
                    break;

                case StackOperation.Update:
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.PENDING, "Updating existing stack", null, null, null)));
                    await _cfnWrapper.UpdateStackAsync(Feature, stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters, stackConfig.ExecutionTimeoutMinutes);
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.ACTIVE, "Updated existing stack successfully", null, null, null)));
                    break;

                default:
                    throw new NotImplementedException($"stackOperation: {stackOperation}");
            }
        }

        public async Task<Stack> DescribeStackAsync(Enums.Feature feature, string stackName)
        {
            return await _cfnWrapper.GetStackDescriptionAsync(feature, stackName);
        }

        protected virtual Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig) => null;        

        public async Task DeleteStackAsync(OnboardingStackConfig stackConfig, bool isTriggeredByError = false)
        {
            Action<string, string> statusUpdate = (s, sm) => Console.WriteLine($"Status={s}, StatusMessage={sm}");

            try
            {
                if (isTriggeredByError)
                {
                    statusUpdate = async (status, message) => await TryUpdateStackStatus(stackConfig.OnboardingId, status, message);
                    await TryUpdateStatus(stackConfig.OnboardingId, "Deleting stack", Enums.Status.PENDING);
                }
                
                await _cfnWrapper.DeleteStackAsync(Feature, stackConfig.StackName, statusUpdate, stackConfig.ExecutionTimeoutMinutes);
                
                if (isTriggeredByError)
                {
                    await TryUpdateStatus(stackConfig.OnboardingId, "Delete stack complete", Enums.Status.ERROR);                
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete stack. Feature={Feature}, Error={e}");
                await TryUpdateStatus(stackConfig.OnboardingId, "Delete stack failed", Enums.Status.ERROR);
            }
            
        }

        private async Task TryUpdateStatus(string onboardingId, string statusMessage, Enums.Status activeStatus)
        {
            try
            {
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(onboardingId, Feature, activeStatus, statusMessage, null, null, null)));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(TryUpdateStatus)}] failed. Error:{ex}");
            }
        }

        private async Task TryUpdateStackStatus(string onboaringId, string stackStatus, string stackMessage)
        {
            try
            {
                await _apiProvider.UpdateOnboardingStatus(new StatusModel(onboaringId, Feature, Enums.Status.None, null, stackStatus, stackMessage, null)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(TryUpdateStackStatus)}] failed. Error:{ex}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cfnWrapper?.Dispose();
                }

                _disposed = true;
            }
        }       

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
