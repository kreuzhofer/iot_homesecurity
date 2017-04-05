using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Standard;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;
using W10Home.Plugin.ABUS.SecVest.Utils;

namespace W10Home.Plugin.ABUS.SecVest
{
    public class SecVestDevice : DeviceBase
    {
		private HttpClient _httpClient;
		private List<IDeviceChannel> _channels = new List<IDeviceChannel>();

	    public SecVestDevice()
	    {
		    Debug.WriteLine("SecVestDevice Instance created.");
	    }

		public override async Task InitializeAsync(IDeviceConfiguration configuration)
	    {
			var connectionString = configuration.Properties["ConnectionString"];
			var username = configuration.Properties["Username"];
			var password = configuration.Properties["Password"];

			// create default HttpClient used by all channels
			_httpClient = new HttpClient();
		    _httpClient.BaseAddress = new Uri(connectionString);
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Base64.EncodeTo64(username+":"+password));

			_channels.Add(new SecVestStatusChannel(_httpClient));
		}

		public override IEnumerable<IDeviceChannel> GetChannels()
	    {
		    return _channels;
	    }

	    public override async Task Teardown()
	    {
	    }
    }
}
