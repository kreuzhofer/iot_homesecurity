using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Entities
{
    public class DeviceEntity : TableEntity
    {
        public string eMail { get; set; } 
    }
}