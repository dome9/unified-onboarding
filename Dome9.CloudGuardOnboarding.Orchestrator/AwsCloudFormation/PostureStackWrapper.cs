using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class PostureStackWrapper
    {
        enum StackOperation
        {
            None,
            Create,
            Update,
        }

        private readonly IStackWrapper _cftWrapper;
        private readonly ICloudGuardApiWrapper _apiProvider;
        
        private readonly List<StackStatus> _nonExistingStatus = new List<StackStatus> {StackStatus.CREATE_FAILED, StackStatus.DELETE_COMPLETE};
        private readonly List<StackStatus> _inProgressStatus = new List<StackStatus> {StackStatus.CREATE_IN_PROGRESS, StackStatus.DELETE_IN_PROGRESS, StackStatus.IMPORT_IN_PROGRESS, StackStatus.REVIEW_IN_PROGRESS, StackStatus.UPDATE_IN_PROGRESS, StackStatus.ROLLBACK_IN_PROGRESS, StackStatus.IMPORT_ROLLBACK_IN_PROGRESS, StackStatus.UPDATE_ROLLBACK_IN_PROGRESS, StackStatus.UPDATE_COMPLETE_CLEANUP_IN_PROGRESS, StackStatus.UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS};
        private readonly List<StackStatus> _readyStatus = new List<StackStatus> {StackStatus.CREATE_COMPLETE, StackStatus.IMPORT_COMPLETE, StackStatus.UPDATE_COMPLETE, StackStatus.ROLLBACK_COMPLETE};

        public PostureStackWrapper(ICloudGuardApiWrapper apiProvider)
        {
            _cftWrapper = StackWrapper.Get();
            _apiProvider = apiProvider;
        }


        /// <summary>
        /// TODO: handle failure on create and update - especially in create - maybe should call DeleteStack
        /// </summary>
        /// <param name="stackConfig"></param>
        /// <returns></returns>
        public async Task RunStackAsync(PostureStackConfig stackConfig)
        {
            var parameters = new Dictionary<string, string>
            {             
                {"CloudGuardAwsAccountId",  stackConfig.CloudGuardAwsAccountId},
                {"RoleExternalTrustSecret", stackConfig.RoleExternalTrustSecret }
            };

            var stackOperation = await GetStackOperation(stackConfig.StackName);
            switch (stackOperation)
            {
                case StackOperation.Create:
                    await _apiProvider.UpdateOnboardingStatus(new StatusModel(stackConfig.OnboardingId, "Creating new Stack", Status.PENDING));
                    await _cftWrapper.CreateStackAsync(stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters);
                    break;
                case StackOperation.Update:
                    throw new NotImplementedException("Need to check diff before can update.");
                    await _apiProvider.UpdateOnboardingStatus(new StatusModel(stackConfig.OnboardingId, "Updating existing Stack", Status.PENDING));
                    await _cftWrapper.UpdateStackAsync(stackConfig.TemplateS3Url, stackConfig.StackName, stackConfig.Capabilities, parameters);
                    break;
                default:
                    throw new Exception($"stackOperation: {stackOperation}");
            }
        }

        public async Task DeleteStackAsync(PostureStackConfig stackConfig)
        {
            await _cftWrapper.DeleteStackAsync(stackConfig.StackName);
        }


        /// <summary>
        /// TODO: update action will fail if there is nothing to update, so checking the name has one of _readyStatus values is not enough. 
        /// </summary>
        /// <param name="stackName"></param>
        /// <param name="iteration"></param>
        /// <returns></returns>
        private async Task<StackOperation> GetStackOperation(string stackName, int iteration = 0)
        {
            var existingStack = await _cftWrapper.GetStackSummaryAsync(stackName);
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

        // private async Task PollStackProgress()
        // {
        //     throw new NotImplementedException();
        //
        //     while (_statusAction != null)
        //     {
        //         using (var client = new AmazonCloudFormationClient())
        //         {
        //            //TODO: find out how how do we poll for status
        //         }
        //         _statusAction.Invoke("whatever is the status now");
        //         await Task.Delay(TimeSpan.FromSeconds(5));
        //     }
        // }
    }
}