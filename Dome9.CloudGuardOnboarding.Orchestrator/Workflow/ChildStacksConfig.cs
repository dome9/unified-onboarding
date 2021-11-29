using System.Linq;
using System.Collections.Generic;
using System;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public enum OnboardingType
    {
        RoleBased,
        UserBased

    }

    public class ChildStacksConfig
    {

        public static Dictionary<Enums.Feature, string> GetSupportedFeaturesStackNames(OnboardingType onboardingType)
        {
            return SupportedStackes[onboardingType];
        }

        private static Dictionary<Enums.Feature, string> FeatureStackNames
        {
            get => new Dictionary<Enums.Feature, string>()
                {
                    { Enums.Feature.Intelligence, "CloudGuard-Onboarding-Intelligence"},
                    { Enums.Feature.ServerlessProtection, "CloudGuard-Onboarding-Serverless"},
                    { Enums.Feature.Permissions, "CloudGuard-Onboarding-Permissions"}
                };
        }

        private static Dictionary<OnboardingType, Dictionary<Enums.Feature, string>> SupportedStackes
        {
            get => new Dictionary<OnboardingType, Dictionary<Enums.Feature, string>>()
                {
                    {
                        OnboardingType.RoleBased, new Dictionary<Enums.Feature, string>()
                        {
                            { Enums.Feature.Permissions, FeatureStackNames[Enums.Feature.Permissions]},
                            { Enums.Feature.ServerlessProtection, FeatureStackNames[Enums.Feature.ServerlessProtection]},
                            { Enums.Feature.Intelligence, FeatureStackNames[Enums.Feature.Intelligence]},
                        }
                    },
                    {
                        OnboardingType.UserBased, new Dictionary<Enums.Feature, string>()
                        {
                            { Enums.Feature.Permissions, FeatureStackNames[Enums.Feature.Permissions]},
                        }
                    },
                };
        }

    }
}
