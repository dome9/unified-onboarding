
namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class Enums
    {
        public enum Status
        {
            None,
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
            Permissions,
            Posture,
            ServerlessProtection,
            Intelligence
        }
    }
}
