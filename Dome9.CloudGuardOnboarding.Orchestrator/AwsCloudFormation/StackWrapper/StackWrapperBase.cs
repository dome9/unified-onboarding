﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation.StackConfig;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public abstract class StackWrapperBase
    {
        protected enum StackOperation
        {
            None,
            Create,
            Update,
        }

        protected readonly ICloudFormationWrapper _cfnWrapper;
        protected readonly ICloudGuardApiWrapper _apiProvider;
        protected readonly IRetryAndBackoffService _retryAndBackoffService;

        private readonly List<StackStatus> _nonExistingStatus = new List<StackStatus> { StackStatus.CREATE_FAILED, StackStatus.DELETE_COMPLETE };
        private readonly List<StackStatus> _inProgressStatus = new List<StackStatus> { StackStatus.CREATE_IN_PROGRESS, StackStatus.DELETE_IN_PROGRESS, StackStatus.IMPORT_IN_PROGRESS, StackStatus.REVIEW_IN_PROGRESS, StackStatus.UPDATE_IN_PROGRESS, StackStatus.ROLLBACK_IN_PROGRESS, StackStatus.IMPORT_ROLLBACK_IN_PROGRESS, StackStatus.UPDATE_ROLLBACK_IN_PROGRESS, StackStatus.UPDATE_COMPLETE_CLEANUP_IN_PROGRESS, StackStatus.UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS };
        private readonly List<StackStatus> _readyStatus = new List<StackStatus> { StackStatus.CREATE_COMPLETE, StackStatus.IMPORT_COMPLETE, StackStatus.UPDATE_COMPLETE, StackStatus.ROLLBACK_COMPLETE };

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
        public async Task RunStackAsync(OnboardingStackConfig stackConfig)
        {
            Dictionary<string, string> parameters = GetParameters(stackConfig);

            var stackOperation = await GetStackOperation(stackConfig.StackName);
            switch (stackOperation)
            {
                case StackOperation.Create:
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(stackConfig.OnboardingId, "Creating stack", Feature)));
                    await _cfnWrapper.CreateStackAsync(Feature, stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters, (status) => TryUpdateStackStatus(stackConfig.OnboardingId, status, Feature), stackConfig.ExecutionTimeoutMinutes);
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(stackConfig.OnboardingId, "Created stack successfully", Feature)));
                    break;

                case StackOperation.Update:
                    throw new NotImplementedException("Need to check diff before can update.");
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(stackConfig.OnboardingId, "Updating existing stack", Feature)));
                    await _cfnWrapper.UpdateStackAsync(Feature, stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters);
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(stackConfig.OnboardingId, "Updated existing stack successfully", Feature)));
                    break;

                default:
                    throw new NotImplementedException($"stackOperation: {stackOperation}");
            }
        }

        protected virtual Dictionary<string, string> GetParameters(OnboardingStackConfig onboardingStackConfig) => null;        

        public async Task DeleteStackAsync(OnboardingStackConfig stackConfig)
        {
            await TryUpdateStatus(stackConfig.OnboardingId, "Deleting Stack", Enums.Status.ERROR);
            await _cfnWrapper.DeleteStackAsync(Feature, stackConfig.StackName);
            await TryUpdateStatus(stackConfig.OnboardingId, "Deleted Stack", Enums.Status.ERROR);
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

        private void TryUpdateStackStatus(string onboaringId, string stackStatus, Enums.Feature feature)
        {
            try
            {
                _apiProvider.UpdateOnboardingStatus(StatusModel.CreateStackStatusModel(onboaringId, stackStatus, feature)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(TryUpdateStackStatus)}] failed. Error:{ex}");
            }
        }

        /// <summary>
        /// TODO: update action will fail if there is nothing to update, so checking the name has one of _readyStatus values is not enough. 
        /// </summary>
        /// <param name="stackName"></param>
        /// <param name="iteration"></param>
        /// <returns></returns>
        protected async Task<StackOperation> GetStackOperation(string stackName, int iteration = 0)
        {
            // for now disabling this recursive method, as we only support Create anyway.
            return StackOperation.Create;


            var existingStack = await _cfnWrapper.GetStackSummaryAsync(Feature, stackName);
            if (existingStack == null || _nonExistingStatus.Contains(existingStack.StackStatus))
            {
                return StackOperation.Create;
            }

            if (_inProgressStatus.Contains(existingStack.StackStatus))
            {
                if (iteration >= 12)
                {
                    return StackOperation.None;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
                return await GetStackOperation(stackName, ++iteration);
            }

            if (_readyStatus.Contains(existingStack.StackStatus))
            {
                return StackOperation.Update;
            }

            return StackOperation.None;
        }
    }
}