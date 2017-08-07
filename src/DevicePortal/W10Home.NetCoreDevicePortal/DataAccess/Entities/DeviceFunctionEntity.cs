using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Entities
{
    public class DeviceFunctionEntity : TableEntity
    {
        public string Name { get; set; }
        public string TriggerType { get; set; }
        public string QueueName { get; set; }
        public int Interval { get; set; }
        public string Script { get; set; }
        public string Language { get; set; }
    }
}