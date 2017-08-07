using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using W10Home.NetCoreDevicePortal.DataAccess;
using W10Home.NetCoreDevicePortal.Models;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [Produces("application/json")]
    [Route("api/DeviceFunction")]
    public class DeviceFunctionController : Controller
    {
        private IDeviceFunctionService _deviceFunctionService;

        public DeviceFunctionController(IDeviceFunctionService deviceFunctionService)
        {
            _deviceFunctionService = deviceFunctionService;
        }

        // GET: api/DeviceFunction/DeviceId
        [HttpGet("{deviceId}")]
        public async Task<List<DeviceFunctionEntity>> Get(string deviceId)
        {
            var result = await _deviceFunctionService.GetFunctionsAsync(deviceId);
            return result;
        }

        // GET: api/DeviceFunction/DeviceId/FunctionId
        [HttpGet("{deviceId}/{functionId}")]
        public async Task<DeviceFunctionEntity> GetSingle(string deviceId, string functionId)
        {
            var result = await _deviceFunctionService.GetFunctionAsync(deviceId, functionId);
            return result;
        }
    }
}
