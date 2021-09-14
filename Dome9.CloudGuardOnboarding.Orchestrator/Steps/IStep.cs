using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    /// <summary>
    /// Represents onboarding workflow steps that can have rollback or cleanup actions
    /// </summary>
    public interface IStep
    {
        Task Execute();
        Task Rollback();
        Task Cleanup();
    }
}
