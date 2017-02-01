using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Interfaces;

namespace W10Home.Core.Configuration
{
	public class DeviceRegistry
	{
		private Dictionary<string, IDevice> _deviceList = new Dictionary<string, IDevice>();

		public void RegisterDevice(string name, IDevice device)
		{
			_deviceList.Add(name, device);
		}

		public async Task InitializeDevicesAsync(RootConfiguration configurationObject)
		{
			foreach (var configuration in configurationObject.DeviceConfigurations)
			{
				try
				{
					var device = _deviceList[configuration.Name];
					await device.InitializeAsync(configuration);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Error while initializing plugin " + configuration.Name + ": " + ex.Message);
					// TODO log
				}
			}
		}

		public IEnumerable<T> GetDevices<T>() where T : class, IDevice
		{
			return _deviceList.Select(d => d.Value).Where(d => (d as T) != null).Select(d=>d as T);
		}

		public T GetDevice<T>() where T : class, IDevice
		{
			return _deviceList.Select(d => d.Value).Where(d => (d as T) != null).Select(d => d as T).Single();
		}
	}
}
