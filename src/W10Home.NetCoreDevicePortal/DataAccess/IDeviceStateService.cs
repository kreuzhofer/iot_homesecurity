using System.Collections.Generic;
using System.Threading.Tasks;
using W10Home.DevicePortal.Models;

namespace W10Home.NetCoreDevicePortal.DataAccess
{
    public interface IDeviceStateService
    {
        Task<DeviceStateEntity> GetDeviceState(string deviceId, string channelKey);
        Task<List<DeviceStateEntity>> GetDeviceState(string deviceId);
    }
}