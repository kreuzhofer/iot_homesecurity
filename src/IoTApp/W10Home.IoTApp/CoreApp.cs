using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Restup.Webserver.File;
using Restup.Webserver.Http;
using Restup.Webserver.Rest;
using Restup.WebServer.Http;
using W10Home.IoTCoreApp.Auth;
using W10Home.IoTCoreApp.Controllers;
using IoTHs.Plugin.ABUS.SecVest;
using IoTHs.Plugin.AzureIoTHub;
using IoTHs.Plugin.Twilio;
using Windows.Storage;
using Windows.System;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Channels;
using IoTHs.Core.Configuration;
using IoTHs.Core.Queing;
using IoTHs.Devices.Interfaces;
using IoTHs.Plugin.ETATouch;
using IoTHs.Plugin.HomeMatic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace W10Home.App.Shared
{
    internal class CoreApp
    {
		private HttpServer _httpServer;
        private ILogger _log;
        private Timer _everyMinuteTimer;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private FunctionsEngine _functionsEngine;
        private IDeviceRegistry _deviceRegistry;
        private DeviceConfigurationProvider _configurationProvider;

        public CoreApp(IDeviceRegistry deviceRegistry, FunctionsEngine functionsEngine, ILoggerFactory loggerFactory, DeviceConfigurationProvider configurationProvider)
        {
            _deviceRegistry = deviceRegistry;
            _functionsEngine = functionsEngine;
            _log = loggerFactory.CreateLogger<CoreApp>();
            _configurationProvider = configurationProvider;
        }

        public async Task RunAsync()
	    {
            _log.LogTrace("Run");

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

#region manual scripting

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

#endregion

            var configString = JsonConvert.SerializeObject(configurationObject, Formatting.Indented);
			Debug.WriteLine(configString);

	        _configurationProvider.SetConfiguration(configurationObject);

			// configure devices
			await _deviceRegistry.InitializeDevicesAsync(configurationObject);

			// start lua engine
			_functionsEngine.Initialize(configurationObject);
			
			// define cron timers
			//_everySecondTimer = new Timer(EverySecondTimerCallback, null, 1000, 1000);
			_everyMinuteTimer = new Timer(EveryMinuteTimerCallbackAsync, null, 60 * 1000, 60 * 1000);

			await StartWebserverAsync();
	    }

        public async Task StartWebserverAsync()
        {
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

        public void StopWebserver()
        {
            _log.LogTrace("Stop webserver");
            if (_httpServer != null)
            {
                _httpServer.StopServer();
                _httpServer = null;
            }
        }

        public async Task ShutdownAsync()
        {
            _log.LogTrace("Stop timers");
            if (_everyMinuteTimer != null)
            {
                _everyMinuteTimer.Dispose();
                _everyMinuteTimer = null;
            }

            StopWebserver();

            if (_functionsEngine != null)
            {
                _functionsEngine.Shutdown();
                _functionsEngine = null;
            }

            if (_deviceRegistry != null)
            {
                await _deviceRegistry.TeardownDevicesAsync();
                _deviceRegistry = null;
            }
        }

        private void EveryMinuteTimerCallbackAsync(object state)
        {
            // report memory usage every minute
            var usageReport = MemoryManager.GetAppMemoryReport();
            var messageQueue = ServiceLocator.Current.GetService<IMessageQueue>();
            _log.LogTrace("Memory usage: "+usageReport.TotalCommitUsage+" of max "+usageReport.TotalCommitLimit+ " ~ "+String.Format("{0:P2}",usageReport.TotalCommitUsage/usageReport.TotalCommitLimit));
            messageQueue.Enqueue("iothub", "appmemory", $"{usageReport.TotalCommitUsage}", ChannelType.None.ToString());
        }
    }
}
