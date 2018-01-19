using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHs.Api.Shared;

namespace IoTHs.Devices.Interfaces
{
	public interface IPluginRegistry
	{
		void RegisterPluginType<T>() where T : class, IPlugin;

		IEnumerable<T> GetPlugins<T>() where T : class, IPlugin;
		T GetPlugin<T>() where T : class, IPlugin;
		T GetPlugin<T>(string name) where T : class, IPlugin;
		Task TeardownPluginsAsync();
		object GetPlugin(string name);
	    IEnumerable<IPlugin> GetPlugins();
	    Task InitializePluginsAsync(AppConfigurationModel configurationObject);
	}
}
