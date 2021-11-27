using System;
using System.Collections.Generic;
using System.Text;

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
