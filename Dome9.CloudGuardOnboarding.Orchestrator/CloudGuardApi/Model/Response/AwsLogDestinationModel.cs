using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class AwsLogDestination
    {
        public string BucketName { get; set; }
        public string BucketAccountId { get; set; }
        public bool IsOnboarded { get; set; }
        public bool IsCentralized { get; set; }
    }
}
