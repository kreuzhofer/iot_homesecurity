using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.AzureIoTHub
{
    public interface IAzureIoTHubDevice : IDevice
    {
        string Name { get; }
        string Type { get; }
        string ServiceBaseUrl { get; }
        string ApiKey { get; }
        string DeviceId { get; }
        IEnumerable<IDeviceChannel> Channels { get; }
        Task InitializeAsync(DevicePluginConfigurationModel configuration);
        IEnumerable<IDeviceChannel> GetChannels();
        Task TeardownAsync();
        IDeviceChannel GetChannel(string name);
    }
}