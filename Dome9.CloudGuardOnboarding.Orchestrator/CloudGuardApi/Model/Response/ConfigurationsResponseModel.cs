namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ConfigurationsResponseModel
    {
        public string PostureStackName { get; set; }
        public string PostureTemplateS3Path { get; set; }
        public string ServerlessStackName { get; set; }
        public string ServerlessTemplateS3Path { get; set; }
        public bool ServerlessProtectionEnabled { get; set; }
        public string ServerlessCftRegion { get; set; }
        public string IntelligenceStackName { get; set; }
        public string IntelligenceTemplateS3Path { get; set; }
        public bool IntelligenceEnabled { get; set; }
        public string IntelligenceSnsTopicArn { get; set; }
        public List<long> IntelligenceRulesSetsIds { get; set; }
        public string CloudGuardAwsAccountId { get; set; }
        public string RoleExternalTrustSecret { get; set; }
    }
}
