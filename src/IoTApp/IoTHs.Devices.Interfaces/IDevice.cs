using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHs.Api.Shared;

namespace IoTHs.Devices.Interfaces
{
    public interface IDevice
    {
        string Name { get; }
        string Type { get; }
        IEnumerable<IDevice> Devices { get; }
        IEnumerable<IDeviceChannel> Channels { get; }
        IDeviceChannel GetChannel(string name);
        Task InitializeAsync(DeviceConfigurationModel configuration);
        Task TearDownAsync();
    }
}