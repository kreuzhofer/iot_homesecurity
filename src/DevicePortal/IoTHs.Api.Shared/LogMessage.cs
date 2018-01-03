namespace IoTHs.Api.Shared
{
	public class LogMessage
	{
		public string DeviceId { get; set; }
		public string LocalTimestamp { get; set; }
		public string Severity { get; set; }
		public string Message { get; set; }
        public string Source { get; set; }
	}
}