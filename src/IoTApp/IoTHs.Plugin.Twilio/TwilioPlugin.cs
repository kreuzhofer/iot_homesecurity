using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.Twilio
{
	public class TwilioPlugin : PluginBase
	{
		private string _accountSid;
		private string _authToken;

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            await base.InitializeAsync(configuration);

			_accountSid = configuration.Properties["AccountSid"];
			_authToken = configuration.Properties["AuthToken"];
			_devices.Add(new TwilioSmsDevice(_accountSid, _authToken, configuration.Properties["OutgoingPhone"], configuration.Properties["ReceiverPhone"]));
		}
	}
}
