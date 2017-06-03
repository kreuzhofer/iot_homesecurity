using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;

namespace W10Home.Core.Configuration
{
	public class DeviceConfiguration : IDeviceConfiguration
	{
		private Dictionary<string, string> _properties = new Dictionary<string, string>();

		public string Name { get; set; }
		public string Type { get; set; }
		public Dictionary<string,string> Properties
		{
			get
			{
				return _properties;
			}
			set
			{
				_properties = value;
			}
		}
	}
}
