using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using W10Home.NetCoreDevicePortal.DataAccess;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;
using W10Home.NetCoreDevicePortal.DataAccess.Services;
using W10Home.NetCoreDevicePortal.Security;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [ApiKeyAuthentication()]
    [Produces("application/json")]
    [Route("api/DeviceConfiguration")]
    public class DeviceConfigurationApiController : Controller
    {
        private readonly IDeviceFunctionService _deviceFunctionService;
        private DevicePluginService _devicePluginService;
        private DevicePluginPropertyService _devicePluginPropertyService;
        private IConfiguration _configuration;

        public DeviceConfigurationApiController(IDeviceFunctionService deviceFunctionService, DevicePluginService devicePluginService, DevicePluginPropertyService devicePluginPropertyService,
            IConfiguration configuration)
        {
            _deviceFunctionService = deviceFunctionService;
            _devicePluginService = devicePluginService;
            _devicePluginPropertyService = devicePluginPropertyService;
            _configuration = configuration;
        }

        // GET: api/DeviceConfiguration/{deviceId}
        [HttpGet("{deviceId}")]
        public async Task<DeviceConfigurationModel> Get(string deviceId)
        {
            var deviceFunctions = await _deviceFunctionService.GetFunctionsAsync(deviceId);

            var result = new DeviceConfigurationModel
            {
                DeviceId = deviceId,
                ServiceBaseUrl = _configuration["ExternalBaseUrl"],
                DevicePluginConfigurations = new List<DevicePluginConfigurationModel>
                {
                    new DevicePluginConfigurationModel // default iot hub configuration for tpm
                    {
                        Name = "iothub",
                        Type = "AzureIoTHubDevice",
                        Properties = new Dictionary<string, string>()
                    }
                },
                DeviceFunctionIds = deviceFunctions.Select(a=>a.RowKey).ToList()
            };

            // add device plugin configurations from database
            var devicePlugins = await _devicePluginService.GetAsync(deviceId);
            foreach (var devicePlugin in devicePlugins.Where(p=>p.Enabled)) // only the enabled plugins are reported to the device
            {
                var props = await _devicePluginPropertyService.GetAsync(devicePlugin.RowKey);
                result.DevicePluginConfigurations.Add(new DevicePluginConfigurationModel
                {
                    Name = devicePlugin.RowKey,
                    Type = devicePlugin.Type,
                    Properties = props.ToDictionary(k => k.RowKey, k=>k.Value)
                });
            }
            return result;
        }
    }
}
