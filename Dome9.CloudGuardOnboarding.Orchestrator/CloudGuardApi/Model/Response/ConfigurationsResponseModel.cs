using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Response
{
    public class ConfigurationsResponseModel
    {
        public string PostureStackName { get; set; }
        public string PostureTemplateS3Path { get; set; }
        public string ServerlessStackName { get; set; }
        public string ServerlessTemplateS3Path { get; set; }
        public bool ServerlessProtectionEnabled { get; set; }
        public string ServerlessCftRegion { get; set; }
        public string CloudGuardAwsAccountId { get; set; }
        public string RoleExternalTrustSecret { get; set; }
    }
}
