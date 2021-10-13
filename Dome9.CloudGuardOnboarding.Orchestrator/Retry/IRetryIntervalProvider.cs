using System;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public interface IRetryIntervalProvider
    {
        TimeSpan[] GetDelayIntervals(int intervalCount);
    }
}
