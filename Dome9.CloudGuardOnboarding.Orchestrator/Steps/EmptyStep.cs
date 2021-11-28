using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Steps
{
    public class EmptyStep : IStep
    {
        public Task Cleanup()
        {
            return Task.CompletedTask;
        }

        public Task Execute()
        {
            return Task.CompletedTask;
        }

        public Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
