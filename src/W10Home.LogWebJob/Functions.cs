using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace W10Home.LogWebJob
{
	public class Functions
	{
		// This function will get triggered/executed when a new message is written 
		// on an Azure Queue called queue.
		public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
		{
			log.WriteLine(message);
		}

		public static void HandleOne([EventHubTrigger("log")] EventData eventData, TextWriter log)
		{
			var message = Encoding.UTF8.GetString(eventData.GetBytes());
			log.WriteLine(message);
		}

	}
}
