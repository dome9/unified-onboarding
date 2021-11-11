namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ServelessAddAccountModel
    {
        /// <summary>
        /// This is the AWS Account Number (NOT CloudAccountEntity.Id)
        /// </summary>
        public string CloudAccountId { get; set; }

        public ServerlessSupportedCloudProvider CloudProvider { get; set; }

        public ServelessAddAccountModel(string cloudAccountId)
        {
            CloudAccountId = cloudAccountId;
            CloudProvider = ServerlessSupportedCloudProvider.aws;
        }
    }

    public enum ServerlessSupportedCloudProvider
    {
        aws,
        //azure //currently not supported by Unified-Onboarding
    }
}
