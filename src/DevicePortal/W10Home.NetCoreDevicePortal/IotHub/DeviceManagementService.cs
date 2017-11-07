using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;

namespace W10Home.DevicePortal.IotHub
{

    public class DeviceManagementService
    {
        public DeviceManagementService(IConfiguration configuration, IDeviceFunctionService deviceFunctionService)
        {
            this._configuration = configuration;
            _deviceFunctionService = deviceFunctionService;
        }

        static RegistryManager _globalRegistryManager;
		private static ServiceClient _client;
        private IConfiguration _configuration;
        private IDeviceFunctionService _deviceFunctionService;

        public RegistryManager GlobalRegistryManager
        {
            get
            {
                if (_globalRegistryManager == null)
                {
                    var config = _configuration.GetSection("ConnectionStrings")["IotHub"];
                    _globalRegistryManager = RegistryManager.CreateFromConnectionString(config);
                }
	            return _globalRegistryManager;
            }
        }

	    public ServiceClient ServiceClient
	    {
		    get
		    {
				if (_client == null)
				{
				    var config = _configuration.GetSection("ConnectionStrings")["IotHub"];
                    _client = ServiceClient.CreateFromConnectionString(config);
				}
			    return _client;
		    }
		}

        public async Task UpdateFunctionsAndVersionsTwinPropertyAsync(string deviceId)
        {
            var functions = await _deviceFunctionService.GetFunctionsAsync(deviceId);
            string functionsAndVersions = "";
            foreach (var function in functions)
            {
                functionsAndVersions += function.RowKey + ":" + function.Version + ",";
            }
            functionsAndVersions = functionsAndVersions.TrimEnd(',');

            // update device twin
            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        functions = new
                        {
                            versions = functionsAndVersions,
                            baseUrl = _configuration["ExternalBaseUrl"]
                        }
                    }
                }
            };
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                CheckAdditionalContent = false
            };
            var twin = await _globalRegistryManager.GetTwinAsync(deviceId);
            await _globalRegistryManager.UpdateTwinAsync(deviceId, JsonConvert.SerializeObject(patch),
                twin.ETag);
        }
    }
}