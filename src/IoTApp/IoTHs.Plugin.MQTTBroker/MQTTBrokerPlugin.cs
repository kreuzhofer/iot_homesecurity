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
using IoTHs.Plugin.AzureIoTHub;
using System.Net.Http;
using IoTHs.Core.Lua;
using Newtonsoft.Json;

namespace IoTHs.Plugin.MQTTBroker
{
    public class MQTTBrokerPlugin : DeviceBase
    {
        private IEnumerable<IDeviceChannel> _channels;
        private IMqttServer _mqttServer;
        private string _name;
        private string _type;
        private List<string> _requestedFunctions = new List<string>();
        private readonly object _lockObj = new object();

        private CancellationTokenSource _threadCancellation;
        private Task _messageReceiverTask;
        private ILogger<MQTTBrokerPlugin> _log;

        public override string Name => _name;

        public override string Type => _type;

        public MQTTBrokerPlugin(ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<MQTTBrokerPlugin>();
        }

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            _name = configuration.Name;
            _type = configuration.Type;
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

        public override IEnumerable<IDeviceChannel> GetChannels()
        {
            return _channels.AsEnumerable();
        }

        public override async Task TeardownAsync()
        {
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
                    var iotHub = ServiceLocator.Current.GetService<IAzureIoTHubDevice>();
                    if (String.IsNullOrEmpty(iotHub.ServiceBaseUrl))
                    {
                        return;
                    }
                    var httpClient = new LocalHttpClient();
                    httpClient.Client.DefaultRequestHeaders.Add("apikey", iotHub.ApiKey);

                    string deviceScript = "";
                    bool deviceDetected = false;
                    // find out, if the message tells us, which device we have here
                    if (message.Topic.Contains("INFO1") && body.Contains("S20 Socket"))
                    {
                        deviceScript = @"
function run(message)
    if(string.match(message.Key, 'stat/POWER') != nil) then
        message.Tag = 'switch';
        queue.enqueue('iothub', message); --simply forward to iot hub message queue
    end;
    return 0;
    end;
";
                        var configuration = new
                        {
                            ChannelType = ChannelType.OnOffSwitch.ToString(),
                            OnOffTopic = rootTopic + "/cmnd/POWER",
                            OnMessage = "ON",
                            OffMessage = "OFF"
                        };
                        string configBody = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                        var task = httpClient.Client.PostAsync(iotHub.ServiceBaseUrl + "DeviceConfiguration/" + iotHub.DeviceId + "/" + _name + "/channel:" + rootTopic, new StringContent(configBody, Encoding.UTF8, "application/json"));
                        Task.WaitAll(task);
                        var result = task.Result;
                        deviceDetected = true;
                    }

                    if (deviceDetected) // only create a function if device was detected correctly
                    {
                        var model = new DeviceFunctionModel()
                        {
                            DeviceId = iotHub.DeviceId,
                            FunctionId = Guid.NewGuid().ToString(),
                            Name = rootTopic,
                            TriggerType = W10Home.Interfaces.Configuration.FunctionTriggerType.MessageQueue,
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
                if (queue.TryPeek(_name, out QueueMessage queuemessage))
                {
                    _mqttServer.Publish(new MqttApplicationMessage(queuemessage.Key, Encoding.UTF8.GetBytes(queuemessage.Value), MqttQualityOfServiceLevel.AtMostOnce, false));
                    queue.TryDeque(_name, out QueueMessage pop);
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
