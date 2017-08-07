using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTHs.Devices.Interfaces
{
	public interface IDeviceRegistry
	{
		void RegisterDeviceType<T>() where T : class, IDevice;

		IEnumerable<T> GetDevices<T>() where T : class, IDevice;
		T GetDevice<T>() where T : class, IDevice;
		T GetDevice<T>(string name) where T : class, IDevice;
		Task TeardownDevicesAsync();
		object GetDevice(string name);
	    IEnumerable<IDevice> GetDevices();
	}
}
