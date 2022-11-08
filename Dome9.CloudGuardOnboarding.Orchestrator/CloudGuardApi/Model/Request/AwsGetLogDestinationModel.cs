
namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request
{
    public class AwsGetLogDestinationModel
    {
        public AwsGetLogDestinationModel() { }
        public AwsGetLogDestinationModel(string cloudAccountId)
        {
            CloudAccountId = cloudAccountId;
            LogType = "cloudtrail";
        }

        public string CloudAccountId { get; set; }
        public string LogType { get; set; }
    }
}
