using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Workflow
{
    public class EmptyWorkflow : IWorkflow
    {
        public Task RunAsync(CloudFormationRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            return Task.CompletedTask;
        }
    }
}
