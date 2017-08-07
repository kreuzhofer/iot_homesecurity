using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.Models
{
    public class DeviceEntity : TableEntity
    {
        public string eMail { get; set; } 
    }
}