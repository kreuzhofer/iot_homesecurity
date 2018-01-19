using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Http;
using IoTHs.Devices.Interfaces;
using IoTHs.Plugin.ABUS.Utils;

namespace IoTHs.Plugin.ABUS.SecVest
{
    public class SecVestPlugin : PluginBase
    {
		private LocalHttpClient _httpClient;

        public SecVestPlugin()
	    {
		    Debug.WriteLine("SecVestDevice Instance created.");
	    }

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            await base.InitializeAsync(configuration);

			var connectionString = configuration.Properties["ConnectionString"];
			var username = configuration.Properties["Username"];
			var password = configuration.Properties["Password"];

			// create default HttpClient used by all channels
			_httpClient = new LocalHttpClient();
            _httpClient.Client.DefaultRequestHeaders.Add("Authorization", "Basic "+Base64.EncodeTo64(username+":"+password));

            _devices.Add(new SecvestDevice(_httpClient.Client, connectionString));
		}
    }
}
