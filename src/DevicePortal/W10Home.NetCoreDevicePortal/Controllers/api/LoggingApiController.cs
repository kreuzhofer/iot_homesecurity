using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using W10Home.NetCoreDevicePortal.Security;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [ApiKeyAuthentication()]
    [Produces("application/json")]
    [Route("api/Logging")]
    public class LoggingApiController : Controller
    {
        private static CloudQueueClient _queueClient;

        public LoggingApiController(IConfiguration configuration)
        {
            if (_queueClient == null)
            {
                var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
                _queueClient = CloudStorageAccount.Parse(connection).CreateCloudQueueClient();
            }
        }

        [HttpPost("{deviceId}")]
        public async Task<IActionResult> Post(string deviceId, [FromBody] LogMessage logMessage)
        {
            Debug.WriteLine(logMessage.Message);

            var queue = _queueClient.GetQueueReference("log-" + deviceId);
            await queue.CreateIfNotExistsAsync();
            await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(logMessage, Formatting.Indented)), TimeSpan.FromMinutes(15), null, null, null);

            return new AcceptedResult();
        }
    }
}