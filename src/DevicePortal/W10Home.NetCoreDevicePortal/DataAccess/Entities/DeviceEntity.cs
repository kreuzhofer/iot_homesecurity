using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Entities
{
    public class DeviceEntity : TableEntity
    {
        public string Name { get; set; }
        public string ApiKey { get; set; }
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}