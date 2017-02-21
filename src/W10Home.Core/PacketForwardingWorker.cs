using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace W10Home.Core
{
	public class PacketForwardingWorker
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
			var sourceSocket = new StreamSocket();
			var sourceStreamReader = new DataReader(sourceSocket.InputStream);
			sourceStreamReader.InputStreamOptions = InputStreamOptions.Partial;
			var sourceStreamWriter = new DataWriter(sourceSocket.OutputStream);
			await sourceSocket.ConnectAsync(new HostName(_sourceHost), _sourcePort);

			var targetSocket = new StreamSocket();
			var targetStreamReader = new DataReader(targetSocket.InputStream);
			targetStreamReader.InputStreamOptions = InputStreamOptions.Partial;
			var targetStreamWriter = new DataWriter(targetSocket.OutputStream);
			await targetSocket.ConnectAsync(new HostName(_targetHost), _targetPort);

			uint packetSize = 1024;

			do
			{
				token.ThrowIfCancellationRequested();

				var readBytes = await sourceStreamReader.LoadAsync(packetSize);
				if (readBytes > 0)
				{
					var buffer = new byte[readBytes];
					sourceStreamReader.ReadBytes(buffer);
					targetStreamWriter.WriteBytes(buffer);
					await targetStreamWriter.StoreAsync();
				}
				readBytes = await targetStreamReader.LoadAsync(packetSize);
				if (readBytes > 0)
				{
					var buffer = new byte[readBytes];
					targetStreamReader.ReadBytes(buffer);
					sourceStreamWriter.WriteBytes(buffer);
					await sourceStreamWriter.StoreAsync();
				}
			} while (true);
		}
	}
}