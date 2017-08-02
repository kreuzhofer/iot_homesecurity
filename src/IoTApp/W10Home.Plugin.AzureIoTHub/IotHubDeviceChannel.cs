using System;
using W10Home.Core.Queing;
using W10Home.Interfaces;

namespace W10Home.Plugin.AzureIoTHub
{
    public class IotHubDeviceChannel : IDeviceChannel
    {
        private IMessageQueue _messageQueue;

        public IotHubDeviceChannel(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        public string Name => "device";
        public bool IsRead => false;
        public bool IsWrite => true;
        public ChannelType ChannelType => ChannelType.Message;
        public object Read()
        {
            throw new System.NotImplementedException();
        }

        public void Write(object value)
        {
            if (value is QueueMessage)
            {
                _messageQueue.Enqueue("iothub", value as QueueMessage);
            }
            else
            {
                throw new InvalidCastException("Expected Type QueueMessage in device channel.");
            }
        }
    }
}