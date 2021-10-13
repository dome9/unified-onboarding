using System;
using System.Linq;
using System.Net;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardAccountException : OnboardingException
    {
        public OnboardAccountException(string message) : base(message, Enums.Feature.ContinuousCompliance) { }

        public HttpStatusCode HttpStatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Content { get; set; }

        public override string ToString()
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
                Console.WriteLine($"[ERROR][{nameof(OnboardAccountException)}] ToString() internal parse error, using default ToString(). Error: {ex}");
            }

            return base.ToString();
        }
    }
}
