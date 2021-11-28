using Amazon.CloudFormation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class ChangeSetStatusExtensions
    {
        public static bool IsFinal(this ChangeSetStatus changeSetStatus)
        {
            switch (changeSetStatus)
            {
                case "FAILED":
                case "DELETE_COMPLETE":
                case "CREATE_COMPLETE":
                case "DELETE_FAILED":                    
                    return true;
                    
                default:
                    return false;
            }
        }

        public static bool IsSuccess(this ChangeSetStatus changeSetStatus)
        {
            switch (changeSetStatus)
            {
                case "CREATE_COMPLETE":             
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsFinal(this ExecutionStatus executionStatus)
        {
            switch (executionStatus)
            {
                case "UNAVAILABLE":
                case "AVAILABLE":
                case "EXECUTE_COMPLETE":
                case "EXECUTE_FAILED":
                case "OBSOLETE":
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsSuccess(this ExecutionStatus executionStatus)
        {
            switch (executionStatus)
            {
                case "EXECUTE_COMPLETE":
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsReady(this ExecutionStatus executionStatus)
        {
            switch (executionStatus)
            {
                case "AVAILABLE":
                    return true;

                default:
                    return false;
            }
        }
    }
}
