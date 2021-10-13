namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class ServerlessStackWrapper : StackWrapperBase
    {
        public ServerlessStackWrapper(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base(apiProvider, retryAndBackoffService)
        {
        }

        protected override Enums.Feature Feature => Enums.Feature.ServerlessProtection;
    }
}
