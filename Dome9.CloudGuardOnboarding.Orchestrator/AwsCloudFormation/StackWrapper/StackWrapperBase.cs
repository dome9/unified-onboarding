using System;
using System.Collections.Generic;
using System.Linq;
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
        protected readonly StackOperation _stackOperation;

        private bool _disposed;

        public StackWrapperBase(StackOperation stackOperation)
        {
            _cfnWrapper = CloudFormationWrapper.Get();
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _stackOperation = stackOperation;
        }
        
        public StackWrapperBase(StackOperation stackOperation, string region)
        {
            _cfnWrapper = CloudFormationWrapper.Get(region);
            _apiProvider = CloudGuardApiWrapperFactory.Get();
            _retryAndBackoffService = RetryAndBackoffServiceFactory.Get();
            _stackOperation = stackOperation;
        }

        protected abstract Enums.Feature Feature { get; }

        /// <summary>
        /// Create or update stack 
        /// </summary>
        /// <param name="stackConfig"></param>
        /// <returns></returns>
        public async Task RunStackAsync(OnboardingStackConfig stackConfig)
        {
            var parameters = GetParameters(stackConfig);
            Console.WriteLine($"[INFO] [{nameof(RunStackAsync)}] StackName={stackConfig.StackName}, Parameters=[{string.Join(", ", parameters.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}].");

            switch (_stackOperation)
            {
                case StackOperation.Create:
                    await StatusHelper.UpdateStatusAsync(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.PENDING, "Creating stack"));
                    await _cfnWrapper.CreateStackAsync(Feature, stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters, (status, message) => TryUpdateStackStatus(stackConfig.OnboardingId, status, message, OnboardingAction.Create), stackConfig.ExecutionTimeoutMinutes);
                    await StatusHelper.UpdateStatusAsync(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.ACTIVE, "Created stack successfully"));
                    break;

                case StackOperation.Update:
                    await StatusHelper.UpdateStatusAsync(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.PENDING, "Updating existing stack", OnboardingAction.Update));
                    await _cfnWrapper.UpdateStackAsync(Feature, stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters, (status, message) => TryUpdateStackStatus(stackConfig.OnboardingId, status, message, OnboardingAction.Update), stackConfig.ExecutionTimeoutMinutes);
                    await StatusHelper.UpdateStatusAsync(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.ACTIVE, "Updated existing stack successfully", OnboardingAction.Update));
                    break;

                default:
                    throw new NotImplementedException($"stackOperation: {_stackOperation}");
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
            var onboardinAction = _stackOperation == StackOperation.Create ? OnboardingAction.Create : OnboardingAction.Update;
            try
            {
                if (isTriggeredByError)
                {
                    statusUpdate = async (status, message) => await TryUpdateStackStatus(stackConfig.OnboardingId, status, message, onboardinAction);
                    await StatusHelper.TryUpdateStatusAsync(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.PENDING, "Deleting stack", onboardinAction));
                }

                await _cfnWrapper.DeleteStackAsync(Feature, stackConfig.StackName, statusUpdate, stackConfig.ExecutionTimeoutMinutes);

                if (isTriggeredByError)
                {
                    await StatusHelper.TryUpdateStatusAsync(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.ERROR, "Delete stack complete", onboardinAction));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete stack. Feature={Feature}, Error={e}");
                await StatusHelper.TryUpdateStatusAsync(new StatusModel(stackConfig.OnboardingId, Feature, Enums.Status.ERROR, "Delete stack failed", onboardinAction));
            }

        }

        private async Task TryUpdateStackStatus(string onboaringId, string stackStatus, string stackMessage, OnboardingAction action)
        {
            await StatusHelper.TryUpdateStatusAsync(new StatusModel(onboaringId, Feature, stackStatus, stackMessage, action)).ConfigureAwait(false);
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
