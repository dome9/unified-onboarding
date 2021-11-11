
namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class Enums
    {
        public enum Status
        {
            INACTIVE,
            ACTIVE,
            PENDING,
            ERROR,
            WARNING
        }

        public enum Feature
        {
            None,
            Inventory,
            ContinuousCompliance,
            ServerlessProtection,
            Intelligence
        }
    }
}
