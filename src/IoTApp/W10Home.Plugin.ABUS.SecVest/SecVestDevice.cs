using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Core;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
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

	    public override async Task Teardown()
	    {
	    }
    }
}
