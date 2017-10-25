using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Devices.Interfaces;
using MQTTnet;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;

namespace IoTHs.Plugin.MQTTBroker
{
    public class MQTTBrokerPlugin : DeviceBase
    {
        private IEnumerable<IDeviceChannel> _channels;
        private IMqttServer _mqttServer;
        private string _name;
        private string _type;

        public override string Name => _name;

        public override string Type => _type;

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            _mqttServer = new MqttServerFactory().CreateMqttServer(new MqttServerOptions()
            {
                ConnectionValidator = ConnectionValidator
            });
            await _mqttServer.StartAsync();
            _mqttServer.ApplicationMessageReceived += MqttServerOnApplicationMessageReceived;
        }

        public override IEnumerable<IDeviceChannel> GetChannels()
        {
            return _channels.AsEnumerable();
        }

        public override async Task TeardownAsync()
        {
            if (_mqttServer != null)
            {
                await _mqttServer.StopAsync();
            }
        }

        private void MqttServerOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs)
        {
            //todo handle messages here
        }

        private MqttConnectReturnCode ConnectionValidator(MqttConnectPacket mqttConnectPacket)
        {
            //todo check credentials here
            return MqttConnectReturnCode.ConnectionAccepted;
        }
    }
}
