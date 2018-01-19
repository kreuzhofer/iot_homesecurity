using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Core
{
	public abstract class PluginBase : IPlugin
	{
	    private string _name;
	    private string _type;
	    protected List<IDevice> _devices = new List<IDevice>();

	    public string Name => _name;
        public string Type => _type;

	    public virtual async Task InitializeAsync(DevicePluginConfigurationModel configuration)
	    {
	        _name = configuration.Name;
	        _type = configuration.Type;
	    }

	    public IEnumerable<IDevice> Devices => _devices;

	    public virtual async Task TeardownAsync()
	    {
	        foreach (var device in _devices)
	        {
	            await device.TearDownAsync();
	        }
            _devices.Clear();
	        _devices = null;
	    }
	}
}