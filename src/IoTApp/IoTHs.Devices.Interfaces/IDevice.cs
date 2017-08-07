using IoTHs.Api.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTHs.Devices.Interfaces
{
	public interface IDevice
	{
        string Name { get; }
        string Type { get; }
		Task InitializeAsync(DevicePluginConfigurationModel configuration);
		IEnumerable<IDeviceChannel> GetChannels();
	    IEnumerable<IDeviceChannel> Channels { get; }
		IDeviceChannel GetChannel(string name);
		Task TeardownAsync();
	}
}
