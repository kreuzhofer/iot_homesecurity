using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Entities
{
    public class DevicePluginEntity : TableEntity
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Enabled { get; set; }
    }
}