using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace W10Home.Interfaces
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
