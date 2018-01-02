using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using W10Home.NetCoreDevicePortal.DataAccess;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;
using W10Home.NetCoreDevicePortal.DataAccess.Services;
using W10Home.NetCoreDevicePortal.Security;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [Route("api/DeviceConfiguration")]
    public class DeviceConfigurationApiController : Controller
    {
        private readonly IDeviceFunctionService _deviceFunctionService;
        private DevicePluginService _devicePluginService;
        private DevicePluginPropertyService _devicePluginPropertyService;
        private IConfiguration _configuration;
        private IDeviceConfigurationService _deviceConfigurationService;

        public DeviceConfigurationApiController(IDeviceFunctionService deviceFunctionService, DevicePluginService devicePluginService, 
            DevicePluginPropertyService devicePluginPropertyService, IConfiguration configuration,
            IDeviceConfigurationService deviceConfigurationService)
        {
            _deviceFunctionService = deviceFunctionService;
            _devicePluginService = devicePluginService;
            _devicePluginPropertyService = devicePluginPropertyService;
            _configuration = configuration;
            _deviceConfigurationService = deviceConfigurationService;
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
                        Type = "IAzureIoTHubDevicePlugin",
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

        [HttpPost("{deviceId}/{configurationKey}")]
        public async Task<IActionResult> Post(string deviceId, string configurationKey, [FromBody]dynamic body)
        {
            string value = body.configurationValue;
            await _deviceConfigurationService.SaveConfigAsync(deviceId, configurationKey, value);
            return Accepted();
        }

        [HttpPost("{deviceId}/{devicePluginId}/{devicePluginConfigurationKey}")]
        public async Task<IActionResult> Post(string deviceId, string devicePluginId, string devicePluginConfigurationKey)
        {
            string value = await(new StreamReader(this.Request.Body)).ReadToEndAsync();
            await _devicePluginPropertyService.SavePropertyAsync(devicePluginId, devicePluginConfigurationKey, value);
            return Accepted();
        }
    }
}
