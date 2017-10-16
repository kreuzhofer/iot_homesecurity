namespace IoTHs.Plugin.AzureIoTHub
{
	public class IotHubChannelMessage
	{
		public string MessageType = "Channel";
		public string DeviceId { get; set; }
		public string DeviceType { get; set; }
		public string ChannelType { get; set; }
		public string ChannelKey { get; set; }
		public string ChannelValue { get; set; }
		public string LocalTimestamp { get; set; }
	}
}