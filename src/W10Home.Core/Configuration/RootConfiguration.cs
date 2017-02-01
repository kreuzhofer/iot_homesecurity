using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Interfaces;

namespace W10Home.Core.Configuration
{
	public class RootConfiguration
	{
		public List<DeviceConfiguration> DeviceConfigurations { get; set; }
	}
}
