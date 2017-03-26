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
using W10Home.Plugin.ABUS.SecVest;
using W10Home.Plugin.AzureIoTHub;
using W10Home.Plugin.ETATouch;
using W10Home.Plugin.Twilio;
using System.Linq;

namespace W10Home.App.Shared
{
    internal class CoreApp
    {
		private Timer _everySecondTimer;
		private Timer _everyMinuteTimer;
		private HttpServer _httpServer;

		public async Task Run()
	    {
			// Build configuration object to configure all devices
			var configurationObject = new RootConfiguration();

			// check whether there is alread an iot hub configuration in the TPM


			configurationObject.DeviceConfigurations = new List<IDeviceConfiguration>(new[]
			{
				// by default iot hub configuration now uses TPM chip
				new DeviceConfiguration
				{
					Name = "iothub",
					Type = "AzureIoTHubDevice"
				},
				//new DeviceConfiguration()
				//{
				//	Name = "secvest",
				//	Type = "SecVestDevice",
				//	Properties = new Dictionary<string, string>()
				//	{
				//		{"ConnectionString", "https://192.168.0.22:4433/" },
				//		{"Username", "1234" },
				//		{"Password", "1234" }
				//	}
				//}
			});

			var configString = JsonConvert.SerializeObject(configurationObject, Formatting.Indented);

			// init device registry and add devices
			var deviceRegistry = new DeviceRegistry();
			deviceRegistry.RegisterDeviceType<AzureIoTHubDevice>();
			deviceRegistry.RegisterDeviceType<SecVestDevice>();
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
			_everyMinuteTimer = new Timer(EveryMinuteTimerCallbackAsync, null, 60 * 1000, 60 * 1000);

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
		private async void EveryMinuteTimerCallbackAsync(object state)
		{
			//// get status from secvest every minute
			//var secvest = ServiceLocator.Current.GetInstance<SecVestDevice>();
			//var channels = await secvest.GetChannelsAsync();
			//var statusChannel = (SecVestStatusChannel)channels.Single(c => c.Name == "status");
			//var status = await statusChannel.GetStatusAsync();

			//// send status to iothub queue for
			//var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
			//queue.Enqueue("iothub", new QueueMessage("secveststatus", JsonConvert.SerializeObject(status)));
		}

		private void EverySecondTimerCallback(object state)
		{
		}

		private async void MessageLoopWorker()
		{
			var iotHub = ServiceLocator.Current.GetInstance<IDeviceRegistry>().GetDevice<AzureIoTHubDevice>();
			var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
			do
			{
				if (queue.TryDeque("windsensor", out QueueMessage message))
				{
					// call function
					HandleWindSensorFunctionLua(message);
				}
				await Task.Delay(250);
			} while (true);
		}

		private void HandleWindSensorFunctionLua(QueueMessage message)
		{
			// call lua script with message, which is our dynamic function
		}
	}
}
