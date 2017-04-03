using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;
using W10Home.Plugin.ABUS.SecVest.Utils;

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

			// create default HttpClient used by all channels
			_httpClient = new HttpClient();
			System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Base64.EncodeTo64(username+":"+password));

			_channels.Add(new SecVestStatusChannel(_httpClient));
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
