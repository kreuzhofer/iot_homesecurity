using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;
using Windows.Web.Http;

namespace W10Home.Plugin.ABUS.SecVest
{
    public class SecVestDevice : IDevice
    {
		private HttpClient _httpClient;
		private List<IChannel> _channels = new List<IChannel>();

		public async Task InitializeAsync(IDeviceConfiguration configuration)
	    {
			var connectionString = configuration.Properties["ConnectionString"];
			var username = configuration.Properties["Username"];
			var password = configuration.Properties["Password"];

			_channels.Add(new SecVestStatusChannel());

			_httpClient = new HttpClient();
	    }

	    public async Task<IEnumerable<IChannel>> GetChannelsAsync()
	    {
		    return _channels;
	    }

	    public Task Teardown()
	    {
		    throw new NotImplementedException();
	    }
    }
}
