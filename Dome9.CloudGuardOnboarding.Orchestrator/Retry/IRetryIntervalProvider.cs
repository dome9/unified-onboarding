using System;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Retry
{
    public interface IRetryIntervalProvider
    {
        TimeSpan[] GetDelayIntervals(int intervalCount);
    }
}
