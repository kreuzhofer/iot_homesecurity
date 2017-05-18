using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;

namespace W10Home.LogWebJob
{
	// To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
	class Program
	{
		// Please set the following connection strings in app.config for this WebJob to run:
		// AzureWebJobsDashboard and AzureWebJobsStorage
		static void Main()
		{
			var config = new JobHostConfiguration();

			if (config.IsDevelopment)
			{
				config.UseDevelopmentSettings();
			}

			var eventHubConfig = new EventHubConfiguration();
			string eventHubName = "log";
			eventHubConfig.AddSender(eventHubName, "Endpoint=sb://pocabus.servicebus.cloudapi.de/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=/Soixr306N0ZHlI0d/2agJ6zXSShQ5D/FzHVCSGtIyU=");
			eventHubConfig.AddReceiver(eventHubName, "Endpoint=sb://pocabus.servicebus.cloudapi.de/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=/Soixr306N0ZHlI0d/2agJ6zXSShQ5D/FzHVCSGtIyU=");

			config.UseEventHub(eventHubConfig);


			var host = new JobHost(config);
			// The following code ensures that the WebJob will be running continuously
			host.RunAndBlock();
		}
	}
}
