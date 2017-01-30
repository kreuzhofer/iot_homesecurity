using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Channels;

namespace W10Home.Core.Interfaces
{
	public interface IChannel
	{
		string Name { get; }
		bool IsRead { get; }
		bool IsWrite { get; }
		ChannelType ChannelType { get; }
	}
}
