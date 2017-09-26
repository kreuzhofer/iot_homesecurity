using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using IoTHs.Plugin.ABUS.SecVest.Utils;
using W10Home.Core.Standard;

namespace IoTHs.Plugin.ABUS.SecVest
{
    public class SecVestDevice : DeviceBase
    {
		private HttpClient _httpClient;
		private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
        private string _name;
        private string _type;

        public SecVestDevice()
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

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            _name = configuration.Name;
            _type = configuration.Type;
			var connectionString = configuration.Properties["ConnectionString"];
			var username = configuration.Properties["Username"];
			var password = configuration.Properties["Password"];

		    var filter = new HttpBaseProtocolFilter();
			filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
			filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

			// create default HttpClient used by all channels
			_httpClient = new HttpClient(filter);
			_httpClient.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Basic", Base64.EncodeTo64(username+":"+password));

			_channels.Add(new SecVestStatusChannel(_httpClient, connectionString));
		}

	    public override IEnumerable<IDeviceChannel> GetChannels()
	    {
		    return _channels;
	    }

	    public override async Task TeardownAsync()
	    {
	    }
    }
}
