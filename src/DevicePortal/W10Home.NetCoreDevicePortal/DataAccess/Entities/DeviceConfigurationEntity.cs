using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Entities
{
	public class DeviceConfigurationEntity : TableEntity
	{
		public string Configuration { get; set; }
	}
}