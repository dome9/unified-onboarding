using System.Collections.Generic;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsS3
{
    public class AwsS3
    {
        public AwsS3(){}

        public AwsS3EventNotificationFilterRule CreateAwsS3FilterRule(FilterRule filterRule)
        {
            return (filterRule == null) ? null : new AwsS3EventNotificationFilterRule(filterRule.Name, filterRule.Value);
        }

        public AwsS3EventNotification CreateS3EventNotifications(TopicConfiguration topicConfiguration)
        {
            if (topicConfiguration == null) return null;
            var s3RuleList = new List<AwsS3EventNotificationFilterRule>();
            if (topicConfiguration.Filter?.S3KeyFilter?.FilterRules != null)
            {
                var ruleList = topicConfiguration.Filter.S3KeyFilter.FilterRules.Select(rule => CreateAwsS3FilterRule(rule)).ToList();
                s3RuleList.AddRange(ruleList);
            }
            var eventTypes = new List<EventType>();
            if (topicConfiguration.Events != null)
            {
                eventTypes = topicConfiguration.Events;
            }
            return new AwsS3EventNotification(topicConfiguration.Id, topicConfiguration.Topic, s3RuleList, eventTypes);
        }
    }

    public class AwsS3EventNotification
    {
        public string Id { get; set; }
        public string Topic { get; set; }
        public List<AwsS3EventNotificationFilterRule> FilterRules { get; set; }
        public List<Amazon.S3.EventType> EventTypes { get; set; }

        public AwsS3EventNotification() { }
        public AwsS3EventNotification(string id, string topic, List<AwsS3EventNotificationFilterRule> rules, List<Amazon.S3.EventType> eventTypes)
        {
            Id = id;
            Topic = topic;
            FilterRules = rules;
            EventTypes = eventTypes;
        }
    }

    public class AwsS3EventNotificationFilterRule
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public AwsS3EventNotificationFilterRule() { }
        public AwsS3EventNotificationFilterRule(string name, string value)
        {
            Name = name; // Valid Values: prefix | suffix
            Value = value;
        }
    }

}
