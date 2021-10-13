
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
        }

        public enum Feature
        {
            None,
            Inventory,
            ContinuousCompliance,
            ServerlessProtection
        }
    }
}
