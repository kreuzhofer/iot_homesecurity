using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using W10Home.Core.Configuration;
using W10Home.Core.Queing;
using W10Home.Interfaces;
using Microsoft.Devices.Tpm;
using Microsoft.Practices.ServiceLocation;

namespace W10Home.Plugin.AzureIoTHub
{
	public class AzureIoTHubDevice : IDevice
	{
		private DeviceClient _deviceClient;
		private string _deviceId;

		private async void MessageReceiverLoop()
		{
			do
			{
				try
				{
					// check internal message queue for iot hub messages to be forwarded
					var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
					if (queue.TryDeque("iothub", out QueueMessage queuemessage))
					{
						await SendMessageToIoTHubAsync(queuemessage.Key, queuemessage.Value);
					}

					// check iot hub incoming messages for processing
					var message = await _deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(250));
					if (message != null)
					{
						await _deviceClient.CompleteAsync(message);
						var reader = new StreamReader(message.BodyStream);
						var bodyString = await reader.ReadToEndAsync();
						Debug.WriteLine(bodyString);
						var messageObject = JsonConvert.DeserializeObject<QueueMessage>(bodyString);
						if (messageObject == null) // maybe an incompatible message object -> throw away and continue
						{
							continue;
						}
						if (messageObject.Key == "configure")
						{

						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					//TODO Log
				}
			} while (true);
		}

		public async Task SendMessageToIoTHubAsync(string key, object value)
		{
			try
			{
				string strvalue = null;
				if (value is double || value is float)
				{
					strvalue = $"{((double)value):F}";
				}
				else
				{
					strvalue = $"\"{value.ToString()}\"";
				}

				var payload =
					$"{{\"deviceid\": \"{_deviceId}\", \"channelvalue\": {strvalue}, \"channelkey\": \"{key}\", \"localtimestamp\": \"{DateTime.Now.ToUniversalTime():O}\"}}";

				var msg = new Message(Encoding.UTF8.GetBytes(payload));

				await _deviceClient.SendEventAsync(msg);
				Debug.WriteLine(payload);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				//TODO Log
			}
		}

		public async Task InitializeAsync(IDeviceConfiguration configuration)
		{
			try
			{
				if (configuration.Properties.ContainsKey("ConnectionString"))
				{
					var connectionString = configuration.Properties["ConnectionString"];
					_deviceId = connectionString.Split(';').Single(c => c.ToLower().StartsWith("deviceid")).Split('=')[0];
					// Instantiate the Azure IoT Hub device client
					_deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
				}
				else
				{
					// check tpm next
					TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM by default
					string hubUri = myDevice.GetHostName();
					string deviceId = myDevice.GetDeviceId();
					string sasToken = myDevice.GetSASToken();
					_deviceClient = DeviceClient.Create(
						hubUri,
						AuthenticationMethodFactory.
							CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Mqtt);
				}
				await _deviceClient.SetMethodHandlerAsync("configure", HandleConfigureMethod, null);

				MessageReceiverLoop(); // launch message loop in the background
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				//TODO Log
			}

		}

		private async Task<MethodResponse> HandleConfigureMethod(MethodRequest methodRequest, object userContext)
		{
			Debug.WriteLine("HandleConfigureMethod called");
			return new MethodResponse(0);
		}

		public Task<IEnumerable<IChannel>> GetChannelsAsync()
		{
			throw new NotImplementedException();
		}

		public async Task Teardown()
		{
			if (_deviceClient != null)
			{
				await _deviceClient.CloseAsync();
				_deviceClient.Dispose();
				_deviceClient = null;
			}
		}
	}
}
