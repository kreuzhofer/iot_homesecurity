using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHs.Api.Shared;

namespace IoTHs.Devices.Interfaces
{
	public interface IDeviceRegistry
	{
		void RegisterDeviceType<T>() where T : class, IDevicePlugin;

		IEnumerable<T> GetDevices<T>() where T : class, IDevicePlugin;
		T GetDevice<T>() where T : class, IDevicePlugin;
		T GetDevice<T>(string name) where T : class, IDevicePlugin;
		Task TeardownDevicesAsync();
		object GetDevice(string name);
	    IEnumerable<IDevicePlugin> GetDevices();
	    Task InitializeDevicesAsync(DeviceConfigurationModel configurationObject);
	}
}
