using IoTHs.Api.Shared;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.Interfaces.Configuration;

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
        public bool Enabled { get; set; }

        public DeviceFunctionModel ToDeviceFunctionModel()
        {
            FunctionTriggerType enumTriggerType;
            FunctionTriggerType.TryParse(TriggerType, out enumTriggerType);
            return new DeviceFunctionModel()
            {
                DeviceId = PartitionKey,
                FunctionId = RowKey,
                Interval = Interval,
                Language = Language,
                Name = Name,
                QueueName = QueueName,
                Script = Script,
                TriggerType = enumTriggerType,
                Enabled = Enabled
            };
        }
    }
}