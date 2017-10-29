using System;
using System.Collections.Generic;
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

namespace IoTHs.Plugin.MQTTBroker
{
    public class MQTTBrokerPlugin : DeviceBase
    {
        private IEnumerable<IDeviceChannel> _channels;
        private IMqttServer _mqttServer;
        private string _name;
        private string _type;

        private CancellationTokenSource _threadCancellation;
        private Task _messageReceiverTask;

        public override string Name => _name;

        public override string Type => _type;

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            _name = configuration.Name;
            _type = configuration.Type;

            _mqttServer = new MqttServerFactory().CreateMqttServer(new MqttServerOptions()
            {
                ConnectionValidator = ConnectionValidator
            });
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

        private void MqttServerOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs)
        {
            var message = mqttApplicationMessageReceivedEventArgs.ApplicationMessage;
            var body = Encoding.UTF8.GetString(message.Payload);
            // simple approach, forward to message queue by client id
            var queue = ServiceLocator.Current.GetService<IMessageQueue>();
            queue.Enqueue(mqttApplicationMessageReceivedEventArgs.ClientId, new QueueMessage(message.Topic, body, null));

            var functionEngine = ServiceLocator.Current.GetService<IFunctionsEngine>(); // todo refactor interface out
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
                    _mqttServer.Publish(new MqttApplicationMessage(queuemessage.Key, Encoding.UTF8.GetBytes(queuemessage.Value), MqttQualityOfServiceLevel.ExactlyOnce, false));
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
