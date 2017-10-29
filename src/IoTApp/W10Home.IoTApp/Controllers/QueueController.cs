using Restup.Webserver.Attributes;
using Restup.Webserver.Models.Contracts;
using Restup.Webserver.Models.Schemas;
using Restup.WebServer.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Queing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace W10Home.IoTCoreApp.Controllers
{
	[Authorize]
    [RestController(InstanceCreationType.Singleton)]
    internal class QueueController
    {
        private readonly ILogger _log;

        public QueueController(LoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<QueueController>();
        }

        [UriFormat("/queue/{queuename}")]
        public IPostResponse PostMessage(string queuename, [FromContent] QueueMessage message)
        {
			var queue = ServiceLocator.Current.GetService<IMessageQueue>();
			_log.LogTrace("Queue: " + queuename + "|Message: " + message.ToString());
			queue.Enqueue(queuename, message);
			return new PostResponse(PostResponse.ResponseStatus.Created);
        }

        [UriFormat("/queue")]
        public IGetResponse GetQueues()
        {
            return new GetResponse(GetResponse.ResponseStatus.OK);
        }

		[UriFormat("/queue/{queuename}")]
		public IGetResponse GetQueue(string queuename)
		{
			return new GetResponse(GetResponse.ResponseStatus.OK);
		}
	}
}
