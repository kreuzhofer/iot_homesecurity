using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;

namespace W10Home.Interfaces
{
	public interface IDevice
	{
        string Name { get; }
        string Type { get; }
		Task InitializeAsync(IDeviceConfiguration configuration);
		IEnumerable<IDeviceChannel> GetChannels();
		IDeviceChannel GetChannel(string name);
		Task TeardownAsync();
	}
}
