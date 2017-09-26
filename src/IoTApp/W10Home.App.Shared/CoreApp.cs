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
#if SECVEST
using IoTHs.Plugin.ABUS.SecVest;
#endif
#if AZUREIOTHUB
using W10Home.Plugin.AzureIoTHub;
#endif
#if ETATOUCH
using W10Home.Plugin.ETATouch;
#endif
#if TWILIO
using IoTHs.Plugin.Twilio;
#endif
using System.Linq;
using Windows.Storage;
using Windows.System;
using MetroLog;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using W10Home.App.Shared.Logging;
using W10Home.Interfaces.Configuration;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using IoTHs.Plugin.HomeMatic;
using W10Home.Core.Channels;


namespace W10Home.App.Shared
{
    internal class CoreApp
    {
		private HttpServer _httpServer;
        private readonly ILogger _log = LogManagerFactory.DefaultLogManager.GetLogger<CoreApp>();
        private Timer _everyMinuteTimer;

        public async Task Run()
	    {
            _log.Trace("Run");

            // Build configuration object to configure all devices
			DeviceConfigurationModel configurationObject = new DeviceConfigurationModel();

			// first try to load the configuration file from the LocalFolder
			var localStorage = ApplicationData.Current.LocalFolder;
			var file = await localStorage.TryGetItemAsync("configuration.json");
		    if (file != null) // file exists, continue to deserialize into actual configuration object
		    {
			    // local file content
			    var configFileContent = await FileIO.ReadTextAsync((IStorageFile) file);
			    configurationObject = JsonConvert.DeserializeObject<DeviceConfigurationModel>(configFileContent);
		    }
		    else // there is not yet a configuration file, tell AzureIoTHubDevice to load it from the cloud and then restart
		    {
			    configurationObject.DevicePluginConfigurations = new List<DevicePluginConfigurationModel>(new[]
			    {
				    // by default iot hub configuration now uses TPM chip
				    new DevicePluginConfigurationModel()
				    {
					    Name = "iothub",
					    Type = "AzureIoTHubDevice",
					    Properties = new Dictionary<string, string>()
					    {
							{ "TryLoadConfiguration", "true" }
						    //{ "ConnectionString", "IOT_HUB_CONNECTION_STRING_ONLY_FOR_DEBUGGING"}
					    }
				    }
			    });
		    }

			//configurationObject.DeviceConfigurations = new List<DeviceConfiguration>(new[]
			//{
			//	// by default iot hub configuration now uses TPM chip
			//	new DeviceConfiguration
			//	{
			//		Name = "iothub",
			//		Type = "AzureIoTHubDevice",
			//		Properties = new Dictionary<string, string>()
			//		{
			//			//{ "ConnectionString", ""}
			//		}
			//	},
			//	//new DeviceConfiguration()
			//	//{
			//	//	Name = "secvest",
			//	//	Type = "SecVestDevice",
			//	//	Properties = new Dictionary<string, string>()
			//	//	{
			//	//		{"ConnectionString", "https://192.168.0.22:4433/" },
			//	//		{"Username", "1234" },
			//	//		{"Password", "1234" }
			//	//	}
			//	//},
			//	//new DeviceConfiguration()
			//	//{
			//	//	Name = "cam1",
			//	//	Type = ""
			//	//}
			//});
			//configurationObject.Functions = new List<FunctionDeclaration>(new FunctionDeclaration[]
			//{
			//	//new FunctionDeclaration()
			//	//{
			//	//	TriggerType = FunctionTriggerType.RecurringIntervalTimer,
			//	//	Name = "PollSecvestEveryMinute",
			//	//	Interval = 10*1000, // polling interval measured in miliseconds
			//	//	Code = @"
			//	//	function run()
			//	//		-- get status from secvest every minute
			//	//		secvest = registry.getDevice(""secvest"");
			//	//		statusChannel = secvest.getChannel(""status"");
			//	//		statusValue = statusChannel.read();
			//	//		if(statusValue != nil) then
			//	//			-- send status to iothub queue
			//	//			queue.enqueue(""iothub"", ""status@secvest"", statusValue, ""json"");
			//	//			end;
			//	//		return 0;
			//	//	end;
			//	//	"
			//	//},
			//	new FunctionDeclaration()
			//	{
			//		TriggerType = FunctionTriggerType.MessageQueue,
			//		Name = "WindSensorQueueHandler",
			//		QueueName = "windsensor",
			//		Code = @"
			//		function run(message)
			//			if(message.Key == ""Wind"") then
			//				message.Key = ""windspeed@windsensor"";
			//				message.Tag = ""windspeed_kmh""
			//			end;
			//			if(message.Key == ""Temperature"") then
			//				message.Key = ""temperature@windsensor"";
			//				message.Tag = ""temperature_celsius""
			//			end;

			//			queue.enqueue(""iothub"", message); -- simply forward to iot hub message queue
			//			return 0;
			//		end;
			//		"
			//	},
			//	new FunctionDeclaration()
			//	{
			//		TriggerType = FunctionTriggerType.MessageQueue,
			//		Name = "SecvestOutputSwitchHandler",
			//		QueueName = "secvestoutput",
			//		Code = @"
			//		function run(message)
			//			secvest = registry.getDevice(""secvest"");
			//			statusChannel = secvest.getChannel(""status"");
			//			statusChannel.setOutput(message.Key, message.Value);
			//			return 0;
			//		end;
			//		"
			//	}
			//});

			var configString = JsonConvert.SerializeObject(configurationObject, Formatting.Indented);
			Debug.WriteLine(configString);

			// init device registry and add devices
			var deviceRegistry = new DeviceRegistry();
#if AZUREIOTHUB
            deviceRegistry.RegisterDeviceType<AzureIoTHubDevice>();
#endif
#if SECVEST
            deviceRegistry.RegisterDeviceType<SecVestDevice>();
#endif
#if ETATOUCH
            deviceRegistry.RegisterDeviceType<EtaTouchDevice>();
#endif
#if TWILIO
            deviceRegistry.RegisterDeviceType<TwilioDevice>();
#endif
            deviceRegistry.RegisterDeviceType<HomeMaticDevice>();

			// add functions engine
			var functionsEngine = new FunctionsEngine();

			// init IoC
			var container = new UnityContainer();
	        container.RegisterInstance<DeviceConfigurationModel>(configurationObject);
			container.RegisterInstance<IMessageQueue>(new MessageQueue());
	        container.RegisterType<ChannelValueCache>(new ContainerControlledLifetimeManager());
			container.RegisterInstance<IDeviceRegistry>(deviceRegistry);
			container.RegisterInstance(functionsEngine);

            // register device instances
#if AZUREIOTHUB
            container.RegisterType<AzureIoTHubDevice>(new ContainerControlledLifetimeManager());
#endif
#if SECVEST
            container.RegisterType<SecVestDevice>(new ContainerControlledLifetimeManager());
#endif
#if ETATOUCH
            container.RegisterType<EtaTouchDevice>(new ContainerControlledLifetimeManager());
#endif
#if TWILIO
            container.RegisterType<TwilioDevice>(new ContainerControlledLifetimeManager());
#endif
	        container.RegisterType<HomeMaticDevice>(new ContainerControlledLifetimeManager());

			// make Unity container available to ServiceLocator
			var locator = new UnityServiceLocator(container);
			ServiceLocator.SetLocatorProvider(() => locator);

			// configure devices
			await deviceRegistry.InitializeDevicesAsync(configurationObject);

			// start lua engine
			functionsEngine.Initialize(configurationObject, CancellationToken.None);
			
			// define cron timers
			//_everySecondTimer = new Timer(EverySecondTimerCallback, null, 1000, 1000);
			_everyMinuteTimer = new Timer(EveryMinuteTimerCallbackAsync, null, 60 * 1000, 60 * 1000);

			// start local webserver
			var authProvider = new BasicAuthorizationProvider("Login", new FixedCredentialsValidator());
			var restRouteHandler = new RestRouteHandler(authProvider);
			restRouteHandler.RegisterController<QueueController>();
			var configuration = new HttpServerConfiguration()
				.ListenOnPort(80)
				.RegisterRoute("api", restRouteHandler)
				.RegisterRoute(new StaticFileRouteHandler(@"Web"))
				.EnableCors(); // allow cors requests on all origins
							   //  .EnableCors(x => x.AddAllowedOrigin("http://specificserver:<listen-port>"));

			var httpServer = new HttpServer(configuration);
			_httpServer = httpServer;

			await _httpServer.StartServerAsync();
		}

        private void EveryMinuteTimerCallbackAsync(object state)
        {
            // report memory usage every minute
            var usageReport = MemoryManager.GetAppMemoryReport();
            var messageQueue = ServiceLocator.Current.GetInstance<IMessageQueue>();
            _log.Trace("Memory usage: "+usageReport.TotalCommitUsage+" of max "+usageReport.TotalCommitLimit+ " ~ "+String.Format("{0:P2}",usageReport.TotalCommitUsage/usageReport.TotalCommitLimit));
            messageQueue.Enqueue("iothub", "appmemory", JsonConvert.SerializeObject(usageReport), "json");
        }
    }
}
