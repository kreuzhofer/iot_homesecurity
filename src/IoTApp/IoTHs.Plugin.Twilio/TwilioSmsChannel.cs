using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHs.Core.Channels;
using IoTHs.Devices.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace IoTHs.Plugin.Twilio
{
	public class TwilioSmsChannel : NotificationChannel
	{
		private string _accountSid;
		private string _authToken;
		private string _outgoingPhoneNumber;
		private string _receiverPhoneNumber;

		internal TwilioSmsChannel(string accountSid, string authToken, string outgoingPhoneNumber, string receiverPhoneNumber)
		{
			_accountSid = accountSid;
			_authToken = authToken;
			_outgoingPhoneNumber = outgoingPhoneNumber;
			_receiverPhoneNumber = receiverPhoneNumber;
		}

		public override bool IsRead => false;

		public override bool IsWrite => false;

		public override string Name => "SMS";

		public override async Task<bool> NotifyAsync(string messageBody)
		{
			TwilioClient.Init(_accountSid, _authToken);

			var message = await MessageResource.CreateAsync(
				to: new PhoneNumber(_receiverPhoneNumber),
				@from: new PhoneNumber(_outgoingPhoneNumber),
				body: messageBody);

			return true;
		}

		public override object Read()
		{
			throw new NotImplementedException();
		}

		public override void Write(object value)
		{
			throw new NotImplementedException();
		}

	    public override IEnumerable<IChannelDatapoint> Datapoints { get; }
	}
}
