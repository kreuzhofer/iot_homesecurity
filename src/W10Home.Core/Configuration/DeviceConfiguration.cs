using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;

namespace W10Home.Core.Configuration
{
	public class DeviceConfiguration : IDeviceConfiguration
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public Dictionary<string,string> Properties { get; set; }
	}
}
