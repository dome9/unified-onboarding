using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class StatusModel
    {
        public string OnboardingId { get; set; }
        public Feature Feature { get; set; }
        public string StatusMessage { get; set; }
        public bool IsStackStatus { get; set; }

        /// <summary>
        /// String(one of { INACTIVE, ACTIVE, PENDING, ERROR})
        /// only relevant if not stack status
        /// </summary>
        public string ActiveStatus { get; set; }

        public StatusModel()
        {
        }

        public StatusModel(string onboardingId, string statusMessage, Status activeStatus, Feature feature = Feature.General,  bool isStackStatus = false)
        {
            OnboardingId = onboardingId;
            StatusMessage = statusMessage;
            ActiveStatus = activeStatus.ToString();
            Feature = feature;
            IsStackStatus = isStackStatus;
        }

        public StatusModel(string onboardingId, Feature feature, string stackStatusMessage)
        {
            OnboardingId = onboardingId;
            StatusMessage = stackStatusMessage;
            ActiveStatus = null;
            Feature = feature;
            IsStackStatus = true;
        }

        public override string ToString()
        {
            return $"[{nameof(StatusModel)}] OnboardingId:'{OnboardingId}', Feature:'{Feature}', StatusMessage:'{StatusMessage}', IsStackStatus:'{IsStackStatus}'";
        }
    }

    public enum Status
    {
        INACTIVE,
        ACTIVE,
        PENDING,
        ERROR,
    }

    public enum Feature
    {
        General,
        Inventory,
        ContinuousCompliance,
        AccountActivity,
        SecurityGroupManagement,
        ServerlessProtection
    }
}
