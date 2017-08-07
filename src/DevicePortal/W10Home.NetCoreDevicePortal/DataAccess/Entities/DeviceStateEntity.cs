using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Entities
{
	public class DeviceStateEntity : TableEntity
	{
		public object ChannelType { get; set; }
		public string ChannelKey { get; set; }
		public string ChannelValue { get; set; }
		public string DeviceId { get; set; }
		public string DeviceType { get; set; }
		public string LocalTimestamp { get; set; }
	}
}