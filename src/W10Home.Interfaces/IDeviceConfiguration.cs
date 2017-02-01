using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W10Home.Interfaces
{
	public interface IDeviceConfiguration
	{
		string Name { get; set; }
		string Type { get; set; }
		Dictionary<string, string> Properties { get; set; }
	}
}
