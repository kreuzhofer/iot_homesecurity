using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using Microsoft.Practices.ServiceLocation;
using NLog;

namespace IoTHs.Core
{
	internal class DeviceRegistry : IDeviceRegistry
	{
		private Dictionary<string, Type> _deviceTypes = new Dictionary<string, Type>();
		private Dictionary<string, IDevice> _deviceList = new Dictionary<string, IDevice>();
	    private readonly ILogger _log = LogManager.GetCurrentClassLogger();

        public void RegisterDeviceType<T>() where T : class, IDevice
		{
			_deviceTypes.Add(typeof(T).Name, typeof(T));
		}

		public async Task InitializeDevicesAsync(DeviceConfigurationModel configurationObject)
		{
			foreach (var configuration in configurationObject.DevicePluginConfigurations)
			{
				try
				{
					var deviceInstance = (IDevice)ServiceLocator.Current.GetInstance(_deviceTypes[configuration.Type]);
					_deviceList.Add(configuration.Name, deviceInstance);
					await deviceInstance.InitializeAsync(configuration);
				}
				catch (Exception ex)
				{
					_log.Error(ex, "Error while initializing plugin " + configuration.Name);
				}
			}
		}

		public async Task TeardownDevicesAsync()
		{
            _log.Trace("Shutdown devices");
			foreach (var device in _deviceList.ToList())
			{
				await device.Value.TeardownAsync();
			    _deviceList.Remove(device.Key);
			}
		}

	    public IEnumerable<IDevice> GetDevices()
	    {
	        return _deviceList.Values.AsEnumerable();
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
