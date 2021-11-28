using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudGuardApiWrapperSilent : CloudGuardApiWrapperBase
    {
        public override Task UpdateOnboardingStatus(StatusModel model)
        {
            return Task.CompletedTask;
        }
    }
}
