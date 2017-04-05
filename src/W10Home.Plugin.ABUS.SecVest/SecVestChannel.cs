using System.Net.Http;
using W10Home.Interfaces;

namespace W10Home.Plugin.ABUS.SecVest
{
	public abstract class SecVestChannel : IDeviceChannel
	{
		protected HttpClient _client;

		protected SecVestChannel(HttpClient client)
		{
			_client = client;
		}

		public abstract string Name { get; }
		public abstract bool IsRead { get; }
		public abstract bool IsWrite { get; }
		public abstract ChannelType ChannelType { get; }
		public abstract object Read();
		public abstract void Write(object value);
	}
}