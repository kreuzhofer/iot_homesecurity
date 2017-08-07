using System.Collections.Generic;
using System.Threading.Tasks;
using W10Home.NetCoreDevicePortal.Models;

namespace W10Home.NetCoreDevicePortal.DataAccess
{
    public interface IDeviceFunctionService
    {
        Task SaveFunctionAsync(string deviceId, string functionId, string functionName, string triggerType, int interval, string queueName, string scriptContent);
        Task<DeviceFunctionEntity> GetFunctionAsync(string deviceId, string functionId);
        Task<List<DeviceFunctionEntity>> GetFunctionsAsync(string deviceId);
        Task DeleteFunctionAsync(string deviceId, string functionId);
    }
}