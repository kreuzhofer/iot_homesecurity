using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W10Home.Plugin.Twilio
{
	public class TwilioSmsChannelConfiguration
	{
		public TwilioSmsChannelConfiguration(string outgoingPhoneNumber, string receiverPhoneNumber)
		{
			OutgoingPhoneNumber = outgoingPhoneNumber;
			ReceiverPhoneNumber = receiverPhoneNumber;
		}

		public string OutgoingPhoneNumber { get; private set; }
		public string ReceiverPhoneNumber { get; private set; }
	}
}
