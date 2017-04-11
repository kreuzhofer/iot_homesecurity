using Windows.Web.Http;
using W10Home.Interfaces;

namespace W10Home.Plugin.ABUS.SecVest
{
	public abstract class SecVestChannel : IDeviceChannel
	{
		protected HttpClient _client;
		protected string _baseUrl;

		protected SecVestChannel(HttpClient client, string baseUrl)
		{
			_client = client;
			_baseUrl = baseUrl;
		}

		public abstract string Name { get; }
		public abstract bool IsRead { get; }
		public abstract bool IsWrite { get; }
		public abstract ChannelType ChannelType { get; }
		public abstract object Read();
		public abstract void Write(object value);
	}
}