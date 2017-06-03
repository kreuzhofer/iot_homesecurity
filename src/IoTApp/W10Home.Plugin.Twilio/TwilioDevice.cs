using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Configuration;
using W10Home.Core.Standard;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;

namespace W10Home.Plugin.Twilio
{
	public class TwilioDevice : DeviceBase
	{
		private string _accountSid;
		private string _authToken;
		private List<IDeviceChannel> _channels = new List<IDeviceChannel>();

		public override IEnumerable<IDeviceChannel> GetChannels()
		{
			return _channels.AsEnumerable();
		}

		public override async Task InitializeAsync(IDeviceConfiguration configuration)
		{
			_accountSid = configuration.Properties["AccountSid"];
			_authToken = configuration.Properties["AuthToken"];
			_channels.Add(new TwilioSmsChannel(_accountSid, _authToken, configuration.Properties["OutgoingPhone"], configuration.Properties["ReceiverPhone"]));
		}

		public override async Task Teardown()
		{
		}
	}
}
