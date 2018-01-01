using System.Collections.Generic;
using System.Text;
using IoTHs.Devices.Interfaces;
using MQTTnet.Core;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;

namespace IoTHs.Plugin.MQTTBroker
{
    public class SonoffS20Channel : IDeviceChannel
    {
        private string _name;
        private ChannelType _channelType;
        private IMqttServer _mqttServer;
        private string _topic;
        private bool _lastState;

        public SonoffS20Channel(IMqttServer mqttServer, string topic)
        {
            _mqttServer = mqttServer;
           _name = topic;
        }

        public string Name => _name;

        public bool IsRead => true;

        public bool IsWrite => true;

        public ChannelType ChannelType => ChannelType.OnOffSwitch;

        public object Read()
        {
            return _lastState;
        }

        public void Write(object value)
        {
            bool onOff = value != null && (bool)value;
            _mqttServer.Publish(new MqttApplicationMessage(_name + "/cmnd/POWER", Encoding.UTF8.GetBytes(onOff ? "ON" : "OFF"), MqttQualityOfServiceLevel.AtMostOnce, false));
            _lastState = onOff;
        }

        public IEnumerable<IChannelDatapoint> Datapoints { get; }
    }
}