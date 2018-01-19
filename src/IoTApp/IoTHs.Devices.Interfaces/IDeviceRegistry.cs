using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHs.Api.Shared;

namespace IoTHs.Devices.Interfaces
{
	public interface IDeviceRegistry
	{
		void RegisterDeviceType<T>() where T : class, IPlugin;

		IEnumerable<T> GetDevices<T>() where T : class, IPlugin;
		T GetDevice<T>() where T : class, IPlugin;
		T GetDevice<T>(string name) where T : class, IPlugin;
		Task TeardownDevicesAsync();
		object GetDevice(string name);
	    IEnumerable<IPlugin> GetDevices();
	    Task InitializeDevicesAsync(DeviceConfigurationModel configurationObject);
	}
}
