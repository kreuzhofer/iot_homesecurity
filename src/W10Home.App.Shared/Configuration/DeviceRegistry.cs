using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;

namespace W10Home.Core.Configuration
{
	internal class DeviceRegistry : IDeviceRegistry
	{
		private Dictionary<string, Type> _deviceTypes = new Dictionary<string, Type>();
		private Dictionary<string, IDevice> _deviceList = new Dictionary<string, IDevice>();

		public void RegisterDeviceType<T>() where T : class, IDevice
		{
			_deviceTypes.Add(typeof(T).Name, typeof(T));
		}

		public async Task InitializeDevicesAsync(RootConfiguration configurationObject)
		{
			foreach (var configuration in configurationObject.DeviceConfigurations)
			{
				try
				{
					var deviceInstance = (IDevice)ServiceLocator.Current.GetInstance(_deviceTypes[configuration.Type]);
					_deviceList.Add(configuration.Name, deviceInstance);
					await deviceInstance.InitializeAsync(configuration);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Error while initializing plugin " + configuration.Name + ": " + ex.Message);
					// TODO log
				}
			}
		}

		public async Task TeardownDevicesAsync()
		{
			foreach (var device in _deviceList)
			{
				await device.Value.Teardown();
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

		public T GetDevice<T>(string name) where T : class, IDevice
		{
			return (T)_deviceList[name];
		}

		public object GetDevice(string name)
		{
			return _deviceList[name];
		}
	}
}
