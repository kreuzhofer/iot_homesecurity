using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;

namespace W10Home.Plugin.ABUS.SecVest
{
	public class SecVestStatusChannel : IChannel
	{
		public string Name => "status";

		public bool IsRead => true;

		public bool IsWrite => false;

		public ChannelType ChannelType => ChannelType.None;

		public async Task<SecVestStatus> GetStatusAsync()
		{
			return new SecVestStatus();
		}
	}
}
