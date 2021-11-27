using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class LambdaCustomResourceResponseHandler
    {
        const string SUCCESS = "SUCCESS";
        const string FAILED = "FAILED";

        public LambdaCustomResourceResponseHandler(CloudFormationRequest request, ILambdaContext context)
        {
            _cloudFormationRequest = request;
            _context = context;
        }

        private CloudFormationRequest _cloudFormationRequest;
        private ILambdaContext _context;

        public async Task PostbackSuccess()
        {
            try
            {
                await PostbackStatus(SUCCESS);            
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(LambdaCustomResourceResponseHandler)}.{nameof(PostbackSuccess)}] failed. Error={ex}");
                throw;
            }
        }

        public async Task PostbackFailure(string error = null)
        {
            try
            {
                await PostbackStatus(FAILED, error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(LambdaCustomResourceResponseHandler)}.{nameof(PostbackFailure)}] failed. Error={ex}");
                throw;
            }
        }

        private async Task PostbackStatus(string status, string error = null)
        {
            Console.WriteLine($"[INFO] [{nameof(PostbackStatus)}] status='{status}', error='{error ?? string.Empty}'");

            string errorIfExists = string.IsNullOrWhiteSpace(error) ? string.Empty : $"{error.Trim()}. ";

            using (HttpClient client = new HttpClient())
            {
                CloudFormationResponseBody body = new CloudFormationResponseBody
                {
                    Status = status,
                    Reason = $"{errorIfExists}See details in CloudWatch Log Stream: {_context.LogStreamName}",
                    PhysicalResourceId = _context.LogStreamName,
                    StackId = _cloudFormationRequest.StackId,
                    RequestId = _cloudFormationRequest.RequestId,
                    LogicalResourceId = _cloudFormationRequest.LogicalResourceId,
                    NoEcho = false,
                    //Data = null // needs to look like {}                   
                };

                var content = HttpClientUtils.GetContent<CloudFormationResponseBody>(body,  HttpClientUtils.SerializationOptionsType.PascalCase);
                Console.WriteLine($"[INFO] CloudFormationResponseBody={await content.ReadAsStringAsync()}");
                var response = await client.PutAsync(_cloudFormationRequest.ResponseURL, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Custom resource reponse PUT failed. Reponse StatusCode:{response.StatusCode}, ReasonPhrase:'{response.ReasonPhrase}'");
                }

                Console.WriteLine($"[INFO] Successful S3 response: {response}");
            }       
        }        
    }

    public class CloudFormationResponseBody 
    {
        public string Status { get; set; }
        public string Reason { get; set; }
        public string PhysicalResourceId  { get; set; }
        public string StackId { get; set; }
        public string RequestId { get; set; }
        public string LogicalResourceId  { get; set; }
        public bool NoEcho  { get; set; }
        public string Data { get; set; }
    }
}
