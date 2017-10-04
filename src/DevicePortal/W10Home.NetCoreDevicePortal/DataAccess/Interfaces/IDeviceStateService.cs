using System.Collections.Generic;
using System.Threading.Tasks;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;

namespace W10Home.NetCoreDevicePortal.DataAccess.Interfaces
{
    public interface IDeviceStateService
    {
        Task<DeviceStateEntity> GetDeviceState(string deviceId, string channelKey);
        Task<List<DeviceStateEntity>> GetDeviceState(string deviceId);
    }
}