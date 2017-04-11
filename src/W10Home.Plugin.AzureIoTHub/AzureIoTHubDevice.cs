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
						if (await SendMessageToIoTHubAsync(queuemessage.Key, queuemessage.Value))
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

		private async Task<bool> SendMessageToIoTHubAsync(string key, object value)
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

				var message = new IotHubMessage()
				{
					deviceId = _deviceId,
					deviceType = "tbd",
					channelKey = key,
					channelValue = strvalue,
					localtimestamp = $"{DateTime.Now.ToUniversalTime():O}"
				};

				var payload = JsonConvert.SerializeObject(message);

				var msg = new Message(Encoding.UTF8.GetBytes(payload));

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
				await _deviceClient.SetMethodHandlerAsync("configure", HandleConfigureMethod, null);
				await _deviceClient.SetDesiredPropertyUpdateCallback(DesiredPropertyUpdateCallback, null);

				if (configuration.Properties.ContainsKey("TryLoadConfiguration"))
				{
					var twin = await _deviceClient.GetTwinAsync(); // get configuration from server
					if (twin.Properties.Desired.Contains("configurationUrl"))
					{
						await DownloadConfigAndReboot(twin.Properties.Desired);
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
				await DownloadConfigAndReboot(desiredProperties);
			}
		}

		private async Task DownloadConfigAndReboot(TwinCollection desiredProperties)
		{
// download file
			var fileToDownload = desiredProperties["configurationUrl"].ToString();
			var httpClient = new HttpClient();
			var configFileContent = await httpClient.GetStringAsync(new Uri(fileToDownload));

			// save to disk
			var localStorage = ApplicationData.Current.LocalFolder;
			var file = await localStorage.CreateFileAsync("configuration.json", CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(file, configFileContent);

			ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.Zero);
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
