using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using W10Home.NetCoreDevicePortal.Security;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [ApiKeyAuthentication()]
    [Produces("application/json")]
    [Route("api/Logging")]
    public class LoggingApiController : Controller
    {
        [HttpPost("{deviceId}")]
        public IActionResult Post(string deviceId, [FromBody] LogMessage logMessage)
        {
            Debug.WriteLine(logMessage.Message);
            return Json(logMessage);
        }
    }
}