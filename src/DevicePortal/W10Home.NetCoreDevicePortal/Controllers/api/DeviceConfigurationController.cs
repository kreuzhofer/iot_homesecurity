using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using W10Home.NetCoreDevicePortal.DataAccess;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [Produces("application/json")]
    [Route("api/DeviceConfiguration")]
    public class DeviceConfigurationController : Controller
    {
        private readonly IDeviceFunctionService _deviceFunctionService;

        public DeviceConfigurationController(IDeviceFunctionService deviceFunctionService)
        {
            _deviceFunctionService = deviceFunctionService;
        }

        // GET: api/DeviceConfiguration/{deviceId}
        [HttpGet("{deviceId}")]
        public async Task<DeviceConfigurationModel> Get(string deviceId)
        {
            var deviceFunctions = await _deviceFunctionService.GetFunctionsAsync(deviceId);

            var result = new DeviceConfigurationModel
            {
                DeviceId = deviceId,
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
            return result;
        }
    }
}
