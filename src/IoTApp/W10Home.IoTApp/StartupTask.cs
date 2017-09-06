using Windows.ApplicationModel.Background;
using W10Home.Core.Queing;
using Windows.System;
using W10Home.App.Shared;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using MetroLog;
using MetroLog.Targets;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Practices.ServiceLocation;
using W10Home.App.Shared.Logging;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace W10Home.IoTCoreApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
	    private CoreApp _coreApp;
        private ILogger _log;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // This deferral should have an instance reference, if it doesn't... the GC will
            // come some day, see that this method is not active anymore and the local variable
            // should be removed. Which results in the application being closed.
            _deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;
            
            // this is one way to handle unobserved task exceptions but not the best
            //TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // configure logging first
            var telemetryClient = new TelemetryClient();
            telemetryClient.InstrumentationKey = "4e4ea96b-6b69-4aba-919b-558b4a4583ae";
            LogManagerFactory.DefaultConfiguration = new LoggingConfiguration();
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new DebugTarget());
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new EtwTarget());
            var streamingFileTarget = new StreamingFileTarget() {KeepLogFilesOpenForWrite = false};
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, streamingFileTarget);
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new ApplicationInsightsTarget(telemetryClient));
            // init custom metrolog logger for iot hub
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new IotHubTarget());


            _log = LogManagerFactory.DefaultLogManager.GetLogger<StartupTask>();
            _log.Info("Starting");
            // send package version to iot hub for tracking device software version
            var package = Windows.ApplicationModel.Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;
            _log.Info("Package version: "+version.Major+"."+version.Minor+"."+version.Build);

            _log.Trace("Launching CoreApp");
            _log.Trace("Local data folder: " + Windows.Storage.ApplicationData.Current.LocalFolder.Path);

            try
            {
                _coreApp = new CoreApp();
                await _coreApp.Run();
            }
            catch (Exception ex)
            {
                _log.Error("CoreApp Run crashed", ex);
                throw;
            }

            // The message Loop Worker runs in the background and checks for specific messages
			// which tell the CoreApp to either reboot the device or exit the app, which should
			// restart of the app
            _log.Trace("Launching MessageLoopWorker");
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
            _log.Info("StartupTask terminated for reason "+reason.ToString());
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
            _log.Trace("MessageLoopWorker");
			IMessageQueue queue = null;
			do
			{
				if (ServiceLocator.Current != null)
				{
					try
					{
						queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
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
                            _log.Info("Rebooting");
							ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.Zero);
						}
						else if (message.Key == "exit")
						{
                            _log.Info("Exiting");
							if (_deferral != null)
							{
								_deferral.Complete();
								_deferral = null;
								return;
							}
						}
					}
					catch (Exception ex)
					{
                        _log.Error("MessageLoopWorker", ex);
					}
				}
				await Task.Delay(250);
			} while (true);
		}


	}
}
