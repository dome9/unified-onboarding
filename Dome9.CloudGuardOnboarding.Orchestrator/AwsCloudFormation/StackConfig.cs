using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class StackConfig
    {
        public string TemplateS3Url { get; set; }
        public string StackName { get; set; }
        public string ExecutionRoleArn { get; set; }
        public List<string> Capabilities { get; set; }
        public int ExecutionTimeoutMinutes { get; set; }

        public StackConfig(string templateS3Url, string stackName, List<string> capabilities, int executionTimeoutMinutes = 5)
        {
            TemplateS3Url = templateS3Url;
            StackName = stackName;
            Capabilities = capabilities;
            ExecutionTimeoutMinutes = executionTimeoutMinutes;
        }

        public StackConfig() { }
    }
}