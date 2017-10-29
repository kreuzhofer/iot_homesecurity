using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace W10Home.IoTCoreApp
{
	internal class PacketForwardingWorker
	{
		private string _sourceHost;
		private string _sourcePort;
		private string _targetHost;
		private string _targetPort;

		public PacketForwardingWorker(string sourceHost, string sourcePort, string targetHost, string targetPort)
		{
			_sourceHost = sourceHost;
			_sourcePort = sourcePort;
			_targetHost = targetHost;
			_targetPort = targetPort;
		}

		public async Task RunAsync(CancellationToken token)
		{
			do
			{
				token.ThrowIfCancellationRequested();

				try
				{
					await RunAsyncInternal(token);
				}
				catch
				{
					// ignore
				}
				await Task.Delay(1000);
			} while (true);
		}

		private async Task RunAsyncInternal(CancellationToken token)
		{
			var sourceSocket = new StreamSocket();
			var sourceStreamReader = sourceSocket.InputStream.AsStreamForRead();
			var sourceStreamWriter = sourceSocket.OutputStream.AsStreamForWrite();
			await sourceSocket.ConnectAsync(new HostName(_sourceHost), _sourcePort);

			var targetSocket = new StreamSocket();
			var targetStreamReader = targetSocket.InputStream.AsStreamForRead();
			var targetStreamWriter = targetSocket.OutputStream.AsStreamForWrite();
			await targetSocket.ConnectAsync(new HostName(_targetHost), _targetPort);

			int packetSize = 8192;
			var buffer = new byte[packetSize];

			var task1 = Task.Factory.StartNew(() =>
			{
				do
				{
					token.ThrowIfCancellationRequested();

					var readBytes = sourceStreamReader.Read(buffer, 0, packetSize);
					if (readBytes > 0)
					{
						targetStreamWriter.Write(buffer, 0, readBytes);
						targetStreamWriter.Flush();
					}
				} while (true);
			}, token);
			var task2 = Task.Factory.StartNew(() =>
			{
				do
				{
					token.ThrowIfCancellationRequested();

					var readBytes = targetStreamReader.Read(buffer, 0, packetSize);
					if (readBytes > 0)
					{
						sourceStreamWriter.Write(buffer, 0, readBytes);
						sourceStreamWriter.Flush();
					}
				} while (true);
			}, token);
			Task.WaitAll(new[]{task1, task2}, token);
		}
	}
}