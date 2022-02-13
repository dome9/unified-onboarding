using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleNotificationService;

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
        public async Task FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            var message = snsEvent.Records[0].Sns.Message;
            Console.WriteLine($"[INFO] snsEvent={snsEvent}");
            var cloudFormationRequest = JsonSerializer.Deserialize<CloudFormationRequest>(message);
            Console.WriteLine($"[INFO] cloudFormationRequest={cloudFormationRequest}");

            Console.WriteLine("[INFO] Function Handler start");

            #region param logging            

            Console.WriteLine("[INFO] CloudFormationRequest start");
            Console.WriteLine($"[INFO] {cloudFormationRequest}");
            Console.WriteLine("[INFO] CloudFormationRequest end");

            #endregion

            try
            {
                await WorkflowFactory.Create(cloudFormationRequest)
                .RunAsync(
                    cloudFormationRequest,
                    new LambdaCustomResourceResponseHandler(cloudFormationRequest, context));
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] failed to run onboarding. CloudFormationRequest={cloudFormationRequest}, error={e}");
            }
            Console.WriteLine($"[INFO] succeed to run onboarding. CloudFormationRequest={cloudFormationRequest}.");

            try
            {
                Console.WriteLine($"[INFO] Deleting Subscription {cloudFormationRequest.ResourceProperties.Subscription}");
                var snsClient = new AmazonSimpleNotificationServiceClient();
                await snsClient.UnsubscribeAsync(cloudFormationRequest.ResourceProperties.Subscription);

                Console.WriteLine($"[INFO] Deleting self {cloudFormationRequest.ResourceProperties.Self}");
                var lambdaClient = new AmazonLambdaClient();
                await lambdaClient.DeleteFunctionAsync(cloudFormationRequest.ResourceProperties.Self);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[INFO] failed to delete sns or lambda. CloudFormationRequest={cloudFormationRequest}, error ={e}");
                throw;
            }
            
            Console.WriteLine("[INFO] Function Handler end");
        }
    }
}
