using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHs.Api.Shared;

namespace IoTHs.Devices.Interfaces
{
    public interface IAzureIoTHubPlugin : IPlugin
    {
        string ServiceBaseUrl { get; }
        string ApiKey { get; }
        string DeviceId { get; }
    }
}