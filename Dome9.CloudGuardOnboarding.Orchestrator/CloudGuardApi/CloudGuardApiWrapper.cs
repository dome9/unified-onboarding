using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudGuardApiWrapper : ICloudGuardApiWrapper, IDisposable
    {
        private HttpClient _httpClient;

        private const string CONTROLLER_ROUTE = "/v2/UnifiedOnboarding";
        private const string SERVERLESS_ADD_ACCOUNT_ROUTE = "/v2/serverless/accounts";
        private string _baseUrl;
        private bool disposedValue;
        private static StatusModel _lastStatus = new StatusModel();
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private const string INTELLIGENCE_ENABLE_ACCOUNT_IN_BACKEND = "/v2/view/magellan/magellan-cloudtrail-onboarding";

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
                throw new OnboardingException(ex.Message, Enums.Feature.None);
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
                throw new OnboardingException(ex.Message, Enums.Feature.None);
            }
        }

        public async Task UpdateOnboardingStatus(StatusModel model)
        {
            await _semaphore.WaitAsync();

            try
            {
                string methodRoute = "UpdateStatus";
                if (_lastStatus.Equals(model))
                {
                    Console.WriteLine($"[INFO] [{nameof(UpdateOnboardingStatus)}] Status is same as previous, hence will not be post update to server.");
                    return;
                }

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
            finally
            {
                _semaphore.Release();
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
                throw new OnboardingException(ex.Message, Enums.Feature.None);
            }
        }

        public async Task OnboardAccount(AccountModel model)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{CONTROLLER_ROUTE}", HttpClientUtils.GetContent(model, HttpClientUtils.SerializationOptionsType.CamelCase));
                if (response == null)
                {
                    if (response == null)
                    {
                        throw new Exception("OnboardAccount failed. Response is null.");
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content?.ReadAsStringAsync();
                    throw new OnboardAccountException($"OnboardAccount failed. Reponse StatusCode:{response.StatusCode}, ReasonPhrase:'{response.ReasonPhrase}', Content:'{responseContent}'")
                    {
                        ReasonPhrase = response?.ReasonPhrase,
                        HttpStatusCode = response.StatusCode,
                        Content = responseContent
                    };
                }
            }
            catch (OnboardAccountException ex)
            {
                Console.WriteLine($"[Error] [{nameof(OnboardAccount)} failed. ex={ex}]");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(OnboardAccount)} failed. ex={ex}]");
                throw new OnboardingException(ex.Message, Enums.Feature.None);
            }
        }

        public async Task ServerlessAddAccount(ServelessAddAccountModel model)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{SERVERLESS_ADD_ACCOUNT_ROUTE}", HttpClientUtils.GetContent(model, HttpClientUtils.SerializationOptionsType.CamelCase));
                if (response == null)
                {
                    if (response == null)
                    {
                        throw new Exception("Serverless add account failed. Response is null.");
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content?.ReadAsStringAsync();

                    throw new Exception($"Serverless add account failed failed. Reponse StatusCode:{response.StatusCode}, ReasonPhrase:'{response.ReasonPhrase}', Content:'{responseContent}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(OnboardAccount)} failed. ex={ex}]");
                throw new OnboardingException(ex.Message, Enums.Feature.ServerlessProtection);
            }
        }

        public async Task<ConfigurationsResponseModel> GetConfiguration(ConfigurationsRequestModel model)
        {

            try
            {
                var response = await _httpClient.GetAsync($"{CONTROLLER_ROUTE}/Configuration/{model.OnboardingId}");
                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to get configuration from CloudGuard. Reponse StatusCode:{response?.StatusCode}, ReasonPhrase:'{response?.ReasonPhrase}'");
                }

                var jsonString = await response.Content?.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ConfigurationsResponseModel>(jsonString, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(GetConfiguration)} failed. Error={ex}]");
                throw new OnboardingException(ex.Message, Enums.Feature.None);
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

        public async Task OnboardIntelligence(IntelligenceOnboardingModel data)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{INTELLIGENCE_ENABLE_ACCOUNT_IN_BACKEND}", HttpClientUtils.GetContent(data, HttpClientUtils.SerializationOptionsType.CamelCase));
                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new Exception($"Intelligence {INTELLIGENCE_ENABLE_ACCOUNT_IN_BACKEND} failed. could not enable account to Intelligence. Response StatusCode:{response?.StatusCode}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(OnboardAccount)} failed. ex={ex}]");
                throw;
            }
        }
    }
}