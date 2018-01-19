using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using Microsoft.Extensions.Logging;

namespace IoTHs.Core
{
	public class PluginRegistry : IPluginRegistry
	{
		private Dictionary<string, Type> _deviceTypes = new Dictionary<string, Type>();
		private Dictionary<string, IPlugin> _deviceList = new Dictionary<string, IPlugin>();
	    private readonly ILogger _log;

	    public PluginRegistry(ILoggerFactory loggerFactory)
	    {
	        _log = loggerFactory.CreateLogger<PluginRegistry>();
	    }

        public void RegisterPluginType<T>() where T : class, IPlugin
		{
			_deviceTypes.Add(typeof(T).Name.ToLower(), typeof(T));
		}

		public async Task InitializePluginsAsync(AppConfigurationModel configurationObject)
		{
			foreach (var configuration in configurationObject.DevicePluginConfigurations)
			{
				try
				{
					var deviceInstance = (IPlugin)ServiceLocator.Current.GetService(_deviceTypes[configuration.Type.ToLower()]);
					_deviceList.Add(configuration.Name.ToLower(), deviceInstance);
					await deviceInstance.InitializeAsync(configuration);
				}
				catch (Exception ex)
				{
					_log.LogError(ex, "Error while initializing plugin " + configuration.Name);
				}
			}
		}

		public async Task TeardownPluginsAsync()
		{
            _log.LogTrace("Shutdown devices");
			foreach (var device in _deviceList.ToList())
			{
				await device.Value.TeardownAsync();
			    _deviceList.Remove(device.Key);
			}
		}

	    public IEnumerable<IPlugin> GetPlugins()
	    {
	        return _deviceList.Values.AsEnumerable();
	    }

		public IEnumerable<T> GetPlugins<T>() where T : class, IPlugin
		{
			return _deviceList.Select(d => d.Value).Where(d => (d as T) != null).Select(d=>d as T);
		}

		public T GetPlugin<T>() where T : class, IPlugin
		{
			return _deviceList.Select(d => d.Value).Where(d => (d as T) != null).Select(d => d as T).Single();
		}

		public T GetPlugin<T>(string name) where T : class, IPlugin
		{
			return (T)_deviceList[name.ToLower()];
		}

		public object GetPlugin(string name)
		{
			return _deviceList[name.ToLower()];
		}
	}
}
