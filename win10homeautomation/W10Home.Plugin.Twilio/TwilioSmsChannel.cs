using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using W10Home.Core.Channels;
using W10Home.Core.Interfaces;

namespace W10Home.Plugin.Twilio
{
	public class TwilioSmsChannel : NotificationChannel
	{
		private string _accountSid;
		private string _authToken;
		private string _outgoingPhoneNumber;
		private string _receiverPhoneNumber;

		internal TwilioSmsChannel(string accountSid, string authToken, TwilioSmsChannelConfiguration channelConfiguration)
		{
			_accountSid = accountSid;
			_authToken = authToken;
			_outgoingPhoneNumber = channelConfiguration.OutgoingPhoneNumber;
			_receiverPhoneNumber = channelConfiguration.ReceiverPhoneNumber;
		}

		public override bool IsRead => false;

		public override bool IsWrite => false;

		public override string Name => "SMS";

		public override async Task<bool> NotifyAsync(string messageBody)
		{
			TwilioClient.Init(_accountSid, _authToken);

			var message = await MessageResource.CreateAsync(
				to: new PhoneNumber(_receiverPhoneNumber),
				from: new PhoneNumber(_outgoingPhoneNumber),
				body: messageBody);

			return true;
		}
	}
}
