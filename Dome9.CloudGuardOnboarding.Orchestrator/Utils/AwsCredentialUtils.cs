using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.IdentityManagement;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class AwsCredentialUtils
    {
        public static async Task<string> GetAwsAccountNameAsync(string awsAccountId)
        {
            try
            {
                var iamClient = new AmazonIdentityManagementServiceClient();
                var aliases = await iamClient.ListAccountAliasesAsync();
                var alias = aliases.AccountAliases.FirstOrDefault();
                Console.WriteLine($"[INFO] [{nameof(GetAwsAccountNameAsync)} got alias:{alias}");


                string accountName = string.IsNullOrWhiteSpace(alias) ? awsAccountId : alias;

                return accountName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] [{nameof(GetAwsAccountNameAsync)}] failed. Returning awsAccountId:{awsAccountId} as fallback. ex={ex}");
                return awsAccountId;
            }
        }
    }
}
