using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace SocketAgent
{
	public class SocketService
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private CancellationTokenSource cts;
		private int _encodingThreads;

		public SocketService(int encodingThreads)
		{
			_encodingThreads = encodingThreads;
		}

		public void Start()
		{
			logger.Debug($"Starting worker...");
			var myThread = new Thread(new ThreadStart(Run)) { IsBackground = true };
			myThread.Start();
		}


		public void Run()
		{
			cts = new CancellationTokenSource();
			RunAsync(cts.Token).Wait(cts.Token);
		}

		public async Task RunAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				logger.Debug($"Starting workers...");
				var workersList = new List<Task>();
				for (int i = 0; i < _encodingThreads; i++)
				{
					var task = Task.Factory.StartNew(() => { new PacketForwadingWorker().Start(cancellationToken); }, cancellationToken);
					workersList.Add(task);
				}
				Task.WaitAll(workersList.ToArray());
			}
			logger.Debug($"Stopped");
		}

		public void Stop()
		{
			logger.Debug($"Stopping");
			this.cts.Cancel();
			logger.Debug($"Cancellation sent");
		}
	}
}