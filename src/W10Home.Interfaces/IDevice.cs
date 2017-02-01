using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;

namespace W10Home.Interfaces
{
	public interface IDevice
	{
		Task InitializeAsync(IDeviceConfiguration configuration);
		Task<IEnumerable<IChannel>> GetChannelsAsync();
	}
}
