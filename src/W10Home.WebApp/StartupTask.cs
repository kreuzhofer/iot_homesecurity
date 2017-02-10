using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Background;
using Restup.Webserver.Http;
using Restup.Webserver.Rest;
using Restup.Webserver.File;
using System.Threading.Tasks;
using W10Home.Plugin.ETATouch;
using MoonSharp.Interpreter;
using Windows.Web.Http;
using Restup.WebServer.Http;
using W10Home.IoTCoreApp.Auth;
using W10Home.Plugin.Twilio;
using Windows.Devices.Gpio;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using W10Home.IoTCoreApp.Controllers;
using W10Home.Core.Queing;
using System.Threading;
using System.Diagnostics;
using W10Home.Core.Configuration;
using W10Home.Plugin.AzureIoTHub;
using W10Home.Interfaces;
using Newtonsoft.Json;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace W10Home.IoTCoreApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private HttpServer _httpServer;

        private BackgroundTaskDeferral _deferral;
		private const int LED_PIN = 6;
		private Timer _everySecondTimer;
		private Timer _everyMinuteTimer;

		public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // This deferral should have an instance reference, if it doesn't... the GC will
            // come some day, see that this method is not active anymore and the local variable
            // should be removed. Which results in the application being closed.
            _deferral = taskInstance.GetDeferral();


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
						{"ConnectionString" ,Config.AZURE_IOT_HUB_CONNECTION}
					}
				},
				new DeviceConfiguration
				{
					Name = "eta",
					Type = "ETATouchDevice",
					Properties = new Dictionary<string, string>()
					{
						{"ConnectionString", Config.ETA_TOUCH_URL}
					}
				},
				new DeviceConfiguration
				{
					Name = "twilio",
					Type = "TwilioDevice",
					Properties = new Dictionary<string, string>()
					{
						{"AccountSid", Config.TWILIO_ACCOUNT_SID},
						{"AuthToken", Config.TWILIO_AUTH_TOKEN },
						{"OutgoingPhone", Config.TWILIO_OUTGOING_PHONE },
						{"ReceiverPhone", Config.TWILIO_RECEIVER_PHONE }
					}
				}
			});

			var configString = JsonConvert.SerializeObject(configurationObject, Formatting.Indented);

			// init device registry and add devices
			var deviceRegistry = new DeviceRegistry();
			deviceRegistry.RegisterDeviceType<AzureIoTHubDevice>();
			deviceRegistry.RegisterDeviceType<ETATouchDevice>();
			deviceRegistry.RegisterDeviceType<TwilioDevice>();
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

            // Dont release deferral, otherwise app will stop
        }

		private async void EveryMinuteTimerCallback(object state)
		{
			var iotHub = ServiceLocator.Current.GetInstance<IDeviceRegistry>().GetDevice<AzureIoTHubDevice>();
			var eta = ServiceLocator.Current.GetInstance<IDeviceRegistry>().GetDevice<ETATouchDevice>();
			try
			{
				var menu = await eta.GetMenuStructureFromEtaAsync();
				var value = await eta.GetValueFromEtaValuePathAsync(menu, "/Sys/Eingänge/Außentemperatur");
				double degrees = (double)value.Value / (double)value.ScaleFactor;
				await iotHub.SendMessageToIoTHubAsync("homecontroller", "home", "outdoortemp", degrees);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
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
