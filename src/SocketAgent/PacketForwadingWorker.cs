using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace SocketAgent
{
	public class PacketForwadingWorker
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private const int buffersize = 8192;
		private byte[] byteBuffer = new byte[buffersize];
		private TcpListener _receiverServer;
		private Socket _receiverSocket;
		private TcpListener _senderServer;
		private Socket _senderSocket;

		public void Start(CancellationToken token)
		{
			_senderServer = new TcpListener(IPAddress.Any, 10000);
			_senderServer.Start();
			_receiverServer = new TcpListener(IPAddress.Any, 80);
			_receiverServer.Start();

			var receiverTask = WaitReceiverConnect();
			var senderTask = WaitSenderConnect();
			Task.WaitAll(receiverTask, senderTask); // wait for sender and receiver to connect

			var task1 = Task.Factory.StartNew(() =>
			{
				do
				{
					var size = _receiverSocket.Receive(byteBuffer);
					if (size > 0)
					{
						_senderSocket.Send(byteBuffer, size, SocketFlags.None);
					}
				} while (!token.IsCancellationRequested);
			});
			var task2 = Task.Factory.StartNew(() =>
			{
				do
				{
					var size = _senderSocket.Receive(byteBuffer);
					if (size > 0)
					{
						_receiverSocket.Send(byteBuffer, size, SocketFlags.None);
					}
				} while (!token.IsCancellationRequested);
			});
			Task.WaitAll(new[] {task1, task2}, token);
		}

		private async Task WaitReceiverConnect()
		{
			_receiverSocket = await _receiverServer.AcceptSocketAsync();
			logger.Debug("Receiver socket connected");
		}

		private async Task WaitSenderConnect()
		{
			_senderSocket = await _senderServer.AcceptSocketAsync();
			logger.Debug("Sender socket connected");
		}
	}
}