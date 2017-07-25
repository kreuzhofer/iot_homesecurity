using Windows.ApplicationModel.Background;
using W10Home.Core.Queing;
using Windows.System;
using W10Home.App.Shared;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using MetroLog;
using Microsoft.Practices.ServiceLocation;

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

            // configure logging first
            GlobalCrashHandler.Configure();
            _log = LogManagerFactory.DefaultLogManager.GetLogger<CoreApp>();

            _log.Trace("Launching CoreApp");
            _log.Trace("Local data folder: " + Windows.Storage.ApplicationData.Current.LocalFolder.Path);
            _coreApp = new CoreApp();
			await _coreApp.Run();

			// The message Loop Worker runs in the background and checks for specific messages
			// which tell the CoreApp to either reboot the device or exit the app, which should
			// restart of the app
            _log.Trace("Launching MessageLoopWorker");
	        MessageLoopWorker();

	        // Dont release deferral, otherwise app will stop
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
							ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.Zero);
						}
						else if (message.Key == "exit")
						{
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
						Debug.WriteLine(ex.Message);
						//todo log
					}
				}
				await Task.Delay(250);
			} while (true);
		}


	}
}
