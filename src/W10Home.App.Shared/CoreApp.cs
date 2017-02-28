using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Restup.Webserver.File;
using Restup.Webserver.Http;
using Restup.Webserver.Rest;
using Restup.WebServer.Http;
using W10Home.Core.Configuration;
using W10Home.Core.Queing;
using W10Home.Interfaces;
using W10Home.IoTCoreApp;
using W10Home.IoTCoreApp.Auth;
using W10Home.IoTCoreApp.Controllers;
using W10Home.Plugin.AzureIoTHub;
using W10Home.Plugin.ETATouch;
using W10Home.Plugin.Twilio;

namespace W10Home.App.Shared
{
    internal class CoreApp
    {
		private Timer _everySecondTimer;
		private Timer _everyMinuteTimer;
		private HttpServer _httpServer;

		public async Task Run()
	    {
			// Get local serial number from disk

			// Build configuration object to configure all devices
			var configurationObject = new RootConfiguration();

			configurationObject.DeviceConfigurations = new List<IDeviceConfiguration>(new[]
			{
				new DeviceConfiguration
				{
					Name = "iothub",
					Type = "AzureIoTHubDevice",
					Properties = new Dictionary<string, string>()
					{
						{"ConnectionString" ,Config.AZURE_IOT_HUB_CONNECTION},
					}
				},
				//new DeviceConfiguration
				//{
				//	Name = "eta",
				//	Type = "ETATouchDevice",
				//	Properties = new Dictionary<string, string>()
				//	{
				//		{"ConnectionString", Config.ETA_TOUCH_URL}
				//	}
				//},
				//new DeviceConfiguration
				//{
				//	Name = "twilio",
				//	Type = "TwilioDevice",
				//	Properties = new Dictionary<string, string>()
				//	{
				//		{"AccountSid", Config.TWILIO_ACCOUNT_SID},
				//		{"AuthToken", Config.TWILIO_AUTH_TOKEN },
				//		{"OutgoingPhone", Config.TWILIO_OUTGOING_PHONE },
				//		{"ReceiverPhone", Config.TWILIO_RECEIVER_PHONE }
				//	}
				//}
			});

			var configString = JsonConvert.SerializeObject(configurationObject, Formatting.Indented);

			// init device registry and add devices
			var deviceRegistry = new DeviceRegistry();
			deviceRegistry.RegisterDeviceType<AzureIoTHubDevice>();
			//deviceRegistry.RegisterDeviceType<ETATouchDevice>();
			//deviceRegistry.RegisterDeviceType<TwilioDevice>();
			await deviceRegistry.InitializeDevicesAsync(configurationObject);

			// init IoC
			var container = new UnityContainer();
			container.RegisterInstance<IMessageQueue>(new MessageQueue());
			container.RegisterInstance<IDeviceRegistry>(deviceRegistry);
			var locator = new UnityServiceLocator(container);
			ServiceLocator.SetLocatorProvider(() => locator);

			// start background worker that collects and forwards data
			MessageLoopWorker();

			// define cron timers
			_everySecondTimer = new Timer(EverySecondTimerCallback, null, 1000, 1000);
			_everyMinuteTimer = new Timer(EveryMinuteTimerCallback, null, 60 * 1000, 60 * 1000);

			// start local webserver

			var authProvider = new BasicAuthorizationProvider("Login", new FixedCredentialsValidator());
			var restRouteHandler = new RestRouteHandler(authProvider);
			restRouteHandler.RegisterController<QueueController>();
			var configuration = new HttpServerConfiguration()
				.ListenOnPort(80)
				.RegisterRoute("api", restRouteHandler)
				.RegisterRoute(new StaticFileRouteHandler(@"Web", authProvider))
				.EnableCors(); // allow cors requests on all origins
							   //  .EnableCors(x => x.AddAllowedOrigin("http://specificserver:<listen-port>"));

			var httpServer = new HttpServer(configuration);
			_httpServer = httpServer;

			await httpServer.StartServerAsync();
		}
		private async void EveryMinuteTimerCallback(object state)
		{
			//var iotHub = ServiceLocator.Current.GetInstance<IDeviceRegistry>().GetDevice<AzureIoTHubDevice>();
			//var eta = ServiceLocator.Current.GetInstance<IDeviceRegistry>().GetDevice<ETATouchDevice>();
			//try
			//{
			//	var menu = await eta.GetMenuStructureFromEtaAsync();
			//	var value = await eta.GetValueFromEtaValuePathAsync(menu, "/Sys/Eingänge/Außentemperatur");
			//	double degrees = (double)value.Value / (double)value.ScaleFactor;
			//	await iotHub.SendMessageToIoTHubAsync("homecontroller", "home", "outdoortemp", degrees);
			//}
			//catch (Exception ex)
			//{
			//	Debug.WriteLine(ex.Message);
			//}
		}

		private void EverySecondTimerCallback(object state)
		{
		}

		private async void MessageLoopWorker()
		{
			var iotHub = ServiceLocator.Current.GetInstance<IDeviceRegistry>().GetDevice<AzureIoTHubDevice>();
			do
			{
				var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
				if (queue.TryDeque("windsensor", out QueueMessage message))
				{
					await iotHub.SendMessageToIoTHubAsync("homecontroller", "home", message.Key, Double.Parse(message.Value));
				}

				await Task.Delay(250);
			} while (true);
		}

		private void BackgroundScriptRunner(List<TreeItem> menu)
		{
			//do
			//{
			//	Script.RunFile("Scripts\\main.lua");
			//}
			//while (true);
		}
	}
}
