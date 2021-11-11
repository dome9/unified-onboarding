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
            #region param logging

            Console.WriteLine("[INFO] CloudFormationRequest start");
            Console.WriteLine($"[INFO] {cloudFormationRequest}");
            Console.WriteLine("[INFO] CloudFormationRequest end");
            Console.WriteLine("[INFO] ILambdaContext start");
            Console.WriteLine($"[INFO] {context}");
            Console.WriteLine("[INFO] ILambdaContext end");

            #endregion

            var customResourceResponseHandler = new LambdaCustomResourceResponseHandler(cloudFormationRequest, context);
            if (cloudFormationRequest.RequestType.ToLower().Equals("delete"))
            {
                // TODO: delete all "child" stacks (e.g. posture, serverless)
                // this will be invoked if the users deletes the stack manually from the aws cloudformation console ui
                // as a last step, after deleting the stacks, this lambda should post back to API to remove the "cloud account"
                // the above should be a new type of workflow.
                await TryPostbackDeleteSuccess(customResourceResponseHandler);
                return;
            }
            else if (cloudFormationRequest.RequestType.ToLower().Equals("update"))
            {
                throw new NotImplementedException("Request of type 'Update' is not supported");
            }

            await WorkflowFactory.Create(!string.IsNullOrWhiteSpace(cloudFormationRequest.ResourceProperties?.AwsPartition))
                .RunAsync(cloudFormationRequest.ResourceProperties, customResourceResponseHandler);
        }

        private static async Task TryPostbackDeleteSuccess(LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            try
            {
                Console.WriteLine("[INFO] Request type is 'Delete', will attempt postback of successful result.");
                await customResourceResponseHandler.PostbackSuccess();
                Console.WriteLine("[INFO] Request type is 'Delete', postback of successful result succeeded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Request type is 'Delete', postback of successful result failed. Error={ex}");
            }
        }
    }
}
