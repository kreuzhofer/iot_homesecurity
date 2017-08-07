using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [Produces("application/json")]
    [Route("api/DeviceConfiguration")]
    public class DeviceConfigurationController : Controller
    {
        // GET: api/DeviceConfiguration/{deviceId}
        [HttpGet("{deviceId}")]
        public IActionResult Get(string deviceId)
        {
            return "value";
        }
    }
}
