using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Configuration;
using W10Home.Core.Interfaces;

namespace W10Home.Plugin.Twilio
{
	public class TwilioDevice : IDevice
	{
		private string _accountSid;
		private string _authToken;
		private List<IChannel> _channels = new List<IChannel>();

		public Task<IEnumerable<IChannel>> GetChannelsAsync()
		{
			return Task.FromResult(_channels.AsEnumerable());
		}

		public async Task InitializeAsync(DeviceConfiguration configuration)
		{
			_accountSid = configuration.Properties["AccountSid"];
			_authToken = configuration.Properties["AuthToken"];
			_channels.Add(new TwilioSmsChannel(_accountSid, _authToken, configuration.Properties["OutgoingPhone"], configuration.Properties["ReceiverPhone"]));
		}
	}
}
