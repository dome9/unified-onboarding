using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class WorkflowExtensions
    {
        public static bool IsUserBased(this CloudFormationRequest cloudFormationRequest)
        {
            return !(string.IsNullOrWhiteSpace(cloudFormationRequest.ResourceProperties?.AwsPartition) || cloudFormationRequest.ResourceProperties?.AwsPartition == "aws");
        }
    }
}
