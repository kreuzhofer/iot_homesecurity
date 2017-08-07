using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IoTHs.Devices.Interfaces
{
	public interface IDeviceChannel
	{
		string Name { get; }
		bool IsRead { get; }
		bool IsWrite { get; }
	    [JsonConverter(typeof(StringEnumConverter))]
        ChannelType ChannelType { get; }
		object Read();
		void Write(object value);
	}
}
