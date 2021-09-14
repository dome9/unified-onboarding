using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public interface IStackWrapper
    {
        Task<string> CreateStackAsync(string stackTemplateS3Url, string stackName,
            List<string> capabilities, Dictionary<string, string> parameters,
            int executionTimeoutMinutes = 5);
        Task<string> UpdateStackAsync(string stackTemplateS3Url, string stackName,
            List<string> capabilities, Dictionary<string, string> parameters);
        Task<string> GetStackTemplateAsync(string stackName);
        Task<StackSummary> GetStackSummaryAsync(string stackName);
        Task DeleteStackAsync(string stackName);
    }
}