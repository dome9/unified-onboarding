using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudGuardApiWrapper : ICloudGuardApiWrapper, IDisposable
    {        
        private HttpClient _httpClient;
        
        // TODO: route may change after PR on controller - update the route
        private const string CONTROLLER_ROUTE = "/v2/UnifiedOnboarding";
        private string _baseUrl;
        private bool disposedValue;

        public void SetLocalCredentials(ServiceAccount cloudGuardServiceAccount)
        {
            _httpClient?.Dispose();
            _httpClient = new HttpClient();

            _baseUrl = cloudGuardServiceAccount.BaseUrl;
            var authenticationString = $"{cloudGuardServiceAccount.ApiCredentials.ApiKeyId}:{cloudGuardServiceAccount.ApiCredentials.ApiKeySecret}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

            //setup reusable http client
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.ConnectionClose = true;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
        }

        /// <summary>
        /// This method should disappear, and instead we should create a method called ReplaceServiceAccount, which gets a new ApiKey+Secret
        /// </summary>
        /// <returns></returns>
        public async Task<ServiceAccount> ReplaceServiceAccount(CredentialsModel model)
        {
            string methodRoute = "ReplaceServiceAccount";
            try
            {
                Console.WriteLine($"[INFO] [{nameof(ReplaceServiceAccount)}] POST method:{methodRoute}, OnboardingId:{model?.OnboardingId}");

                var response = await _httpClient.PostAsync($"{CONTROLLER_ROUTE}/{methodRoute}", HttpClientUtils.GetContent(model, HttpClientUtils.SerializationOptionsType.CamelCase));

                if (response != null && response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content?.ReadAsStringAsync();
                    var cred = JsonSerializer.Deserialize<ServiceAccountCredentials>(jsonString, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    return new ServiceAccount(cred.ApiKeyId, cred.ApiKeySecret, _baseUrl);
                }
                throw new Exception($"Reponse StatusCode:{response?.StatusCode} Reason:{response?.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {nameof(ReplaceServiceAccount)} failed. Error={ex}");
                throw;
            }
        }
        public async Task DeleteServiceAccount(CredentialsModel model)
        {
            string methodRoute = "DeleteServiceAccount";
            try
            {
                Console.WriteLine($"[INFO] [{nameof(DeleteServiceAccount)}] POST method:{methodRoute}, OnboardingId:{model?.OnboardingId}");

                var response = await _httpClient.PostAsync($"{CONTROLLER_ROUTE}/{methodRoute}", HttpClientUtils.GetContent(model, HttpClientUtils.SerializationOptionsType.CamelCase));
                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new Exception($"Reponse StatusCode:{response?.StatusCode} Reason:{response?.ReasonPhrase}");
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {nameof(DeleteServiceAccount)} failed. Error={ex}");
                throw;
            }
        }

        public async Task UpdateOnboardingStatus(StatusModel model)
        {
            string methodRoute = "UpdateStatus";
            try
            {
                Console.WriteLine($"[INFO] [{nameof(UpdateOnboardingStatus)}] POST method:{methodRoute}, model:{model}");

                var response = await _httpClient.PostAsync($"{CONTROLLER_ROUTE}/{methodRoute}", HttpClientUtils.GetContent(model, HttpClientUtils.SerializationOptionsType.CamelCase));
                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new Exception($"Reponse StatusCode:{response?.StatusCode} Reason:{response?.ReasonPhrase}, Reponse:{response}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(UpdateOnboardingStatus)} failed. Error={ex}]");
                throw;
            }
        }

        public async Task ValidateOnboardingId(string onboardingId)
        {
            string methodRoute = "GetStatus";

            try
            {
                Console.WriteLine($"[INFO] [{nameof(ValidateOnboardingId)}] GET method:{methodRoute}, onboardingId:{onboardingId}");

                var response = await _httpClient.GetAsync($"{CONTROLLER_ROUTE}/{methodRoute}/{onboardingId}");
                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new Exception($"Invalid onboarding id. Reponse StatusCode:{response?.StatusCode}, ReasonPhrase:'{response?.ReasonPhrase}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(UpdateOnboardingStatus)} failed. Error={ex}]");
                throw;
            }
        }

        public async Task OnboardAccount(AccountModel model)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{CONTROLLER_ROUTE}", HttpClientUtils.GetContent(model, HttpClientUtils.SerializationOptionsType.CamelCase));
                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new Exception($"OnboardAccount failed. Reponse StatusCode:{response?.StatusCode}, ReasonPhrase:'{response?.ReasonPhrase}', Response:{response}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(OnboardAccount)} failed. ex={ex}]");
                throw;
            }
        }        

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }        
    }
}

