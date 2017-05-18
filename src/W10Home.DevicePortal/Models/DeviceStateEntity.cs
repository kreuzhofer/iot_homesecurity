using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace W10Home.DevicePortal.Models
{
	public class DeviceStateEntity : TableEntity
	{
		public string channeltype { get; set; }
		public string channelkey { get; set; }
		public string channelvalue { get; set; }
		public string deviceid { get; set; }
		public string devicetype { get; set; }
		public DateTime localtimestamp { get; set; }
	}
}