using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;

namespace W10Home.Core.Standard
{
	public abstract class DeviceBase : IDevice
	{
		public abstract Task InitializeAsync(IDeviceConfiguration configuration);
		public abstract IEnumerable<IDeviceChannel> GetChannels();
		public IDeviceChannel GetChannel(string name)
		{
			return GetChannels().Single(c => c.Name == name);
		}
		public abstract Task Teardown();
	}
}