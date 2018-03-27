using IoTHs.Api.Shared;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace W10Home.NetCoreDevicePortal.Models
{
    public class LogMessageTableEntitiy : TableEntity
    {
        public string DeviceId { get; set; }
        public string LocalTimestamp { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }

        public LogMessageTableEntitiy(LogMessage logMessage)
        {
            DeviceId = logMessage.DeviceId;
            LocalTimestamp = logMessage.LocalTimestamp;
            Severity = logMessage.Severity;
            Message = logMessage.Message;
            Source = logMessage.Source;

            // TableEntity fields
            PartitionKey = logMessage.Source;
            RowKey = logMessage.LocalTimestamp;
        }
    }
}
