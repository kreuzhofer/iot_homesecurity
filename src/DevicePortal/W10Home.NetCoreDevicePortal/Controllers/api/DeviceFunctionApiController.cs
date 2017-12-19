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
using W10Home.DevicePortal.IotHub;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;
using W10Home.NetCoreDevicePortal.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [Route("api/DeviceFunction")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class DeviceFunctionApiController : Controller
    {
        private IDeviceFunctionService _deviceFunctionService;
        private DeviceManagementService _deviceManagementService;

        public DeviceFunctionApiController(IDeviceFunctionService deviceFunctionService, DeviceManagementService deviceManagementService)
        {
            _deviceFunctionService = deviceFunctionService;
            _deviceManagementService = deviceManagementService;
        }

        // GET: api/DeviceFunction/DeviceId
        [HttpGet("{deviceId}")]
        public async Task<List<DeviceFunctionModel>> Get(string deviceId)
        {
            var result = await _deviceFunctionService.GetFunctionsAsync(deviceId);
            return result.Select(a => a.ToDeviceFunctionModel()).ToList();
        }

        // GET: api/DeviceFunction/DeviceId/FunctionId
        [HttpGet("{deviceId}/{functionId}")]
        public async Task<DeviceFunctionModel> GetSingle(string deviceId, string functionId)
        {
            var result = await _deviceFunctionService.GetFunctionAsync(deviceId, functionId);
            return result.ToDeviceFunctionModel();
        }

        [HttpPost("{deviceId}/{functionId}")]
        public async Task<IActionResult> CreateNew(string deviceId, string functionId, [FromBody]DeviceFunctionModel functionModel)
        {
            await _deviceFunctionService.SaveFunctionAsync(deviceId, functionId, functionModel.Name, functionModel.TriggerType.ToString(), functionModel.Interval, functionModel.QueueName, functionModel.Enabled, functionModel.Script);
            await _deviceManagementService.UpdateFunctionsAndVersionsTwinPropertyAsync(deviceId);
            return Ok();
        }

        [HttpDelete("{deviceId}/{functionId}")]
        public async Task<IActionResult> Delete(string deviceId, string functionId)
        {
            await _deviceFunctionService.DeleteFunctionAsync(deviceId, functionId);
            await _deviceManagementService.UpdateFunctionsAndVersionsTwinPropertyAsync(deviceId);
            return Ok();
        }
    }
}
