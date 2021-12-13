namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class StatusModel
    {
        public StatusModel()
        {
        }

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

        public StatusModel(string onboardingId, Enums.Feature feature, Enums.Status status, string message, string stackStatus, string stackMessage, string remediationRecommendation)
        {
            OnboardingId = onboardingId;
            Feature = feature.ToString();
            Status = status == Enums.Status.None ? null : status.ToString();            
            Message = message;
            StackStatus = stackStatus;
            StackMessage = stackMessage;
            RemediationRecommendation = remediationRecommendation;            
        }

        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                return false;
            }

            var other = obj as StatusModel;
            
            return
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
