using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
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
using W10Home.Core.Queing;
using W10Home.Interfaces;
#if USE_TPM
using Microsoft.Devices.Tpm;
#endif
using Microsoft.Practices.ServiceLocation;
using W10Home.Core.Standard;
using W10Home.Interfaces.Configuration;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using W10Home.Core.Channels;

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
	    private const int CLIENT_TIMEOUT = 59; // timeout in minutes before the iot hub client gets renewed (max 60 minutes)
	    private readonly ILogger _log = LogManagerFactory.DefaultLogManager.GetLogger<AzureIoTHubDevice>();
	    private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
	    private IDeviceRegistry _deviceRegistry;
	    private string _name;
	    private string _type;
	    private AutoResetEvent _messageLoopTerminationEvent;
	    private Task _messageReceiverTask;

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

                // cache last value
			    ServiceLocator.Current.GetInstance<ChannelValueCache>().Set(key, strvalue);

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
			    if (!IsInternetConnected())
			    {
			        return false;
			    }

				var payload = JsonConvert.SerializeObject(message, Formatting.Indented);

				var msg = new Message(Encoding.UTF8.GetBytes(payload));
				msg.Properties.Add("MessageType", messageType);

				await _deviceClient.SendEventAsync(msg);
				_log.Trace(_name + ":"+payload);
				return true;
			}
			catch (Exception ex)
			{
                _log.Error("SendMessageToIoTHubAsync", ex);
				return false;
			}
		}


	    public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
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


                await StartupAsync();

			    if (configuration.Properties.ContainsKey("TryLoadConfiguration"))
				{
					var twin = await _deviceClient.GetTwinAsync(); // get device twin from server
					if (twin.Properties.Desired.Contains("configurationUrl"))
					{
						await DownloadConfigAndRestart(twin.Properties.Desired);
					}
				}

                // send package version to iot hub for tracking device software version
			    var package = Windows.ApplicationModel.Package.Current;
			    var packageId = package.Id;
			    var version = packageId.Version;
			    await SendChannelMessageToIoTHubAsync("packageversion", version, ChannelType.None.ToString());
			}
			catch (Exception ex)
			{
                _log.Error("InitializeAsync", ex);
			}
		}

	    private async Task StartupAsync()
	    {
            _log.Trace("StartupAsync");
	        await CreateDeviceClientAsync();

	        _threadCancellation = new CancellationTokenSource();
	        _messageReceiverTask = MessageReceiverLoop(_threadCancellation.Token); // launch message loop in the background
        }

        private async Task CreateDeviceClientAsync()
	    {
            _log.Trace("CreateDeviceClientAsync");

#if USE_TPM
// check tpm next
			TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM by default
			string hubUri = myDevice.GetHostName();
			_deviceId = myDevice.GetDeviceId();
#else
#if USE_LIMPET
	        _connectionString = await GetConnectionStringFromTpmAsync();
	        _deviceId = _connectionString.Split(';').Single(c => c.ToLower().StartsWith("deviceid")).Split('=')[1];
#endif
#endif

#if USE_TPM
                _deviceClient = DeviceClient.Create(
						hubUri,
						new AuthenticationProvider(),
						TransportType.Mqtt);
#else
            // Instantiate the Azure IoT Hub device client
            _deviceClient = DeviceClient.CreateFromConnectionString(_connectionString, TransportType.Mqtt);
#endif
	        _log.Trace("Device connection established");

	        await _deviceClient.SetMethodHandlerAsync("getdevices", HandleGetDevicesMethod, null);
	        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, null);

	        _clientTimeoutTimer = new Timer(ClientTimeoutTimerCallback, null, TimeSpan.FromMinutes(CLIENT_TIMEOUT), TimeSpan.Zero);
        }

	    private async Task MessageReceiverLoop(CancellationToken cancellationToken)
	    {
            _messageLoopTerminationEvent = new AutoResetEvent(false);
	        do
	        {
                if (IsInternetConnected())
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
                        Message message = null;
                        try
                        {
                            message = await _deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(250));
                        }
                        catch (TaskCanceledException)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                        }

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
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error("MessageReceiverLoop", ex);
                    } 
                }
	            if (!cancellationToken.IsCancellationRequested)
	            {
	                await Task.Delay(1, cancellationToken);
	            }
	        } while (!cancellationToken.IsCancellationRequested);
            _log.Trace("Exit MessageReceiverLoop");
	        _messageLoopTerminationEvent.Set();
	    }

        private async void ClientTimeoutTimerCallback(object state)
        {
            try
            {
                _log.Trace("Recreating device client");
                await TeardownAsync();
                await StartupAsync();
            }
            catch(Exception ex)
            {
                _log.Error("ClientTimeoutTimerCallback|Error while recreating IoTHub client", ex);
            }
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
		        string functionsAndVersions = desiredProperties["functions"].versions.ToString();
		        var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
		        queue.Enqueue("functionsengine", "checkversionsandupdate", functionsAndVersions);
            }
		}

		private async Task DownloadConfigAndRestart(TwinCollection desiredProperties)
		{
            // create http base protocol filter to be able to download from untrusted https address in internal network
		    var aHBPF = new HttpBaseProtocolFilter();
		    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
		    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
		    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            // download file
            var baseUri = desiredProperties["configurationUrl"].ToString();
		    var configUri = baseUri + "api/DeviceConfiguration/" + _deviceId;
			var httpClient = new HttpClient(aHBPF);
			var configFileContent = await httpClient.GetStringAsync(new Uri(configUri));

			// save config file to disk
			var localStorage = ApplicationData.Current.LocalFolder;
			var file = await localStorage.CreateFileAsync("configuration.json", CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(file, configFileContent);

            // deserialize configuration object and download functions to seperate files
		    var configuration = JsonConvert.DeserializeObject<DeviceConfigurationModel>(configFileContent);
		    foreach (var functionId in configuration.DeviceFunctionIds)
		    {
		        var functionUri = baseUri + "api/DeviceFunction/" + _deviceId + "/" + functionId;
                httpClient = new HttpClient(aHBPF);
		        var functionContent = await httpClient.GetStringAsync(new Uri(functionUri));
                // store function file to disk
		        file = await localStorage.CreateFileAsync("function_"+functionId+".json", CreationCollisionOption.ReplaceExisting);
		        await FileIO.WriteTextAsync(file, functionContent);
            }

			var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
			queue.Enqueue("management", "exit", null, null); // restart the app, StartupTask takes care of this. External check to restart the app must be in place.
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
            _log.Trace("TeardownAsync");
		    if (_threadCancellation != null)
		    {
		        _threadCancellation.Cancel();
		        _messageLoopTerminationEvent?.WaitOne(2000);
		        _messageReceiverTask.Wait();
		        _messageReceiverTask = null;
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

	    private bool IsInternetConnected()
	    {
	        ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
	        bool internet = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
	        return internet;
	    }
    }
}
