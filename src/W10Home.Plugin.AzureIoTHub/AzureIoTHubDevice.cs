using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Provider;
using Windows.Storage;
using Windows.System;
using Windows.Web.Http;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using W10Home.Core.Configuration;
using W10Home.Core.Queing;
using W10Home.Interfaces;
using Microsoft.Devices.Tpm;
using Microsoft.Practices.ServiceLocation;
using W10Home.Core.Standard;
using W10Home.Interfaces.Configuration;

namespace W10Home.Plugin.AzureIoTHub
{
	public class AzureIoTHubDevice : DeviceBase
	{
		private DeviceClient _deviceClient;
		private string _deviceId;
		private string _deviceType = "RaspberryPiGateway_v1";

		private async void MessageReceiverLoop()
		{
			do
			{
				try
				{
					// check internal message queue for iot hub messages to be forwarded
					var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
					if (queue.TryPeek("iothub", out QueueMessage queuemessage))
					{
						if (await SendChannelMessageToIoTHubAsync(queuemessage.Key, queuemessage.Value, queuemessage.Tag))
						{
							queue.TryDeque("iothub", out QueueMessage pop);
						};
					}

					// check iot hub incoming messages for processing
					var message = await _deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(250));
					if (message != null)
					{
						await _deviceClient.CompleteAsync(message);
						var reader = new StreamReader(message.BodyStream);
						var bodyString = await reader.ReadToEndAsync();
						Debug.WriteLine(bodyString);
						dynamic messageObject = JsonConvert.DeserializeObject(bodyString); // try to deserialize into something json...
						if (messageObject == null) // maybe a json incompatible message object -> throw away and continue
						{
							continue;
						}
						string queueName = messageObject.queue;
						string key = messageObject.key;
						string value = messageObject.value;
						queue.Enqueue(queueName, key, value, "");
						//if (messageObject.Key == "configure")
						//{

						//}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					//TODO Log
				}
			} while (true);
		}

		public async Task<bool> SendLogMessageToIoTHubAsync(string severity, string message)
		{
			var messageObj = new IotHubLogMessage()
			{
				DeviceId = _deviceId,
				DeviceType = _deviceType,
				Severity = severity,
				Message = message,
				LocalTimestamp = $"{DateTime.Now.ToUniversalTime():O}"
			};
			return await SendMessageToIoTHubAsync(messageObj, "Log");
		}

		public async Task<bool> SendChannelMessageToIoTHubAsync(string key, object value, string channelType)
		{
			try
			{
				string strvalue = null;
				if (value is double || value is float)
				{
					strvalue = $"{((double)value):F}";
				}
				else if (value is string)
				{
					strvalue = value.ToString();
				}
				else
				{
					strvalue = $"{JsonConvert.SerializeObject(value)}";
				}

				var message = new IotHubChannelMessage()
				{
					DeviceId = _deviceId,
					DeviceType = _deviceType,
					ChannelType = channelType,
					ChannelKey = key,
					ChannelValue = strvalue,
					LocalTimestamp = $"{DateTime.Now.ToUniversalTime():O}"
				};

				return await SendMessageToIoTHubAsync(message, "Channel");
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				//TODO Log
				return false;
			}
		}

		private async Task<bool> SendMessageToIoTHubAsync(object message, string messageType)
		{
			try
			{
				var payload = JsonConvert.SerializeObject(message);

				var msg = new Message(Encoding.UTF8.GetBytes(payload));
				msg.Properties.Add("MessageType", messageType);

				await _deviceClient.SendEventAsync(msg);
				Debug.WriteLine(payload);
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				//TODO Log
				return false;
			}
		}

		public override async Task InitializeAsync(IDeviceConfiguration configuration)
		{
			try
			{
				if (configuration.Properties.ContainsKey("ConnectionString"))
				{
					var connectionString = configuration.Properties["ConnectionString"];
					_deviceId = connectionString.Split(';').Single(c => c.ToLower().StartsWith("deviceid")).Split('=')[1];
					// Instantiate the Azure IoT Hub device client
					_deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
				}
				else
				{
					// check tpm next
					TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM by default
					string hubUri = myDevice.GetHostName();
					_deviceId = myDevice.GetDeviceId();
					_deviceClient = DeviceClient.Create(
						hubUri,
						new AuthenticationProvider(),
						TransportType.Mqtt);
				}
				await SendLogMessageToIoTHubAsync("Info", "Device connection established");

				await _deviceClient.SetMethodHandlerAsync("configure", HandleConfigureMethod, null);
				await _deviceClient.SetDesiredPropertyUpdateCallback(DesiredPropertyUpdateCallback, null);

				if (configuration.Properties.ContainsKey("TryLoadConfiguration"))
				{
					var twin = await _deviceClient.GetTwinAsync(); // get configuration from server
					if (twin.Properties.Desired.Contains("configurationUrl"))
					{
						await DownloadConfigAndRestart(twin.Properties.Desired);
					}
				}

				MessageReceiverLoop(); // launch message loop in the background
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				//TODO Log
			}

		}

		private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
		{
			Debug.WriteLine(desiredProperties.ToString());
			if (desiredProperties.Contains("configurationUrl"))
			{
				await DownloadConfigAndRestart(desiredProperties);
			}
		}

		private async Task DownloadConfigAndRestart(TwinCollection desiredProperties)
		{
			// download file
			var fileToDownload = desiredProperties["configurationUrl"].ToString();
			var httpClient = new HttpClient();
			var configFileContent = await httpClient.GetStringAsync(new Uri(fileToDownload));

			// save to disk
			var localStorage = ApplicationData.Current.LocalFolder;
			var file = await localStorage.CreateFileAsync("configuration.json", CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(file, configFileContent);

			var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
			queue.Enqueue("management", "exit", null, null); // restart the app, StartupTask takes care of this
		}

		private class AuthenticationProvider : IAuthenticationMethod
		{
			public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
			{
				// check tpm next
				TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM by default
				string deviceId = myDevice.GetDeviceId();
				string sasToken = myDevice.GetSASToken();

				return AuthenticationMethodFactory.CreateAuthenticationWithToken(deviceId, sasToken).Populate(iotHubConnectionStringBuilder);
			}
		}

		private async Task<MethodResponse> HandleConfigureMethod(MethodRequest methodRequest, object userContext)
		{
			Debug.WriteLine("HandleConfigureMethod called");
			return new MethodResponse(0);
		}

		public override IEnumerable<IDeviceChannel> GetChannels()
		{
			throw new NotImplementedException();
		}

		public override async Task Teardown()
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
