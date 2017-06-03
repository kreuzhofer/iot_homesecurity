using System.Collections.Generic;

namespace W10Home.Interfaces.Configuration
{
	public interface IDeviceConfiguration
	{
		string Name { get; set; }
		string Type { get; set; }
		Dictionary<string, string> Properties { get; set; }
	}
}
