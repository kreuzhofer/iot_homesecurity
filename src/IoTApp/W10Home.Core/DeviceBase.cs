using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;

namespace W10Home.Core.Standard
{
	public abstract class DeviceBase : IDevice
	{
	    public abstract string Name { get; }
	    public abstract string Type { get; }
	    public abstract Task InitializeAsync(DevicePluginConfigurationModel configuration);
		public abstract IEnumerable<IDeviceChannel> GetChannels();

	    public IEnumerable<IDeviceChannel> Channels => GetChannels();

	    public IDeviceChannel GetChannel(string name)
		{
			return GetChannels().Single(c => c.Name == name);
		}
		public abstract Task TeardownAsync();
	}
}