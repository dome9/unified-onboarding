namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class StatusModel
    {
        public string OnboardingId { get; set; }
        public string Feature { get; set; }
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

        private StatusModel(string onboardingId, string statusMessage, Enums.Status activeStatus, Enums.Feature feature = Enums.Feature.None,  bool isStackStatus = false)
        {
            OnboardingId = onboardingId;
            StatusMessage = statusMessage;
            ActiveStatus = activeStatus.ToString();
            Feature = feature.ToString();
            IsStackStatus = isStackStatus;
        }
       
        private StatusModel(string onboardingId, Enums.Feature feature, string stackStatusMessage)
        {
            OnboardingId = onboardingId;
            StatusMessage = stackStatusMessage;
            ActiveStatus = null;
            Feature = feature.ToString();
            IsStackStatus = true;            
        }

        public static StatusModel CreateStackStatusModel(string onboardingId, string stackStatusMessage, Enums.Feature feature)
        {
            return new StatusModel(onboardingId, feature, stackStatusMessage);
        }

        public static StatusModel CreateActiveStatusModel(string onboardingId, Enums.Status activeStatus, string statusMessage, Enums.Feature feature = Enums.Feature.None)
        {
            return new StatusModel(onboardingId, statusMessage, activeStatus, feature, false);
        }       

        public override string ToString()
        {
            return $"[{nameof(StatusModel)}] OnboardingId:'{OnboardingId}', Feature:'{Feature}', StatusMessage:'{StatusMessage}', IsStackStatus:'{IsStackStatus}', ActiveStatus:'{ActiveStatus}'";
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
                StatusMessage == other.StatusMessage &&
                ActiveStatus == other.ActiveStatus &&
                Feature == other.Feature &&
                IsStackStatus == other.IsStackStatus;
        }
    }    
}
