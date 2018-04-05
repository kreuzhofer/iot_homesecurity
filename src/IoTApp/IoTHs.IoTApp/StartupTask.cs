using Windows.ApplicationModel.Background;
using Windows.System;
using System.Diagnostics;
using System;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Authentication;
using IoTHs.Core.Channels;
using IoTHs.Core.Configuration;
using IoTHs.Core.Logging;
using IoTHs.Core.Lua;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Microsoft.Extensions.Logging.Debug;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace W10Home.IoTCoreApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
	    private CoreApp _coreApp;
        private ILogger _log;
        private IPluginRegistry _pluginRegistry;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // This deferral should have an instance reference, if it doesn't... the GC will
            // come some day, see that this method is not active anymore and the local variable
            // should be removed. Which results in the application being closed.
            _deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            // this is one way to handle unobserved task exceptions but not the best
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // configure logging first

            // init IoC
            // see: http://intellitect.com/net-core-dependency-injection/#ActivatorUtilities
            // and https://stackify.com/net-core-dependency-injection/
            // and http://derpturkey.com/vnext-dependency-injection-overview/
            var container = new ServiceCollection();

            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging?tabs=aspnetcore2x#tabpanel_J929VbWwYc_aspnetcore2x
            container.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddRest();
                builder.SetMinimumLevel(LogLevel.Trace);
                //builder.AddFilter<DebugLoggerProvider>("Default", LogLevel.Trace);
                //builder.AddFilter("IoTHs.Plugin.AzureIoTHub", LogLevel.Debug);
            });

            container.AddSingleton<IPluginRegistry, PluginRegistry>();

            container.AddSingleton<IMessageQueue>(new MessageQueue());
            container.AddSingleton<ChannelValueCache>();
            container.AddSingleton<DeviceConfigurationProvider>();
            container.AddTransient<CoreApp>();
            container.AddSingleton<FunctionsEngine>();
            container.AddSingleton<IApiAuthenticationService, ApiAuthenticationService>();

            container.AddSingleton<IAzureIoTHubPlugin, AzureIoTHubPlugin>();
#if ABUS
            container.AddTransient<SecVestPlugin>();
#endif
            container.AddTransient<EtaTouchPlugin>();
#if TWILIO
            container.AddTransient<TwilioPlugin>();
#endif
            container.AddTransient<HomeMaticPlugin>();
#if MQTTBROKER
            container.AddTransient<MqttBrokerPlugin>();
#endif

            // container available globally
            var locator = container.BuildServiceProvider();
            ServiceLocator.SetLocatorProvider(() => locator);

            // init device registry and add devices
            _pluginRegistry = locator.GetService<IPluginRegistry>();
            _pluginRegistry.RegisterPluginType<IAzureIoTHubPlugin>();
#if ABUS
            _pluginRegistry.RegisterPluginType<SecVestPlugin>();
#endif
            _pluginRegistry.RegisterPluginType<EtaTouchPlugin>();
#if TWILIO
            _pluginRegistry.RegisterPluginType<TwilioPlugin>();
#endif
            _pluginRegistry.RegisterPluginType<HomeMaticPlugin>();
#if MQTTBROKER
            _pluginRegistry.RegisterPluginType<MqttBrokerPlugin>();
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

        private async void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _log.LogError("Unobserved exception handled: " + e.Exception.Flatten().ToString());
            await Task.Delay(5000);
            e.SetObserved();
        }

        private async void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _log.LogInformation("StartupTask terminated for reason "+reason.ToString());
            await Task.Delay(5000);
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
                            await Task.Delay(5000);
							ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.Zero);
						}
                        else if (message.Key == "shutdown")
                        {
                            _log.LogInformation("Shutting down");
                            await Task.Delay(5000);
                            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.Zero);
                        }
                        else if (message.Key == "exit")
						{
                            _log.LogInformation("Exiting");
                            await Task.Delay(5000);
                            if (_deferral != null)
							{
								_deferral.Complete();
								_deferral = null;
								return;
							}
						}
					    if (message.Key == "restart")
					    {
					        _log.LogInformation("Restarting");
                            try
					        {
					            await _coreApp.ShutdownAsync();
					        }
					        catch (Exception ex)
					        {
                                _log.LogError(ex, "CoreApp Shutdown unsuccessful. Restarting...");
                                if (_deferral != null)
                                {
                                    _deferral.Complete();
                                    _deferral = null;
                                    return;
                                }
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

