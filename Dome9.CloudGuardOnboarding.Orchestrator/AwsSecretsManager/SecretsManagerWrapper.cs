using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsSecretsManager
{
    public class SecretsManagerWrapper : ISecretsManagerWrapper
    {
        private readonly AmazonSecretsManagerClient _client;
        private static ISecretsManagerWrapper _wrapper = null;
        private static readonly object _instanceLock = new object();

        private SecretsManagerWrapper()
        {
            _client = new AmazonSecretsManagerClient();
        }

        public static ISecretsManagerWrapper Get()
        {
            lock (_instanceLock)
            {
                return _wrapper ??= new SecretsManagerWrapper();
            }
        }

        public async Task<ApiCredentials> GetCredentialsFromSecretsManager(string key)
        {
            const string tagKey = "aws:cloudformation:logical-id";
            string tagValue = key;
            const string accessKeyId = "ACCESS_KEY";
            const string accessKeySecret = "SECRET_KEY";

            try
            {
                var secrets = await SecretsManagerListSecrets();
                if (!(secrets.Any()))
                {
                    throw new OnboardingException("No secrets found in secrets manager", Enums.Feature.Permissions);
                }

                var secretEntry = secrets.FirstOrDefault(s => s.Tags.Any(t => t.Key.Equals(tagKey) && t.Value.Equals(tagValue)));

                var secretString = await SecretsManagerGetSecretValue(secretEntry?.ARN);
                if (string.IsNullOrWhiteSpace(secretString))
                {
                    throw new OnboardingException($"Secret not found in secrets manager for secret entry with ARN '{secretEntry?.ARN}'", Enums.Feature.Permissions);
                }

                var secretDict = JsonSerializer.Deserialize<Dictionary<string, string>>(secretString);
                var credentials = new ApiCredentials { ApiKeyId = secretDict[accessKeyId], ApiKeySecret = secretDict[accessKeySecret] };
                return credentials;
            }
            catch (OnboardingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetCredentialsFromSecretsManager)}] Failed. Error={ex}");
                throw new OnboardingException(ex.Message, Enums.Feature.Permissions);
            }
        }



        private async Task<List<SecretListEntry>> SecretsManagerListSecrets()
        {
            using (var client = new AmazonSecretsManagerClient())
            {
                var response = await client.ListSecretsAsync(
                    new ListSecretsRequest
                    {
                        Filters = new List<Filter>
                        {
                            new Filter
                            {
                                Key = "tag-key",
                                Values = new List<string>{ "aws:cloudformation:logical-id" }
                            }
                        }
                    });

                Console.WriteLine($"[INFO] [{nameof(SecretsManagerListSecrets)}] ResponseHttpStatusCode='{response?.HttpStatusCode}'");

                return response?.SecretList;
            }
        }

        private async Task<string> SecretsManagerGetSecretValue(string secretId)
        {
            if (string.IsNullOrWhiteSpace(secretId))
            {
                throw new ArgumentException("secretId (ARN) is null or empty");
            }

            var client = new AmazonSecretsManagerClient();
            var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId });

            return response?.SecretString;
        }
    }
}
