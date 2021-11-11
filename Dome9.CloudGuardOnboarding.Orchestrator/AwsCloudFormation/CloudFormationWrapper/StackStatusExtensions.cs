using System;
using System.Collections.Generic;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class StackStatusExtensions
    {
        public class StatusInfo
        {
            public bool IsFinal { get; set; }
            public bool IsError { get; set; }
        }

        /// <summary>
        /// Categorized statuses - IMPORTANT - we are assuming that ROLLBACK is set to TRUE (default) 
        /// </summary>
        private readonly static Dictionary<string, StatusInfo> StatusInfoMap = new Dictionary<string, StatusInfo>()
        {
            // good final statuses (true, false)
            { "CREATE_COMPLETE", new StatusInfo { IsFinal = true, IsError = false }},
            { "UPDATE_COMPLETE", new StatusInfo { IsFinal = true, IsError = false }},
            { "DELETE_COMPLETE", new StatusInfo { IsFinal = true, IsError = false }},
            { "IMPORT_COMPLETE", new StatusInfo { IsFinal = true, IsError = false }},
            { "IMPORT_ROLLBACK_COMPLETE", new StatusInfo { IsFinal = true, IsError = false }},

            // failed final statuses (true, true)
            { "ROLLBACK_COMPLETE", new StatusInfo { IsFinal = true, IsError = true }},
            { "ROLLBACK_FAILED", new StatusInfo { IsFinal = true, IsError = true }},
            { "UPDATE_ROLLBACK_COMPLETE", new StatusInfo { IsFinal = true, IsError = true }},
            { "DELETE_FAILED", new StatusInfo { IsFinal = true, IsError = true }},
            { "IMPORT_ROLLBACK_FAILED", new StatusInfo { IsFinal = true, IsError = true }},  
            
            // failed non final statuses (false, true)
            { "UPDATE_ROLLBACK_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = true }},
            { "UPDATE_ROLLBACK_FAILED", new StatusInfo { IsFinal = false, IsError = true }},
            { "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = true }},            
            { "ROLLBACK_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = true }},
            { "CREATE_FAILED", new StatusInfo { IsFinal = false, IsError = true }},
            { "IMPORT_ROLLBACK_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = true }},

            // good non final statuses (false, false)
            { "CREATE_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = false }},
            { "DELETE_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = false }},                    
            { "UPDATE_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = false }},
            { "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = false }},   
            { "REVIEW_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = false }},
            { "IMPORT_IN_PROGRESS", new StatusInfo { IsFinal = false, IsError = false }},
        };


        public static bool IsFinal(this StackStatus status)
        {
            if( StatusInfoMap.TryGetValue(status, out StatusInfo info))
            {
                return info.IsFinal;
            }
            throw new ArgumentOutOfRangeException(status);
        }

        public static bool IsError(this StackStatus status)
        {
            if (StatusInfoMap.TryGetValue(status, out StatusInfo info))
            {
                return info.IsError;
            }
            throw new ArgumentOutOfRangeException(status);
        }

        public static string ToDetailedString(this StackSummary stackSummary)
        {
            return $"StackName:'{stackSummary.StackName}' Status:'{stackSummary.StackStatus}', Reason:'{stackSummary.StackStatusReason}',  LastUpdated:{stackSummary.LastUpdatedTime}";
        }
    }
}
