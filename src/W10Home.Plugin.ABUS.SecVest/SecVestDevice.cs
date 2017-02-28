using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;

namespace W10Home.Plugin.ABUS.SecVest
{
    public class SecVestDevice : IDevice
    {
	    public Task InitializeAsync(IDeviceConfiguration configuration)
	    {
		    throw new NotImplementedException();
	    }

	    public Task<IEnumerable<IChannel>> GetChannelsAsync()
	    {
		    throw new NotImplementedException();
	    }

	    public Task Teardown()
	    {
		    throw new NotImplementedException();
	    }
    }
}
