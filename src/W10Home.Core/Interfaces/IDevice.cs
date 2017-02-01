using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Configuration;

namespace W10Home.Core.Interfaces
{
	public interface IDevice
	{
		Task InitializeAsync(DeviceConfiguration configuration);
		Task<IEnumerable<IChannel>> GetChannelsAsync();
	}
}
