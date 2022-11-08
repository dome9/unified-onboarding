using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request
{
    public class IntelligenceOnboardingModel
    {
       public IntelligenceOnboardingModel(){}
       public IntelligenceOnboardingModel(string bucketName, string bucketAccountId, string topicArn,
           List<string> cloudAccountIds, string onboardingType, bool isUnifiedOnboarding, List<long> rulesetsIds)
       {
            BucketName = bucketName;
            BucketAccountId = bucketAccountId;
            TopicArn = topicArn;
            CloudAccountIds = cloudAccountIds;
            OnboardingType = onboardingType;
            IsUnifiedOnboarding = isUnifiedOnboarding;
            RulesetsIds = rulesetsIds;
            IsSubscribedAlready = false;
       }

       public IntelligenceOnboardingModel(string bucketAccountId, string onboardingType, bool isUnifiedOnboarding, List<long> rulesetsIds, bool isSubscribedAlready)
       {
            BucketName="";
            BucketAccountId = bucketAccountId;
            TopicArn="";
            CloudAccountIds= new List<string>();  
            OnboardingType = onboardingType;
            IsUnifiedOnboarding = isUnifiedOnboarding;
            RulesetsIds = rulesetsIds;
            IsSubscribedAlready = isSubscribedAlready;
       }

       public string BucketName { get; set; }
       public string BucketAccountId { get; set; }
       public string TopicArn { get; set; }
       public List<string> CloudAccountIds { get; set; }
       public string OnboardingType { get; set; }
       public bool IsUnifiedOnboarding { get; set; }
       public List<long> RulesetsIds { get; set; }
       public bool IsSubscribedAlready { get; set; }
    }
}
