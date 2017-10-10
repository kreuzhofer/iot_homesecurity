using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using W10Home.NetCoreDevicePortal.Security;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [ApiKeyAuthentication("550a4f21-18e0-4467-8d72-1060825f98d0")]
    [Produces("application/json")]
    [Route("api/Logging")]
    public class LoggingApiController : Controller
    {
        [HttpPost]
        public IActionResult Post([FromBody] LogEvent logEvent /*deviceId/*, [FromBody] string message, [FromBody] string severity*/)
        {
            return Json(logEvent);
        }
    }

    public class LogEvent
    {
        public string DeviceId { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
    }
}