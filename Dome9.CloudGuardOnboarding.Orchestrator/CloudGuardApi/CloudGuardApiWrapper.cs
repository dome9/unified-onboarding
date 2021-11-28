using System;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudGuardApiWrapper : CloudGuardApiWrapperBase
    {

        public async override Task UpdateOnboardingStatus(StatusModel model)
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
    }
}