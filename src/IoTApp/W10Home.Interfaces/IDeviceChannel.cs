using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W10Home.Interfaces
{
	public interface IDeviceChannel
	{
		string Name { get; }
		bool IsRead { get; }
		bool IsWrite { get; }
		ChannelType ChannelType { get; }
		object Read();
		void Write(object value);
	}
}
