using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Entities
{
	public class DeviceStateEntity : TableEntity
	{
		public string channeltype { get; set; }
		public string channelkey { get; set; }
		public string channelvalue { get; set; }
		public string deviceid { get; set; }
		public string devicetype { get; set; }
		public string localtimestamp { get; set; }
	}
}