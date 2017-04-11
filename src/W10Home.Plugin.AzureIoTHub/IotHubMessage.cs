namespace W10Home.Plugin.AzureIoTHub
{
	public class IotHubMessage
	{
		public string deviceId { get; set; }
		public string deviceType { get; set; }
		public string channelKey { get; set; }
		public string channelValue { get; set; }
		public string localtimestamp { get; set; }
	}
}