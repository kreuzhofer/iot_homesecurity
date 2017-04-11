using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;

namespace W10Home.Core.Configuration
{
	public class RootConfiguration
	{
		public List<DeviceConfiguration> DeviceConfigurations { get; set; }
		public List<FunctionDeclaration> Functions { get; set; }
	}
}
