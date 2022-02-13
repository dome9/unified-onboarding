using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator.Retry
{
    public class RetryAndBackoffServiceFactory
    {
        private static IRetryAndBackoffService _retryAndBackoffService;

        private RetryAndBackoffServiceFactory() { }

        public static void Init(string intervalProvider = "")
        {
            if (_retryAndBackoffService != null)
            {
                return;
            }
            switch (intervalProvider)
            {
                default:
                    _retryAndBackoffService = new RetryAndBackoffService(new SimpleExponentialRetryIntervalProvider());
                    break;
            }
        }

        public static IRetryAndBackoffService Get()
        {
            if (_retryAndBackoffService == null)
            {
                throw new NullReferenceException(nameof(_retryAndBackoffService));
            }
            return _retryAndBackoffService;
        }
    }
}
