using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public interface ICloudFormationWrapper
    {
        Task<string> CreateStackAsync(
            Enums.Feature feature, 
            string stackTemplateS3Url, 
            string stackName,
            List<string> capabilities, 
            Dictionary<string, string> parameters, 
            Action<string> statusUpdate,
            int executionTimeoutMinutes);

        Task<string> UpdateStackAsync(
            Enums.Feature feature, 
            string stackTemplateS3Url, 
            string stackName,
            List<string> capabilities, 
            Dictionary<string, string> parameters);

        Task<string> GetStackTemplateAsync(Enums.Feature feature, string stackName);

        Task<StackSummary> GetStackSummaryAsync(Enums.Feature feature, string stackName);

        Task DeleteStackAsync(Enums.Feature feature, string stackName);

        Task<ApiCredentials> GetCredentialsFromSecretsManager();
    }
}