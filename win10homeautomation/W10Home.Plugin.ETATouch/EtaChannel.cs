using System;
using System.Threading.Tasks;
using W10Home.Core.Interfaces;

namespace W10Home.Plugin.ETATouch
{

	public class EtaChannel : IChannel
	{
		private string _name;

		public EtaChannel(string name)
		{
			_name = name;
		}

		public bool IsRead => true;

		public bool IsWrite => false;

		public string Name => _name;

		public Task<bool> SendMessageAsync(string messageBody)
		{
			throw new NotImplementedException();
		}
	}
}
