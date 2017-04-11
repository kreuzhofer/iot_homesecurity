using Microsoft.Practices.ServiceLocation;
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
using W10Home.Core.Queing;

namespace W10Home.IoTCoreApp.Controllers
{
	[Authorize]
    [RestController(InstanceCreationType.Singleton)]
    internal class QueueController
    {
        [UriFormat("/queue/{queuename}")]
        public IPostResponse PostMessage(string queuename, [FromContent] QueueMessage message)
        {
			var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
			Debug.WriteLine("Queue: " + queuename + "|Message: " + message.ToString());
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
