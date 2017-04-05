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
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using W10Home.Interfaces.Configuration;

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
					Type = "AzureIoTHubDevice",
					Properties = new Dictionary<string, string>()
					{
						//{ "ConnectionString", "IOT_HUB_CONNECTION_STRING_ONLY_FOR_DEBUGGING"}
					}
				},
				new DeviceConfiguration()
				{
					Name = "secvest",
					Type = "SecVestDevice",
					Properties = new Dictionary<string, string>()
					{
						{"ConnectionString", "https://192.168.0.22:4433/" },
						{"Username", "1234" },
						{"Password", "1234" }
					}
				}
			});
			configurationObject.Functions = new List<IFunctionDeclaration>(new[]
			{
				new RecurringIntervalTriggeredFunctionDeclaration()
				{
					TriggerType = FunctionTriggerType.RecurringIntervalTimer,
					Name = "PollSecvestEveryMinute",
					Interval = 10*1000, // ten seconds test interval
					Code = @"
					function run()
						-- get status from secvest every minute
						secvest = registry.getDevice(""secvest"");
						statusChannel = secvest.getChannel(""status"");
						statusValue = statusChannel.read();

						-- send status to iothub queue
						queue.enqueue(""iothub"", ""status@secvest"", statusValue);
						return 0;
					end;
					"
				}
			});

			var configString = JsonConvert.SerializeObject(configurationObject, Formatting.Indented);

			// init device registry and add devices
			var deviceRegistry = new DeviceRegistry();
			deviceRegistry.RegisterDeviceType<AzureIoTHubDevice>();
			deviceRegistry.RegisterDeviceType<SecVestDevice>();
			deviceRegistry.RegisterDeviceType<ETATouchDevice>();
			deviceRegistry.RegisterDeviceType<TwilioDevice>();

			// add functions engine
			var functionsEngine = new FunctionsEngine();

			// init IoC
			var container = new UnityContainer();
			container.RegisterInstance<IMessageQueue>(new MessageQueue());
			container.RegisterInstance<IDeviceRegistry>(deviceRegistry);
			container.RegisterInstance(functionsEngine);

			// register device instances
			container.RegisterType<SecVestDevice>(new ContainerControlledLifetimeManager());
		    container.RegisterType<ETATouchDevice>(new ContainerControlledLifetimeManager());
		    container.RegisterType<TwilioDevice>(new ContainerControlledLifetimeManager());

			var locator = new UnityServiceLocator(container);
			ServiceLocator.SetLocatorProvider(() => locator);

			// configure devices
			await deviceRegistry.InitializeDevicesAsync(configurationObject);

			// start lua engine
			functionsEngine.Initialize(configurationObject);
			
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
			//var statusChannel = (SecVestStatusChannel)secvest.GetChannel("status");
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
					try
					{
						// call function
						HandleMessageLua(message, "queue.enqueue(\"iothub\", message); -- simply forward to iot hub message queue");
					}
					catch(Exception ex)
					{
						Debug.WriteLine(ex.Message);
						//todo log
					}
				}
				await Task.Delay(250);
			} while (true);
		}

		private void HandleMessageLua(QueueMessage message, string scriptCode)
		{
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
			var iotHub = ServiceLocator.Current.GetInstance<IDeviceRegistry>().GetDevice<AzureIoTHubDevice>();
			var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
			var secvest = ServiceLocator.Current.GetInstance<SecVestDevice>();

			// call lua script with message, which is our dynamic function
			var script = new Script(CoreModules.Preset_Complete);
			var iotHubeDynValue = UserData.Create(iotHub);
			script.Globals.Set("iothub", iotHubeDynValue);
			var messageDynValue = UserData.Create(message);
			script.Globals.Set("message", messageDynValue);
			var messageQueueDynValue = UserData.Create(queue);
			script.Globals.Set("queue", messageQueueDynValue);
			var secvestDynValue = UserData.Create(secvest);
			script.Globals.Set("secvest", secvestDynValue);

			script.DoString(scriptCode);
		}
	}
}
