using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.DevicePortal.Models
{
	public class DeviceConfigurationEntity : TableEntity
	{
		public string Configuration { get; set; }
	}
}