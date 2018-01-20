using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Queing;
using IoTHs.Devices.Interfaces;
using MQTTnet;
using MQTTnet.Core;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Core.Adapter;
using IoTHs.Core.Http;
using System.Net.Http;
using IoTHs.Core.Authentication;
using IoTHs.Core.Lua;
using Newtonsoft.Json;

namespace IoTHs.Plugin.MQTTBroker
{
    public class MqttBrokerPlugin : PluginBase
    {
        private IMqttServer _mqttServer;
        private List<string> _requestedFunctions = new List<string>();
        private readonly object _lockObj = new object();

        private CancellationTokenSource _threadCancellation;
        private Task _messageReceiverTask;
        private ILogger<MqttBrokerPlugin> _log;
        private IApiAuthenticationService _apiAuthenticationService;

        public MqttBrokerPlugin(ILoggerFactory loggerFactory, IApiAuthenticationService apiAuthenticationService)
        {
            _log = loggerFactory.CreateLogger<MqttBrokerPlugin>();
            _apiAuthenticationService = apiAuthenticationService;
        }

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            await base.InitializeAsync(configuration);

            var channelConfigs = configuration.Properties.Select(c => c.Key.StartsWith("channel:")).ToList();

            _mqttServer = new MqttServerFactory().CreateMqttServer(new MqttServerOptions()
            {
                ConnectionValidator = ConnectionValidator
            });
            // https://github.com/chkr1011/MQTTnet/wiki/Server
            // https://github.com/Azure/DotNetty
            // https://github.com/i8beef/HomeAutio.Mqtt.Core
            await _mqttServer.StartAsync();
            _mqttServer.ApplicationMessageReceived += MqttServerOnApplicationMessageReceived;

            _threadCancellation = new CancellationTokenSource();
            _messageReceiverTask = MessageReceiverLoop(_threadCancellation.Token); // launch message loop in the background
        }

        public override async Task TeardownAsync()
        {
            await base.TeardownAsync();

            if (_threadCancellation != null)
            {
                _threadCancellation.Cancel();
            }
            if (_mqttServer != null)
            {
                await _mqttServer.StopAsync();
            }
        }

        private async void MqttServerOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs)
        {
            var message = mqttApplicationMessageReceivedEventArgs.ApplicationMessage;
            var body = Encoding.UTF8.GetString(message.Payload);
            var queue = ServiceLocator.Current.GetService<IMessageQueue>();
            var rootTopic = message.Topic.Split('/').First();
            // simple approach, forward to message queue by root topic
            queue.Enqueue(rootTopic, new QueueMessage(message.Topic, body, null));
            _log.LogTrace("{0}|{1}", message.Topic, body);

            var functionsEngine = ServiceLocator.Current.GetService<FunctionsEngine>();
            lock (_lockObj)
            {
                if (functionsEngine.Functions.All(f => f.Name != rootTopic) && _requestedFunctions.All(f => f != rootTopic))
                {
                    var iotHub = ServiceLocator.Current.GetService<IAzureIoTHubPlugin>();
                    if (String.IsNullOrEmpty(iotHub.ServiceBaseUrl))
                    {
                        return;
                    }

                    string deviceScript = "";
                    bool deviceDetected = false;
                    // find out, if the message tells us, which device we have here
                    if (message.Topic.Contains("INFO1") && body.Contains("S20 Socket"))
                    {
                        _log.LogInformation("No function found for topic " + rootTopic + ". Creating function.");
                        deviceScript =
@"function run(message)
    print('Key: '..message.Key);
    if(string.match(message.Key, 'stat/POWER') != nil) then
        print('Match!');
        print('Value: '..message.Value);
        message.Tag = 'onoffswitch';
        message.Key = string.gsub(message.Key, '/stat/POWER', '');
        queue.enqueue('iothub', message); --simply forward to iot hub message queue
    end;
    return 0;
end;";
                        var tokenTask = _apiAuthenticationService.GetTokenAsync();
                        Task.WaitAll(tokenTask);
                        var token = tokenTask.Result;
                        var httpClient = new LocalHttpClient(token);

                        var configuration = new
                        {
                            ChannelType = ChannelType.OnOffSwitch.ToString(),
                            OnOffTopic = rootTopic + "/cmnd/POWER",
                            OnMessage = "ON",
                            OffMessage = "OFF"
                        };
                        string configBody = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                        var task = httpClient.Client.PostAsync(iotHub.ServiceBaseUrl + "DeviceConfiguration/" + iotHub.DeviceId + "/" + Name + "/channel:" + rootTopic, new StringContent(configBody, Encoding.UTF8, "application/json"));
                        Task.WaitAll(task);
                        var result = task.Result;
                        if (!result.IsSuccessStatusCode)
                        {
                            _log.LogError("Error creating device configuration: "+result.ReasonPhrase);
                        }
                        deviceDetected = true;
                    }

                    if (deviceDetected) // only create a function if device was detected correctly. TODO create a setting for mqttbrokerplugin whether functions should be created automatically or not.
                    {
                        var tokenTask = _apiAuthenticationService.GetTokenAsync();
                        Task.WaitAll(tokenTask);
                        var token = tokenTask.Result;
                        var httpClient = new LocalHttpClient(token);

                        var model = new DeviceFunctionModel()
                        {
                            DeviceId = iotHub.DeviceId,
                            FunctionId = Guid.NewGuid().ToString(),
                            Name = rootTopic,
                            TriggerType = FunctionTriggerType.MessageQueue,
                            Interval = 0,
                            QueueName = rootTopic,
                            Enabled = true,
                            Script = deviceScript
                        };
                        string functionBody = JsonConvert.SerializeObject(model, Formatting.Indented);
                        var task = httpClient.Client.PostAsync(iotHub.ServiceBaseUrl + "DeviceFunction/" + iotHub.DeviceId + "/" + Guid.NewGuid().ToString(), new StringContent(functionBody, Encoding.UTF8, "application/json"));
                        Task.WaitAll(task);
                        var result = task.Result;
                        if (result.IsSuccessStatusCode)
                        {
                            _requestedFunctions.Add(rootTopic);
                        }
                        else
                        {
                            _log.LogError("Error creating device function: " + result.ReasonPhrase);
                        }
                    }
                }
            }
        }

        private MqttConnectReturnCode ConnectionValidator(MqttConnectPacket mqttConnectPacket)
        {
            //todo check credentials here
            return MqttConnectReturnCode.ConnectionAccepted;
        }


        private async Task MessageReceiverLoop(CancellationToken cancellationToken)
        {
            do
            {
                // check internal message queue for iot hub messages to be forwarded
                var queue = ServiceLocator.Current.GetService<IMessageQueue>();
                if (queue.TryPeek(Name, out QueueMessage queuemessage))
                {
                    if (_mqttServer.GetConnectedClients().Any()) // if no client is connected, do not try to send a message
                    {
                        _mqttServer.Publish(new MqttApplicationMessage(queuemessage.Key,
                            Encoding.UTF8.GetBytes(queuemessage.Value), MqttQualityOfServiceLevel.AtMostOnce, false));
                        queue.TryDeque(Name, out QueueMessage pop);
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
        }
    }
}
