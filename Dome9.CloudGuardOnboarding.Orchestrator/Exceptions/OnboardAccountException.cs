using System;
using System.Linq;
using System.Net;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardAccountException : OnboardingException
    {
        public OnboardAccountException(
                string shortMessage,
                string reasonPhrase,
                HttpStatusCode httpStatusCode,
                string content,
                Enums.Feature feature) 
            : base(shortMessage, feature)
        {

            ReasonPhrase = reasonPhrase;
            HttpStatusCode = httpStatusCode;
            Content = content;

            Summary = $"{shortMessage}. Reponse StatusCode:{httpStatusCode}, ReasonPhrase:'{reasonPhrase}', Content:'{content}'";
        }
                
        public HttpStatusCode HttpStatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Content { get; set; }
        public string Summary { get; set; }

        public override string Message => TryGetCleanMessageFromContent();

        private string TryGetCleanMessageFromContent()
        {
            // In case account already exists, we get for example:
            //"statusMessage": "Onboarding failed with following error: 'System.Exception: OnboardAccount failed. Reponse StatusCode:InternalServerError, ReasonPhrase:'Internal Server Error', Content:'Failed to add cloud account. ResultCode: 'AccountAlreadyExist', Message: 'AWS account 488017920092 is already protected by Dome9.''\n   at Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApiWrapper.OnboardAccount(AccountModel model)\n   at Dome9.CloudGuardOnboarding.Orchestrator.RetryAndBackoffService.RunAsync(Func`1 operation, Int32 maxTryCount)\n   at Dome9.CloudGuardOnboarding.Orchestrator.RetryAndBackoffService.RunAsync(Func`1 operation, Int32 maxTryCount)\n   at Dome9.CloudGuardOnboarding.Orchestrator.OnboardingWorkflow.RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)'",
            try
            {
                if (!string.IsNullOrWhiteSpace(Content) && Content.Contains("Message: "))
                {
                   return Content.Split("Message: ").Last().Trim('\'');                    
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR][{nameof(OnboardAccountException)}.{nameof(TryGetCleanMessageFromContent)}] Internal parse error, using summary as default message. Error: {ex}");
            }

            return Summary;
        }
    }
}
