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
        [HttpGet("{deviceId}", Name = "Get")]
        public async Task<List<DeviceFunctionEntity>> Get(string deviceId)
        {
            var result = await _deviceFunctionService.GetFunctionsAsync(deviceId);
            return result;
        }
        
        // POST: api/Script
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }
        
        // PUT: api/Script/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
