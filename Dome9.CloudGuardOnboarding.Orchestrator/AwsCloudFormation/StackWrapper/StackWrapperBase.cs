﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig;

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
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(stackConfig.OnboardingId, Enums.Status.PENDING, "Creating stack", Feature)));
                    await _cfnWrapper.CreateStackAsync(Feature, stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters, (status) => TryUpdateStackStatus(stackConfig.OnboardingId, status, Feature), stackConfig.ExecutionTimeoutMinutes);
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(stackConfig.OnboardingId, Enums.Status.ACTIVE , "Created stack successfully", Feature)));
                    break;

                case StackOperation.Update:
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(stackConfig.OnboardingId, Enums.Status.PENDING, "Updating existing stack", Feature)));
                    await _cfnWrapper.UpdateStackAsync(Feature, stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters, stackConfig.ExecutionTimeoutMinutes);
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(stackConfig.OnboardingId, Enums.Status.ACTIVE, "Updated existing stack successfully", Feature)));
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
            try
            {
                if (isTriggeredByError)
                {
                    await TryUpdateStatus(stackConfig.OnboardingId, "Deleting stack", Enums.Status.PENDING);
                }
                
                await _cfnWrapper.DeleteStackAsync(Feature, stackConfig.StackName, stackConfig.ExecutionTimeoutMinutes);
                
                if (isTriggeredByError)
                {
                    await TryUpdateStatus(stackConfig.OnboardingId, "Delete stack complete", Enums.Status.ERROR);                
                    await TryUpdateStackStatus(stackConfig.OnboardingId, "Delete stack complete", Feature);
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
                await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateActiveStatusModel(onboardingId, activeStatus, statusMessage, Feature)));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(TryUpdateStatus)}] failed. Error:{ex}");
            }
        }

        private async Task TryUpdateStackStatus(string onboaringId, string stackStatus, Enums.Feature feature)
        {
            try
            {
                await _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(onboaringId, stackStatus, feature)).ConfigureAwait(false);
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
