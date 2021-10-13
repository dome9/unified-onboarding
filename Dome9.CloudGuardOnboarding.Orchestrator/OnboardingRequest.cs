using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingRequest
    {
        public string ServiceToken { get; set; }
        public string ApiBaseUrl { get; set; }
        public string PostureStackName { get; set; }
        public string ServerlessStackName { get; set; }
        public string CloudGuardApiKeyId { get; set; }
        public string CloudGuardApiKeySecret { get; set; }
        public string CloudGuardAwsAccountId { get; set; }
        public string ServerlessProtectionEnabled { get; set; }
        public string RoleExternalTrustSecret { get; set; }
        public string OnboardingId { get; set; }
        public string PostureTemplateS3Url { get; set; }
        public string ServerlessTemplateS3Url { get; set; }
        public string AwsAccountId { get; set; }
        
        public override string ToString()
        {
            return $"OnboardingRequest: [OnboardingId={OnboardingId}], [StackName={PostureStackName}], [StackTemplateS3Url={PostureTemplateS3Url}], [AwsAccountId={AwsAccountId}], [CloudGuardAwsAccountId={CloudGuardAwsAccountId}], [ApiBaseUrl={ApiBaseUrl}], [CloudGuardApiKeyId={CloudGuardApiKeyId}],[CloudGuardApiKeySecret={CloudGuardApiKeySecret}], [RoleExternalTrustSecret={RoleExternalTrustSecret}], [ServerlessProtectionEnabled={ServerlessProtectionEnabled}], [ServerlessStackName={ServerlessStackName}], [ServerlessTemplateS3Url={ServerlessTemplateS3Url}]";
        }
    }


    public class CloudFormationRequest
    {
        public string RequestType { get; set; }
        public string ResponseURL { get; set; }
        public string StackId { get; set; }
        public string RequestId { get; set; }
        public string ResourceType { get; set; }
        public string LogicalResourceId { get; set; }
        public OnboardingRequest ResourceProperties { get; set; }
        public override string ToString()
        {
            return $"CloudFormationRequest: [RequestType={RequestType}], [ResponseURL={ResponseURL}], [StackId={StackId}], [RequestId={RequestId}], [ResourceType={ResourceType}], [LogicalResourceId={LogicalResourceId}] >> Nested ResourceProperties: {ResourceProperties}";
        }
    }   
}
