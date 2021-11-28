using Amazon.CloudFormation;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class CloudGuardChangeSetStatus
    {
        public bool? HasChanges { get; set; }
        public ExecutionStatus ExecutionStatus { get; set; }
        public ChangeSetStatus ChangeSetStatus { get; set; }
        public string StatusReason { get; set; }

        public override string ToString()
        {
            return $"{nameof(HasChanges)}='{HasChanges}', {nameof(ExecutionStatus)}='{ExecutionStatus}', {nameof(ChangeSetStatus)}='{ChangeSetStatus}', {nameof(StatusReason)}='{StatusReason}'";
        }

        
    }
}
