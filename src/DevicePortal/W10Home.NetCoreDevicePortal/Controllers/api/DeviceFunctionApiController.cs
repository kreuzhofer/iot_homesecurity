using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using W10Home.NetCoreDevicePortal.DataAccess;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.Models;
using IoTHs.Api.Shared;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [Produces("application/json")]
    [Route("api/DeviceFunction")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class DeviceFunctionApiController : Controller
    {
        private IDeviceFunctionService _deviceFunctionService;

        public DeviceFunctionApiController(IDeviceFunctionService deviceFunctionService)
        {
            _deviceFunctionService = deviceFunctionService;
        }

        // GET: api/DeviceFunction/DeviceId
        [HttpGet("{deviceId}")]
        public async Task<List<DeviceFunctionModel>> Get(string deviceId)
        {
            var result = await _deviceFunctionService.GetFunctionsAsync(deviceId);
            return result.Select(a=>a.ToDeviceFunctionModel()).ToList();
        }

        // GET: api/DeviceFunction/DeviceId/FunctionId
        [HttpGet("{deviceId}/{functionId}")]
        public async Task<DeviceFunctionModel> GetSingle(string deviceId, string functionId)
        {
            var result = await _deviceFunctionService.GetFunctionAsync(deviceId, functionId);
            return result.ToDeviceFunctionModel();
        }
    }
}
