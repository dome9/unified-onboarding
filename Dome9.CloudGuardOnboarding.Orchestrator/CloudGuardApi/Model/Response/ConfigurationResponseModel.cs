﻿using System.Collections.Generic;
namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ConfigurationResponseModel
    {
        public string PermissionsStackName { get; set; }
        public string PermissionsTemplateS3Path { get; set; }
        public string ServerlessStackName { get; set; }
        public string ServerlessTemplateS3Path { get; set; }
        public bool ServerlessProtectionEnabled { get; set; }
        public string ServerlessCftRegion { get; set; }
        public string ServerlessStage { get; set; }
        public string IntelligenceStackName { get; set; }
        public string IntelligenceTemplateS3Path { get; set; }
        public bool IntelligenceEnabled { get; set; }
        public string IntelligenceSnsTopicArn { get; set; }
        public List<long> IntelligenceRulesetsIds { get; set; }
        public string CloudGuardAwsAccountId { get; set; }
        public string RoleExternalTrustSecret { get; set; }
        public string IntelligenceStackRegion { get; set; }

        public override string ToString()
        {
            return $"{nameof(PermissionsStackName)}='{PermissionsStackName}', " +
                $"{nameof(PermissionsTemplateS3Path)}='{PermissionsTemplateS3Path}', " +
                $"{nameof(ServerlessStackName)}='{ServerlessStackName}', " +
                $"{nameof(PermissionsTemplateS3Path)}='{PermissionsTemplateS3Path}', " +
                $"{nameof(ServerlessProtectionEnabled)}='{ServerlessProtectionEnabled}', " +
                $"{nameof(ServerlessCftRegion)}='{ServerlessCftRegion}', " +
                $"{nameof(ServerlessStage)}='{ServerlessStage}', " +
                $"{nameof(IntelligenceStackName)}='{IntelligenceStackName}', " +
                $"{nameof(IntelligenceTemplateS3Path)}='{IntelligenceTemplateS3Path}', " +
                $"{nameof(IntelligenceEnabled)}='{IntelligenceEnabled}', " +
                $"{nameof(IntelligenceSnsTopicArn)}='{IntelligenceSnsTopicArn}', " +
                $"{nameof(IntelligenceRulesetsIds)}='{string.Join(", ", IntelligenceRulesetsIds ?? new List<long>())}', " +
                $"{nameof(CloudGuardAwsAccountId)}='{CloudGuardAwsAccountId}', " +
                $"{nameof(RoleExternalTrustSecret)}='{RoleExternalTrustSecret.MaskChars(2)}'";
        }

        public void SetStackNameSuffix(string suffix)
        {
            PermissionsStackName += suffix;
            ServerlessStackName += suffix;
            IntelligenceStackName += suffix;
        }
    }
}
