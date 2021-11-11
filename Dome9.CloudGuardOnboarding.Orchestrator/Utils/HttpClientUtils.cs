using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class HttpClientUtils
    {
        public enum SerializationOptionsType
        {
            PascalCase,
            CamelCase
        }

        public static StringContent GetContent<T>(T model, SerializationOptionsType serializationOptionsType)
        {
            JsonSerializerOptions jsonSerializerOptions = null;
            switch (serializationOptionsType)
            {

                case SerializationOptionsType.CamelCase:
                    jsonSerializerOptions =  new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    break;

                case SerializationOptionsType.PascalCase:
                    jsonSerializerOptions = new JsonSerializerOptions() { PropertyNamingPolicy = null };
                    break;

                default:
                    throw new NotImplementedException($"SerializationOptionsType: '{serializationOptionsType}'");
            }

            return new StringContent(GetJsonString(model, jsonSerializerOptions), Encoding.UTF8, "application/json");
        }

        private static string GetJsonString<T>(T model, JsonSerializerOptions jsonSerializerOptions)
        {
            return JsonSerializer.Serialize(model, jsonSerializerOptions);
        }       
    }
}
