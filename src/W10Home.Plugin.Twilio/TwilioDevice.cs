using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Interfaces;

namespace W10Home.Plugin.Twilio
{
	public class TwilioDevice : IDevice
	{
		private string _accountSid;
		private string _authToken;
		private List<IChannel> _channels;

		public TwilioDevice(string accountSid, string authToken, IEnumerable<TwilioSmsChannelConfiguration> channelConfigurations)
		{
			_accountSid = accountSid;
			_authToken = authToken;
			_channels = new List<IChannel>();
			foreach (var channelConfig in channelConfigurations)
			{
				_channels.Add(new TwilioSmsChannel(_accountSid, _authToken, channelConfig));
			}
		}

		public Task<IEnumerable<IChannel>> GetChannelsAsync()
		{
			return Task.FromResult(_channels.AsEnumerable());
		}

		public Task InitializeAsync()
		{
			throw new NotImplementedException();
		}
	}
}
