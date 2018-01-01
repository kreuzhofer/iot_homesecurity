using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Core.Channels
{
	public abstract class NotificationChannel : IDeviceChannel
	{
		public ChannelType ChannelType => ChannelType.Notification;
		public abstract bool IsRead { get; }
		public abstract bool IsWrite { get; }
		public abstract string Name { get; }
		public abstract Task<bool> NotifyAsync(string messageBody);
		public abstract object Read();
		public abstract void Write(object value);
	    public abstract IEnumerable<IChannelDatapoint> Datapoints { get; }
	}
}
