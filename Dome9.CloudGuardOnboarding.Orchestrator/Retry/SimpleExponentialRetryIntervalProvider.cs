using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class SimpleExponentialRetryIntervalProvider : IRetryIntervalProvider
    {
        private ConcurrentDictionary<int, TimeSpan[]> _cachedIntervals = new ConcurrentDictionary<int, TimeSpan[]>();
        /// <summary>
        /// Simple exponential series, e.g. 1, 2, 4, 8, 16 ...
        /// </summary>
        /// <param name="intervalCount"></param>
        /// <returns></returns>
        public TimeSpan[] GetDelayIntervals(int intervalCount)
        {
            return _cachedIntervals.GetOrAdd(intervalCount, CreateDelayIntervals(intervalCount));
        }

        private TimeSpan[] CreateDelayIntervals(int intervalCount)
        {
            List<TimeSpan> intervals = new List<TimeSpan>(intervalCount);
            for (int i = 0; i < intervalCount; i++)
            {
                intervals.Add(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
            return intervals.ToArray();
        }
    }
}
