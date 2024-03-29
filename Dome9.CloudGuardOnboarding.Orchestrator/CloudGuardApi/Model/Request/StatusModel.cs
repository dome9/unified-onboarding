﻿namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public enum OnboardingAction
    {
        Create,
        Update,
        Delete,
    }

    public class StatusModel
    {
        public StatusModel()
        {
        }

        public string Action { get; set; }
        public string OnboardingId { get; set; }
        public string Feature { get; set; }
        public string Status { get; set; }
        public string StackStatus { get; set; }
        public string Message { get; set; }
        public string StackMessage { get; set; }
        public string RemediationRecommendation { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(OnboardingId)}: '{OnboardingId}', " +
                $"{nameof(Feature)}: '{Feature}', " +
                $"{nameof(Status)}: '{Status}', " +
                $"{nameof(StackStatus)}: '{StackStatus}', " +
                $"{nameof(Message)}: '{Message}', " +
                $"{nameof(StackMessage)}: '{StackMessage}', " +
                $"{nameof(RemediationRecommendation)}: '{RemediationRecommendation}'";
        }

        public StatusModel(string onboardingId, Enums.Feature feature, Enums.Status status, string message, string stackStatus, string stackMessage, string remediationRecommendation, OnboardingAction action = OnboardingAction.Create)
        {
            Action = action.ToString();
            OnboardingId = onboardingId;
            Feature = feature.ToString();
            Status = status == Enums.Status.None ? null : status.ToString();            
            Message = message;
            StackStatus = stackStatus;
            StackMessage = stackMessage;
            RemediationRecommendation = remediationRecommendation;            
        }

        public StatusModel(string onboardingId, Enums.Feature feature, Enums.Status status, string message, OnboardingAction action = OnboardingAction.Create)
        {
            Action = action.ToString();
            OnboardingId = onboardingId;
            Feature = feature.ToString();
            Status = status == Enums.Status.None ? null : status.ToString();
            Message = message;
            StackStatus = null;
            StackMessage = null;
            RemediationRecommendation = null;
        }

        public StatusModel(string onboardingId, Enums.Feature feature, string stackStatus, string stackMessage, OnboardingAction action = OnboardingAction.Create)
        {
            Action = action.ToString();
            OnboardingId = onboardingId;
            Feature = feature.ToString();
            Status = null;
            Message = null;
            StackStatus = stackStatus;
            StackMessage = stackMessage;
            RemediationRecommendation = null;
        }

        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                return false;
            }

            var other = obj as StatusModel;
            
            return
                Action == other.Action &&
                OnboardingId == other.OnboardingId &&
                Feature == other.Feature &&
                Status == other.Status &&
                Message == other.Message &&
                StackStatus == other.StackStatus &&
                StackMessage == other.StackMessage &&
                RemediationRecommendation == other.RemediationRecommendation;
        }
    }    
}
