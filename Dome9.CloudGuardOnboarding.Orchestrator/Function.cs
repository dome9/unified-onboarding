using System;
using System.Collections.Generic;
using System.Text.Json;
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
        /// The CloudFormationRequest.ResourceProperties object is a System.Text.Json.JsonElement
        /// It can be implicitly cast into a Dictionary<object, object>, or deserialized into a OnboardingRequest - an easier option.
        /// Note that it is mandatory that the CloudFormation template parameters have the exact names for the deserialization to work,
        /// So mind that the CloudFormation template YAML file is maintained if there are any changes to the OnboardingRequest class.
        /// </summary>
        /// <param name="cloudFormationRequest"></param>
        /// <returns></returns>
        public async Task FunctionHandler(CloudFormationRequest cloudFormationRequest, ILambdaContext context)//CloudFormationRequest cloudFormationRequest, ILambdaContext context)
        {
            #region param logging

            Console.WriteLine("cloudFormationRequest start");
            Console.WriteLine(cloudFormationRequest);
            Console.WriteLine("cloudFormationRequest end");
            Console.WriteLine("context start");
            Console.WriteLine(context);
            Console.WriteLine("context end");

            #endregion

            var customResourceResponseHandler = new LambdaCustomResourceResponseHandler(cloudFormationRequest, context);
            if (cloudFormationRequest.RequestType.ToLower().Equals("delete"))
            {
                await TryPostbackDeleteSuccess(customResourceResponseHandler);
                return;
            }
            else if (cloudFormationRequest.RequestType.ToLower().Equals("update"))
            {
                throw new NotImplementedException("Request of type 'Update' is not supported");
            }

            await new OnboardingWorkflow(new CloudGuardApiWrapper()).RunAsync(cloudFormationRequest.ResourceProperties, customResourceResponseHandler);
        }

        private static async Task TryPostbackDeleteSuccess(LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            try
            {
                Console.WriteLine("Request type is 'Delete', will attempt postback of successful result.");
                await customResourceResponseHandler.PostbackSuccess();
                Console.WriteLine("Request type is 'Delete', postback of successful result succeeded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request type is 'Delete', postback of successful result failed. Error={ex}");
            }
        }
    }
}
