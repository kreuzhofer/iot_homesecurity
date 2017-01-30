using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Background;
using Restup.Webserver.Http;
using Restup.Webserver.Rest;
using Restup.Webserver.File;
using System.Threading.Tasks;
using W10Home.Plugin.AzureIoTHub;
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

#pragma warning disable IDE1006 // Naming Styles
		public async void Run(IBackgroundTaskInstance taskInstance)
#pragma warning restore IDE1006 // Naming Styles
        {
            // This deferral should have an instance reference, if it doesn't... the GC will
            // come some day, see that this method is not active anymore and the local variable
            // should be removed. Which results in the application being closed.
            _deferral = taskInstance.GetDeferral();


			// Build configuration object to configure all devices
			var configurationObject = new RootConfiguration();

			configurationObject.DeviceConfigurations = new List<DeviceConfiguration>(new[]
			{
				new DeviceConfiguration
				{
					Type = "AzureIoTHubPlugin",
					Properties = new Dictionary<string, string>()
					{
						{"ConnectionString" ,"HostName=dkreuzhiothub01.azure-devices.net;DeviceId=homecontroller;SharedAccessKey=aVJzBv3boD79ZnE1GCmVCSFJRkC/1DvmWgqzbaogX7U="}
					}
				},
				new DeviceConfiguration
				{
					Type = "EtaTouchDevice"
				}
			});

            // init IoC
            var container = new UnityContainer();
            container.RegisterInstance<IMessageQueue>(new MessageQueue());

            // configure IoT Hub plugin
            var iotHub = new AzureIoTHubPlugin(Config.AZURE_IOT_HUB_CONNECTION);
            container.RegisterInstance(iotHub);

            // get data from ETA
            var eta = new ETATouchDevice(Config.ETA_TOUCH_URL);
            container.RegisterInstance(eta);

			// configure Twilio
			List<TwilioSmsChannelConfiguration> channelConfigurations = new List<TwilioSmsChannelConfiguration>();
			channelConfigurations.Add(new TwilioSmsChannelConfiguration(Config.TWILIO_OUTGOING_PHONE, Config.TWILIO_RECEIVER_PHONE));
			var twilio = new TwilioDevice(Config.TWILIO_ACCOUNT_SID, Config.TWILIO_ACCOUNT_TOKEN, channelConfigurations);
            container.RegisterInstance(twilio);

            // finalize service locator
            var locator = new UnityServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => locator);

            //await (await _twilio.GetChannelsAsync()).Single(c => c.Name == "SMS").SendMessageAsync("Homeautomation starting...");

            // start background worker that collects and forwards data
            MessageLoopWorker();

			// define cron timers
			_everySecondTimer = new Timer(everySecondTimerCallback, null, 1000, 1000);
			_everyMinuteTimer = new Timer(everyMinuteTimerCallback, null, 60 * 1000, 60 * 1000);

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

		private async void everyMinuteTimerCallback(object state)
		{
			var iotHub = ServiceLocator.Current.GetInstance<AzureIoTHubPlugin>();
			var eta = ServiceLocator.Current.GetInstance<ETATouchDevice>();
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

		private void everySecondTimerCallback(object state)
		{
		}

		private async void MessageLoopWorker()
		{
			var iotHub = ServiceLocator.Current.GetInstance<AzureIoTHubPlugin>();
			do
			{
				var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
				QueueMessage message;
				if(queue.TryDeque("windsensor", out message))
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


		private async Task FlashLed()
        {
            var gpio = GpioController.GetDefault();
            var ledPin = gpio.OpenPin(LED_PIN);
            // Initialize LED to the OFF state by first writing a HIGH value
            // We write HIGH because the LED is wired in a active LOW configuration
            ledPin.SetDriveMode(GpioPinDriveMode.Output);
            ledPin.Write(GpioPinValue.Low);
            await Task.Delay(200);
            ledPin.Write(GpioPinValue.High);
        }
    }
}
