using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi.Model.Request
{
    public class IntelligenceOnboardingModel
    {
        public string CloudAccountId { get; set; }
        public string BucketName { get; set; }
        public bool IsUnifiedOnboarding { get; set; }
        public List<long> RulesetsIds { get; set; }
    }
}
