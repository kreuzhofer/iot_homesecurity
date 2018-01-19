using IoTHs.Api.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTHs.Devices.Interfaces
{
	public interface IPlugin
	{
        string Name { get; }
        string Type { get; }
		Task InitializeAsync(DevicePluginConfigurationModel configuration);
	    IEnumerable<IDevice> Devices { get; }
        Task TeardownAsync();
	}
}
