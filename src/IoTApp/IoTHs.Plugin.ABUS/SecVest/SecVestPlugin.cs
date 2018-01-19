using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Http;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.ABUS.SecVest
{
    public class SecVestPlugin : PluginBase
    {
		private LocalHttpClient _httpClient;
		private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
        private string _name;
        private string _type;

        public SecVestPlugin()
	    {
		    Debug.WriteLine("SecVestDevice Instance created.");
	    }

        public override string Name
        {
            get { return _name; }
        }

        public override string Type
        {
            get { return _type; }
        }

#pragma warning disable 1998
        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
#pragma warning restore 1998
        {
            _name = configuration.Name;
            _type = configuration.Type;
			var connectionString = configuration.Properties["ConnectionString"];
			var username = configuration.Properties["Username"];
			var password = configuration.Properties["Password"];

			// create default HttpClient used by all channels
			_httpClient = new LocalHttpClient();

			_channels.Add(new SecVestStatusChannel(_httpClient.Client, connectionString));
		}

	    public override IEnumerable<IDeviceChannel> GetChannels()
	    {
		    return _channels;
	    }

#pragma warning disable 1998
	    public override async Task TeardownAsync()
#pragma warning restore 1998
	    {
	    }
    }
}
