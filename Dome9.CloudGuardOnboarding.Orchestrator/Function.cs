using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class Function
    {
        public Function()
        {
        }

        /// <summary>
        /// Note that it is mandatory that the CloudFormation template parameters have the exact names for the deserialization to work,
        /// So mind that the CloudFormation template YAML file is maintained if there are any changes to the OnboardingRequest class.
        /// </summary>
        /// <param name="cloudFormationRequest"></param>
        /// <returns></returns>
        public async Task FunctionHandler(CloudFormationRequest cloudFormationRequest, ILambdaContext context)
        {
            Console.WriteLine("[INFO] Function Handler start");

            #region param logging            

            Console.WriteLine("[INFO] CloudFormationRequest start");
            Console.WriteLine($"[INFO] {cloudFormationRequest}");
            Console.WriteLine("[INFO] CloudFormationRequest end");
            Console.WriteLine("[INFO] ILambdaContext start");
            Console.WriteLine($"[INFO] {context}");
            Console.WriteLine("[INFO] ILambdaContext end");

            #endregion

            await WorkflowFactory.Create(cloudFormationRequest)
                .RunAsync(
                    cloudFormationRequest.ResourceProperties, 
                    new LambdaCustomResourceResponseHandler(cloudFormationRequest, context));

            Console.WriteLine("[INFO] Function Handler end");
        }
    }
}
