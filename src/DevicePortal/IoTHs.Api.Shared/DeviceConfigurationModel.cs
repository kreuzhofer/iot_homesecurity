using System.Collections.Generic;

namespace IoTHs.Api.Shared
{
    public class DeviceConfigurationModel
    {
        public string DeviceId { get; set; }
        public string ServiceBaseUrl { get; set; }
        public List<DevicePluginConfigurationModel> DevicePluginConfigurations { get; set; }
        public List<string> DeviceFunctionIds { get; set; }
    }
}