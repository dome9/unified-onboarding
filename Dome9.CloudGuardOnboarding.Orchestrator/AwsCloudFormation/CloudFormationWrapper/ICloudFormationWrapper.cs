using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public interface ICloudFormationWrapper : IDisposable
    {
        Task<string> CreateStackAsync(
            Enums.Feature feature, 
            string stackTemplateS3Url, 
            string stackName,
            List<string> capabilities, 
            Dictionary<string, string> parameters, 
            Action<string, string> statusUpdate,
            int executionTimeoutMinutes);

        Task UpdateStackAsync(
            Enums.Feature feature, 
            string stackTemplateS3Url, 
            string stackName,
            List<string> capabilities, 
            Dictionary<string, string> parameters,
            int executionTimeoutMinutes);

        Task<string> GetStackTemplateAsync(Enums.Feature feature, string stackName);

        Task<Stack> GetStackDescriptionAsync(Enums.Feature feature, string stackName, bool filterDeleted = true);

        Task DeleteStackAsync(Enums.Feature feature, string stackName, Action<string, string> statusUpdate, int executionTimeoutMinutes);
        Task<bool> IsStackExist(Enums.Feature feature, string stackName);

        Task<ApiCredentials> GetCredentialsFromSecretsManager(string key);
    }
}