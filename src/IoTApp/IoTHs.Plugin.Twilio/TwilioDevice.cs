using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.Twilio
{
	public class TwilioDevice : DeviceBase
	{
		private string _accountSid;
		private string _authToken;
		private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
	    private string _name;
	    private string _type;

	    public override IEnumerable<IDeviceChannel> GetChannels()
		{
			return _channels.AsEnumerable();
		}

	    public override string Name
	    {
	        get { return _name; }
	    }

	    public override string Type
	    {
	        get { return _type; }
	    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
	        _name = configuration.Name;
	        _type = configuration.Type;
			_accountSid = configuration.Properties["AccountSid"];
			_authToken = configuration.Properties["AuthToken"];
			_channels.Add(new TwilioSmsChannel(_accountSid, _authToken, configuration.Properties["OutgoingPhone"], configuration.Properties["ReceiverPhone"]));
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task TeardownAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
		}
	}
}
