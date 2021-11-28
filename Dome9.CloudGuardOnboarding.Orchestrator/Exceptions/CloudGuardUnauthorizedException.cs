using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudGuardUnauthorizedException : Exception
    {
        public CloudGuardUnauthorizedException(string message) : base(message)
        {
        }
    }
}
