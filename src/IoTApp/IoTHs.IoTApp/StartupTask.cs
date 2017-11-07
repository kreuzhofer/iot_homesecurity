using Windows.ApplicationModel.Background;
using Windows.System;
using System.Diagnostics;
using System;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Channels;
using IoTHs.Core.Configuration;
using IoTHs.Core.Logging;
using IoTHs.Core.Queing;
using IoTHs.Devices.Interfaces;
#if ABUS
using IoTHs.Plugin.ABUS.SecVest;
#endif
using IoTHs.Plugin.AzureIoTHub;
using IoTHs.Plugin.ETATouch;
using IoTHs.Plugin.HomeMatic;
#if MQTTBROKER
using IoTHs.Plugin.MQTTBroker;
#endif
#if TWILIO
using IoTHs.Plugin.Twilio;
#endif
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using W10Home.IoTCoreApp.Lua;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace W10Home.IoTCoreApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
	    private CoreApp _coreApp;
        private ILogger _log;
        private IDeviceRegistry _deviceRegistry;
        private TelemetryClient _telemetryClient;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // This deferral should have an instance reference, if it doesn't... the GC will
            // come some day, see that this method is not active anymore and the local variable
            // should be removed. Which results in the application being closed.
            _deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            // this is one way to handle unobserved task exceptions but not the best
            //TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Init Application Insights
            _telemetryClient = new TelemetryClient {InstrumentationKey = "4e4ea96b-6b69-4aba-919b-558b4a4583ae"};

            // configure logging first

            // init IoC
            var container = new ServiceCollection();

            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging?tabs=aspnetcore2x#tabpanel_J929VbWwYc_aspnetcore2x
            container.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddRest();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            container.AddSingleton<IDeviceRegistry, DeviceRegistry>();

            container.AddSingleton<IMessageQueue>(new MessageQueue());
            container.AddSingleton<ChannelValueCache>();
            container.AddSingleton<DeviceConfigurationProvider>();
            container.AddTransient<CoreApp>();
            container.AddSingleton<FunctionsEngine>();

            container.AddSingleton<IAzureIoTHubDevice, AzureIoTHubDevice>();
#if ABUS
            container.AddTransient<SecVestDevice>();
#endif
            container.AddTransient<EtaTouchDevice>();
#if TWILIO
            container.AddTransient<TwilioDevice>();
#endif
            container.AddTransient<HomeMaticDevice>();
#if MQTTBROKER
            container.AddTransient<MQTTBrokerPlugin>();
#endif

            // container available globally
            var locator = container.BuildServiceProvider();
            ServiceLocator.SetLocatorProvider(() => locator);

            // init device registry and add devices
            _deviceRegistry = locator.GetService<IDeviceRegistry>();
            _deviceRegistry.RegisterDeviceType<IAzureIoTHubDevice>();
#if ABUS
            _deviceRegistry.RegisterDeviceType<SecVestDevice>();
#endif
            _deviceRegistry.RegisterDeviceType<EtaTouchDevice>();
#if TWILIO
            _deviceRegistry.RegisterDeviceType<TwilioDevice>();
#endif
            _deviceRegistry.RegisterDeviceType<HomeMaticDevice>();
#if MQTTBROKER
            _deviceRegistry.RegisterDeviceType<MQTTBrokerPlugin>();
#endif

            _log = locator.GetService<ILoggerFactory>().CreateLogger<StartupTask>();
            _log.LogInformation("Starting");
            // send package version to iot hub for tracking device software version
            var package = Windows.ApplicationModel.Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;
            _log.LogInformation("Package version: "+version.Major+"."+version.Minor+"."+version.Build);

            _log.LogTrace("Launching CoreApp");
            _log.LogTrace("Local data folder: " + Windows.Storage.ApplicationData.Current.LocalFolder.Path);

            try
            {
                _coreApp = locator.GetService<CoreApp>();
                await _coreApp.RunAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CoreApp Run crashed");
                throw;
            }

            // The message Loop Worker runs in the background and checks for specific messages
			// which tell the CoreApp to either reboot the device or exit the app, which should
			// restart of the app
            _log.LogTrace("Launching MessageLoopWorker");
	        MessageLoopWorker();

	        // Dont release deferral, otherwise app will stop
        }

        //private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        //{
        //    Debug.WriteLine("Unobserved exception handled: "+e.Exception.Flatten().ToString());
        //    e.SetObserved();
        //}

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _log.LogInformation("StartupTask terminated for reason "+reason.ToString());
            //a few reasons that you may be interested in.
            switch (reason)
            {
                case BackgroundTaskCancellationReason.Abort:
                    //app unregistered background task (amoung other reasons).
                    break;
                case BackgroundTaskCancellationReason.Terminating:
                    //system shutdown
                    break;
                case BackgroundTaskCancellationReason.ConditionLoss:
                    break;
                case BackgroundTaskCancellationReason.SystemPolicy:
                    break;
            }
            _deferral.Complete();
        }

        private async void MessageLoopWorker()
		{
            _log.LogTrace("MessageLoopWorker");
			IMessageQueue queue = null;
			do
			{
				if (ServiceLocator.Current != null)
				{
					try
					{
						queue = ServiceLocator.Current.GetService<IMessageQueue>();
					}
					catch
					{
						/// ignore, might be too early
					}
				}
			} while (queue == null);
			do
			{
				if (queue.TryDeque("management", out QueueMessage message))
				{
					try
					{
						if (message.Key == "reboot")
						{
                            _log.LogInformation("Rebooting");
							ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.Zero);
						}
						else if (message.Key == "exit")
						{
                            _log.LogInformation("Exiting");
							if (_deferral != null)
							{
								_deferral.Complete();
								_deferral = null;
								return;
							}
						}
					    if (message.Key == "restart")
					    {
					        try
					        {
					            await _coreApp.ShutdownAsync();
					        }
					        catch (Exception ex)
					        {
                                _log.LogError(ex, "CoreApp Shutdown unsuccessful");
					            throw;
					        }
					        try
					        {
					            _coreApp = ServiceLocator.Current.GetService<CoreApp>();
					            await _coreApp.RunAsync();
					        }
					        catch (Exception ex)
					        {
					            _log.LogError(ex, "CoreApp Run crashed");
					            throw;
					        }
                        }
					}
					catch (Exception ex)
					{
                        _log.LogError(ex, "MessageLoopWorker");
					}
				}
				await Task.Delay(IoTHsConstants.MessageLoopDelay);
			} while (true);
		}


	}
}

