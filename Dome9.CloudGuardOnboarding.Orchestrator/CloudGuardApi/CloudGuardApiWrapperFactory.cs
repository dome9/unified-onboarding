using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi
{
    public class CloudGuardApiWrapperFactory
    {
        private static ICloudGuardApiWrapper _cloudGuardApiWrapper;

        private CloudGuardApiWrapperFactory() { }

        public static void Init(string cloudGuardApiKeyId, string cloudGuardApiKeySecret, string apiBaseUrl, string wrapper = "")
        {
            if (_cloudGuardApiWrapper != null)
            {
                return;
            }
            switch (wrapper)
            {
                case "silent":
                    _cloudGuardApiWrapper = new CloudGuardApiWrapperSilent(cloudGuardApiKeyId, cloudGuardApiKeySecret, apiBaseUrl);
                    break;
                case "mock":
                    _cloudGuardApiWrapper = new CloudGuardApiWrapperMock();
                    break;
                default:
                    _cloudGuardApiWrapper = new CloudGuardApiWrapper(cloudGuardApiKeyId, cloudGuardApiKeySecret, apiBaseUrl);
                    break;
            }
        }

        public static ICloudGuardApiWrapper Get()
        {

            if (_cloudGuardApiWrapper == null)
            {
                throw new NullReferenceException(nameof(_cloudGuardApiWrapper));
            }

            return _cloudGuardApiWrapper;
        }
    }
}
