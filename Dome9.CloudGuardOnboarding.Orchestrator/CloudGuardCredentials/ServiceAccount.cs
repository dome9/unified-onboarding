namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ServiceAccount
    {
        public ApiCredentials ApiCredentials { get; set; }
        public string BaseUrl { get; set; }

        public ServiceAccount(string apiKeyId, string apiKeySecret, string baseUrl)
        {
            BaseUrl = baseUrl;
            ApiCredentials = new ApiCredentials
            {
                ApiKeyId = apiKeyId,
                ApiKeySecret = apiKeySecret
            };
        }
    }
}