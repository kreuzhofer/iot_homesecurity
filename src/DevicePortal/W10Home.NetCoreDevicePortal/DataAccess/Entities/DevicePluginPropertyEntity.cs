using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Entities
{
    public class DevicePluginPropertyEntity : TableEntity
    {
        public string Value { get; set; }
    }
}