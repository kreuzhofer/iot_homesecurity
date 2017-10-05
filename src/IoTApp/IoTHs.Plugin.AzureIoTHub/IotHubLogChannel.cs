using System;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using W10Home.Core.Queing;

namespace IoTHs.Plugin.AzureIoTHub
{
    public class IotHubLogChannel : IDeviceChannel
    {
        private IMessageQueue _messageQueue;

        public IotHubLogChannel(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        public string Name => "log";
        public bool IsRead => false;
        public bool IsWrite => true;
        public ChannelType ChannelType => ChannelType.Log;
        public object Read()
        {
            throw new System.NotImplementedException();
        }

        public void Write(object value)
        {
            if (value is QueueMessage)
            {
                _messageQueue.Enqueue("iothublog", value as QueueMessage);
            }
            else
            {
                throw new InvalidCastException("Expected Type QueueMessage in log channel.");
            }
        }
    }
}