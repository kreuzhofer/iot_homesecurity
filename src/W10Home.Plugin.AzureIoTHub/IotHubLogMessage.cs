namespace W10Home.Plugin.AzureIoTHub
{
	public class IotHubLogMessage
	{
		public string MessageType = "Log";
		public string DeviceId { get; set; }
		public string DeviceType { get; set; }
		public string LocalTimestamp { get; set; }
		public string Severity { get; set; }
		public string Message { get; set; }
	}
}