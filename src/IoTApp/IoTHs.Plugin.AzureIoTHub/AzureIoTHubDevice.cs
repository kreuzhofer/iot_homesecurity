using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Channels;
using IoTHs.Core.Queing;
using IoTHs.Devices.Interfaces;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using NLog;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

#if USE_TPM
using Microsoft.Devices.Tpm;
#endif

namespace IoTHs.Plugin.AzureIoTHub
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
	    private readonly ILogger _log = LogManager.GetCurrentClassLogger();
	    private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
	    private IDeviceRegistry _deviceRegistry;
	    private string _name;
	    private string _type;
	    private AutoResetEvent _messageLoopTerminationEvent;
	    private Task _messageReceiverTask;
	    private string _serviceBaseUrl;
	    private string _apiKey;
	    private int _configVersion;

	    public override string Name
	    {
	        get { return _name; }
	    }

	    public override string Type
	    {
	        get { return _type; }
	    }

	    public string ServiceBaseUrl
	    {
            get { return _serviceBaseUrl; }
	    }

	    public string ApiKey
	    {
	        get { return _apiKey; }
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
		    if (String.IsNullOrEmpty(_serviceBaseUrl) || String.IsNullOrEmpty(_apiKey))
		    {
		        return false;
		    }
            try
            {
                var messageObj = new LogMessage()
                {
                    DeviceId = _deviceId,
                    Severity = severity,
                    Message = message,
                    LocalTimestamp = $"{DateTime.Now.ToUniversalTime():O}"
                };
                var aHBPF = new HttpBaseProtocolFilter();
                aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
                aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
                var client = new HttpClient(aHBPF);
                client.DefaultRequestHeaders.Add("apikey", _apiKey);
                var content = new HttpStringContent(JsonConvert.SerializeObject(messageObj, Formatting.Indented), UnicodeEncoding.Utf8, "application/json");
                var result = await client.PostAsync(new Uri(_serviceBaseUrl + "Logging/" + _deviceId), content);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while sending log message to server: "+ex.Message);
                return false;
            }
		    //return await SendMessageToIoTHubAsync(messageObj, "Log");
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
				_log.Error(ex, "SendChannelMessageToIoTHubAsync");
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
                _log.Error(ex, "SendMessageToIoTHubAsync");
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

			    var twin = await _deviceClient.GetTwinAsync(); // get device twin from server
			    if (twin.Properties.Desired.Contains("serviceBaseUrl"))
			    {
			        _serviceBaseUrl = twin.Properties.Desired["serviceBaseUrl"];
			    }
			    if (twin.Properties.Desired.Contains("apikey"))
			    {
			        _apiKey = twin.Properties.Desired["apikey"];
			    }
			    if (twin.Properties.Reported.Contains("ConfigVersion"))
			    {
			        _configVersion = twin.Properties.Reported["ConfigVersion"];
			    }
                if(twin.Properties.Desired.Version>_configVersion)
                {                
					await DownloadConfigAndRestartAsync(_serviceBaseUrl, _apiKey, twin.Properties.Desired.Version);
                }


                // send package version to iot hub for tracking device software version
			    var package = Windows.ApplicationModel.Package.Current;
			    var packageId = package.Id;
			    var version = packageId.Version;
			    await SendChannelMessageToIoTHubAsync("packageversion", $"{version.Major}.{version.Minor}.{version.Build}", ChannelType.None.ToString());
			}
			catch (Exception ex)
			{
                _log.Error(ex, "InitializeAsync");
			}
		}

	    private async Task StartupAsync()
	    {
            _log.Trace("StartupAsync");
	        await Observable.Defer(async () =>
	        {
	            await CreateDeviceClientAsync();
	            return Observable.Return("ok");
	        }).Retry(10);

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
	        await _deviceClient.SetMethodHandlerAsync("getdevices", HandleGetDevicesMethod, null);
	        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, null);
	        _log.Trace("Device connection established");


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
                                _log.Error(ex, "Could not deserialize message from iot hub");
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
                        _log.Error(ex, "MessageReceiverLoop");
                    } 
                }
	            if (!cancellationToken.IsCancellationRequested)
	            {
	                try
	                {
	                    await Task.Delay(IoTHsConstants.MessageLoopDelay, cancellationToken);
	                }
	                catch
	                {
	                    // gulp
	                }
	            }
	        } while (!cancellationToken.IsCancellationRequested);
            _log.Trace("Exit MessageReceiverLoop");
	        _messageLoopTerminationEvent.Set();
	    }

        private async void ClientTimeoutTimerCallback(object state)
        {
            _log.Trace("Recreating device client");
            try
            {
                await TeardownAsync();
            }
            catch(Exception ex)
            {
                _log.Error(ex, "ClientTimeoutTimerCallback|Error while shutting down IoTHub client and message loop");
            }
            try
            {
                await StartupAsync();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "ClientTimeoutTimerCallback|Error while recreating IoTHub client");
            }

        }

        private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
		{
		    _log.Trace(desiredProperties.ToString());
		    var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
		    if (desiredProperties.Contains("functions"))
		    {
		        string functionsAndVersions = desiredProperties["functions"].versions.ToString();
		        _log.Trace("DesiredPropertyUpdateCallback|Updating functions: "+functionsAndVersions);
		        queue.Enqueue("functionsengine", "checkversionsandupdate", functionsAndVersions);
            }
		    if (desiredProperties.Contains("apikey"))
		    {
		        _apiKey = desiredProperties["apikey"].ToString();
		    }
            if (desiredProperties.Contains("serviceBaseUrl"))
		    {
		        _serviceBaseUrl = desiredProperties["serviceBaseUrl"].ToString();
		        await DownloadConfigAndRestartAsync(_serviceBaseUrl, _apiKey, desiredProperties.Version);
		    }
        }

		private async Task DownloadConfigAndRestartAsync(string serviceBaseUrl, string apiKey, long configVersion)
		{
            // create http base protocol filter to be able to download from untrusted https address in internal network
		    var aHBPF = new HttpBaseProtocolFilter();
		    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
		    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
		    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            // download file
		    var configUri = serviceBaseUrl + "DeviceConfiguration/" + _deviceId;
		    _log.Debug("Downloading new configuration from " + configUri);
			var httpClient = new HttpClient(aHBPF);
            httpClient.DefaultRequestHeaders.Add("apikey", apiKey);
			var configFileContent = await httpClient.GetStringAsync(new Uri(configUri));

			// save config file to disk
			var localStorage = ApplicationData.Current.LocalFolder;
			var file = await localStorage.CreateFileAsync("configuration.json", CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(file, configFileContent);

            // deserialize configuration object and download functions to seperate files
		    var configuration = JsonConvert.DeserializeObject<DeviceConfigurationModel>(configFileContent);
		    foreach (var functionId in configuration.DeviceFunctionIds)
		    {
		        var functionUri = serviceBaseUrl + "DeviceFunction/" + _deviceId + "/" + functionId;
                httpClient = new HttpClient(aHBPF);
		        httpClient.DefaultRequestHeaders.Add("apikey", apiKey);
		        var functionContent = await httpClient.GetStringAsync(new Uri(functionUri));
                // store function file to disk
		        file = await localStorage.CreateFileAsync("function_"+functionId+".json", CreationCollisionOption.ReplaceExisting);
		        await FileIO.WriteTextAsync(file, functionContent);
            }

		    var reportedProperties = new TwinCollection();
		    reportedProperties["ConfigVersion"] = configVersion;
		    await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

            var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
			queue.Enqueue("management", "restart", null, null); // restart the app, StartupTask takes care of this. External check to restart the app must be in place.
		}


#if USE_LIMPET
        /// <summary>
        /// TPM access in release mode with .net native and Microsoft TPM library is not possible.
        /// See https://github.com/Azure/azure-iot-hub-vs-cs/issues/9 for more information why this workaround is needed
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetConnectionStringFromTpmAsync()
	    {
            try
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
            catch (System.UnauthorizedAccessException)
            {
                _log.Fatal("Could not launch limpet.exe. Permissions are missing. See https://github.com/Azure/azure-iot-hub-vs-cs/issues/9");
                throw;
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
                try
                {
                    _threadCancellation.Cancel();
                    _messageLoopTerminationEvent?.WaitOne(2000);
                    _messageReceiverTask.Wait();
                }
                finally
                {
                    _messageReceiverTask = null;
                    _threadCancellation = null;
                    _messageLoopTerminationEvent = null;

                }
		    }
		    if (_clientTimeoutTimer != null)
		    {
                try
                {
                    _clientTimeoutTimer.Dispose();
                }
                finally
                {
                    _clientTimeoutTimer = null;
                }

		    }
			if (_deviceClient != null)
			{
                try
                {
                    await _deviceClient.CloseAsync();
                    _deviceClient.Dispose();
                }
                finally
                {
                    _deviceClient = null;
                }
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
