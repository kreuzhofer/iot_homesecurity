using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Provider;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using MetroLog;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using W10Home.Core.Configuration;
using W10Home.Core.Queing;
using W10Home.Interfaces;
#if USE_TPM
using Microsoft.Devices.Tpm;
#endif
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
	    private Timer _clientTimeoutTimer;
	    private CancellationTokenSource _threadCancellation;
	    private string _connectionString;
	    private const int CLIENT_TIMEOUT = 59;
	    private readonly ILogger _log = LogManagerFactory.DefaultLogManager.GetLogger<AzureIoTHubDevice>();
	    private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
	    private IDeviceRegistry _deviceRegistry;
	    private string _name;
	    private string _type;

	    public override string Name
	    {
	        get { return _name; }
	    }

	    public override string Type
	    {
	        get { return _type; }
	    }

        public AzureIoTHubDevice(IMessageQueue messageQueue, IDeviceRegistry deviceRegistry)
	    {
	        _channels.Add(new IotHubDeviceChannel(messageQueue));
            _channels.Add(new IotHubLogChannel(messageQueue));
	        _deviceRegistry = deviceRegistry;
	    }

#if USE_TPM
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
#endif

        private async Task<bool> SendLogMessageToIoTHubAsync(string severity, string message)
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

		private async Task<bool> SendChannelMessageToIoTHubAsync(string key, object value, string channelType)
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
				_log.Error("SendChannelMessageToIoTHubAsync", ex);
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
                _log.Error("SendMessageToIoTHubAsync", ex);
				return false;
			}
		}


	    public override async Task InitializeAsync(IDeviceConfiguration configuration)
	    {
	        _name = configuration.Name;
	        _type = configuration.Type;

			try
			{
				if (configuration.Properties.ContainsKey("ConnectionString"))
				{
					_connectionString = configuration.Properties["ConnectionString"];
					_deviceId = _connectionString.Split(';').Single(c => c.ToLower().StartsWith("deviceid")).Split('=')[1];
				}
#if USE_TPM
				else
				{
					// check tpm next
					TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM by default
					string hubUri = myDevice.GetHostName();
					_deviceId = myDevice.GetDeviceId();

				}
#endif
#if USE_LIMPET
				else
				{
				    _connectionString = await GetConnectionStringFromTpmAsync();
				    _deviceId = _connectionString.Split(';').Single(c => c.ToLower().StartsWith("deviceid")).Split('=')[1];
                }
#endif

			    await StartupAsync();

			    if (configuration.Properties.ContainsKey("TryLoadConfiguration"))
				{
					var twin = await _deviceClient.GetTwinAsync(); // get device twin from server
					if (twin.Properties.Desired.Contains("configurationUrl"))
					{
						await DownloadConfigAndRestart(twin.Properties.Desired);
					}
				}
			}
			catch (Exception ex)
			{
                _log.Error("InitializeAsync", ex);
			}
		}

	    private async Task StartupAsync()
	    {
	        await CreateDeviceClientAsync();

	        _threadCancellation = new CancellationTokenSource();
	        MessageReceiverLoop(_threadCancellation.Token); // launch message loop in the background
        }

        private async Task CreateDeviceClientAsync()
	    {
#if USE_TPM
                _deviceClient = DeviceClient.Create(
						hubUri,
						new AuthenticationProvider(),
						TransportType.Mqtt);
#else
	        // Instantiate the Azure IoT Hub device client
	        _deviceClient = DeviceClient.CreateFromConnectionString(_connectionString, TransportType.Mqtt);
#endif

	        await SendLogMessageToIoTHubAsync("Info", "Device connection established");

	        await _deviceClient.SetMethodHandlerAsync("configure", HandleConfigureMethod, null);
	        await _deviceClient.SetMethodHandlerAsync("getdevices", HandleGetDevicesMethod, null);
	        await _deviceClient.SetDesiredPropertyUpdateCallback(DesiredPropertyUpdateCallback, null);

	        _clientTimeoutTimer = new Timer(ClientTimeoutTimerCallback, null, TimeSpan.FromMinutes(CLIENT_TIMEOUT), TimeSpan.Zero);
        }

	    private async void MessageReceiverLoop(CancellationToken cancellationToken)
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
	                if (queue.TryPeek("iothublog", out QueueMessage logmessage))
	                {
	                    if (await SendLogMessageToIoTHubAsync(logmessage.Key, logmessage.Value))
	                    {
	                        queue.TryDeque("iothublog", out QueueMessage pop);
	                    };
	                }

                    // check iot hub incoming messages for processing
	                if (cancellationToken.IsCancellationRequested)
	                {
	                    break;
	                }
                    var message = await _deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(250));
	                if (message != null)
	                {
	                    await _deviceClient.CompleteAsync(message);
	                    var reader = new StreamReader(message.BodyStream);
	                    var bodyString = await reader.ReadToEndAsync();
	                    Debug.WriteLine(bodyString);
	                    dynamic messageObject = null;
	                    try
	                    {
	                        messageObject =
	                            JsonConvert.DeserializeObject(bodyString); // try to deserialize into something json...
	                    }
	                    catch (Exception ex)
	                    {
	                        _log.Error("Could not deserialize message from iot hub", ex);
	                    }
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
                    _log.Error("MessageReceiverLoop", ex);
	            }
	            if (!cancellationToken.IsCancellationRequested)
	            {
	                await Task.Delay(1, cancellationToken);
	            }
	        } while (!cancellationToken.IsCancellationRequested);
            _log.Trace("Exit MessageReceiverLoop");
	    }

        private async void ClientTimeoutTimerCallback(object state)
        {
            _log.Trace("Recreating device client");
            await TeardownAsync();
            await StartupAsync();
        }

	    private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
		{
			Debug.WriteLine(desiredProperties.ToString());
			if (desiredProperties.Contains("configurationUrl"))
			{
				await DownloadConfigAndRestart(desiredProperties);
			}
		    if (desiredProperties.Contains("functions"))
		    {
		        if (desiredProperties["functions"].loadFunction != null)
		        {
                    // download function code from webserver
		            var aHBPF = new HttpBaseProtocolFilter();
		            // I purposefully have an expired cert to show setting multiple Ignorable Errors
		            aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
		            // Untrused because this is a self signed cert that is not installed
		            aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
		            aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
                    var client =
		                await new HttpClient(aHBPF).GetStringAsync(
		                    new Uri("https://192.168.178.38:45455/api/DeviceFunction/homecontroller/" +
		                            desiredProperties["functions"].loadFunction));
		            dynamic obj = JsonConvert.DeserializeObject(client);

                    // todo refresh function after loading it from server

		            var reportedProperties = new TwinCollection
		            {
		                ["functions"] = new
		                {
		                    loadFunction = desiredProperties["functions"].loadFunction,
                            status = "success"
                        }
		            };
		            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
		        }
            }
		}

		private async Task DownloadConfigAndRestart(TwinCollection desiredProperties)
		{
			// download file
			var baseUri = desiredProperties["configurationUrl"].ToString();
		    var configUri = baseUri + "api/DeviceConfiguration/" + _deviceId;
			var httpClient = new HttpClient();
			var configFileContent = await httpClient.GetStringAsync(new Uri(configUri));

			// save config file to disk
			var localStorage = ApplicationData.Current.LocalFolder;
			var file = await localStorage.CreateFileAsync("configuration.json", CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(file, configFileContent);

			var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
			queue.Enqueue("management", "reboot", null, null); // restart the device, StartupTask takes care of this
		}


#if USE_LIMPET
        /// <summary>
        /// TPM access in release mode with .net native and Microsoft TPM library is not possible.
        /// See https://github.com/Azure/azure-iot-hub-vs-cs/issues/9 for more information why this workaround is needed
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetConnectionStringFromTpmAsync()
	    {
	        var processLauncherOptions = new ProcessLauncherOptions();
	        var standardOutput = new InMemoryRandomAccessStream();

	        processLauncherOptions.StandardOutput = standardOutput;
	        processLauncherOptions.StandardError = null;
	        processLauncherOptions.StandardInput = null;

	        var processLauncherResult = await ProcessLauncher.RunToCompletionAsync(@"c:\windows\system32\limpet.exe", "0 -ast", processLauncherOptions);
	        if (processLauncherResult.ExitCode == 0)
	        {
	            using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
	            {
	                var size = standardOutput.Size;
	                using (var dataReader = new DataReader(outStreamRedirect))
	                {
	                    var bytesLoaded = await dataReader.LoadAsync((uint)size);
	                    var stringRead = dataReader.ReadString(bytesLoaded);
	                    var result = stringRead.Trim();
	                    return result;
	                }
	            }
	        }
	        else
	        {
                _log.Error("GetConnectionStringFromTpmAsync. Cannot get connection string");
	            throw new Exception("Cannot get connection string");
	        }
	    }
#endif

#region DeviceMethods
        private async Task<MethodResponse> HandleConfigureMethod(MethodRequest methodRequest, object userContext)
		{
			Debug.WriteLine("HandleConfigureMethod called");
			return new MethodResponse(0);
		}

	    private async Task<MethodResponse> HandleGetDevicesMethod(MethodRequest methodrequest, object usercontext)
	    {
            var json = JsonConvert.SerializeObject(_deviceRegistry.GetDevices().ToList(), Formatting.Indented);
	        var bytes = Encoding.UTF8.GetBytes(json);
	        return new MethodResponse(bytes, 0);
	    }
#endregion



        public override IEnumerable<IDeviceChannel> GetChannels()
		{
		    return _channels.AsEnumerable();
		}

		public override async Task TeardownAsync()
		{
		    if (_threadCancellation != null)
		    {
		        _threadCancellation.Cancel();
		        _threadCancellation = null;
		    }
		    if (_clientTimeoutTimer != null)
		    {
		        _clientTimeoutTimer.Dispose();
		        _clientTimeoutTimer = null;
		    }
			if (_deviceClient != null)
			{
				await _deviceClient.CloseAsync();
				_deviceClient.Dispose();
				_deviceClient = null;
			}
		}
	}
}
