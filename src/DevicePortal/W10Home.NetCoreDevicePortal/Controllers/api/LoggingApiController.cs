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
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using W10Home.NetCoreDevicePortal.Models;
using W10Home.NetCoreDevicePortal.Security;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [ApiKeyAuthentication()]
    [Produces("application/json")]
    [Route("api/Logging")]
    public class LoggingApiController : Controller
    {
        private static CloudQueueClient _queueClient;
        private static CloudTableClient _tableClient;

        public LoggingApiController(IConfiguration configuration)
        {
            if (_queueClient == null || _tableClient == null)
            {
                var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
                var account = CloudStorageAccount.Parse(connection);
                _queueClient = account.CreateCloudQueueClient();
                _tableClient = account.CreateCloudTableClient();
            }
        }

        [HttpPost("{deviceId}")]
        public async Task<IActionResult> Post(string deviceId, [FromBody] LogMessage logMessage)
        {
            Debug.WriteLine(logMessage.Message);

            try
            {
                // queue to device specific log table
                var queue = _queueClient.GetQueueReference("log-" + deviceId);
                await queue.CreateIfNotExistsAsync();
                await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(logMessage, Formatting.Indented)), TimeSpan.FromMinutes(1), null, null, null);
            }
            catch
            {
                // ignore for the device specific queue, not so important
            }

            // queue to global device log table to process further with log analytics (forwarding will be done by azure function)
            var devicelogqueue = _queueClient.GetQueueReference("devicelog");
            await devicelogqueue.CreateIfNotExistsAsync();
            await devicelogqueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(logMessage, Formatting.Indented)), null, null, null, null);

            return new AcceptedResult();
        }
    }
}