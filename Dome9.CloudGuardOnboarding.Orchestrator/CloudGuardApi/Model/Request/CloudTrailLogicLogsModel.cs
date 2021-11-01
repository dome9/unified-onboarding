using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudTrailLogicLogsModel
    {
        public CloudTrailLogicLogsModel()
        {
        }

        public CloudTrailLogicLogsModel(List<string> externalAccountNumbers)
        {
            ExternalAccountNumbers = externalAccountNumbers;
        }

        public List<string> ExternalAccountNumbers;
    }

    public class TrailLogicLogsViewModel
    {
        public TrailLogicLogsViewModel(List<TrailLogicLogViewModel> cloudTrailLogs)
        {
            logicLogs = cloudTrailLogs;
        }

        public List<TrailLogicLogViewModel> logicLogs { get; set; }
    }

    public class TrailLogicLogViewModel
    {       
        public string CloudAccountId { get; set; }
        public string CloudAccountName { get; set; }
        public string LogGroupName { get; set; }
        public string SubscriptionFilterName { get; set; }
        public string BucketName { get; set; }
        public List<AwsS3Subscription> SnsSubscriptionEvent { get; set; }
        public List<AwsTrailViewModel> Trails { get; set; }
        public bool IsOrganizationTrail { get; set; }
    }

    public class AwsTrailViewModel
    {
        public string TrailId { get; set; }
        public string TrailName { get; set; }
        public string Region { get; set; }
    }

    public class AwsS3Subscription
    {
        public string Id { get; set; }
        public string Topic { get; set; }
        public List<AwsS3FilterRule> FilterRules { get; set; }
        public AwsS3Subscription(string id, string topic, List<AwsS3FilterRule> rules)
        {
            Id = id;
            Topic = topic;
            FilterRules = rules;
        }
    }

    public class AwsS3FilterRule
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public AwsS3FilterRule(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public class MagellanOnboardingModel
    {        
        public string CloudAccountId { get; set; }        
        public string BucketName { get; set; }
        public bool IsUnifiedOnboarding { get; set; }
    }
}
