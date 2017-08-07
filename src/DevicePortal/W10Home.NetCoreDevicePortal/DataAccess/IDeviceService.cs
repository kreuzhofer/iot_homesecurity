using System.Collections.Generic;
using System.Threading.Tasks;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.Models;

namespace W10Home.NetCoreDevicePortal.DataAccess
{
    public interface IDeviceService
    {
        Task<IEnumerable<DeviceEntity>> GetDevicesForUserAsync(string userId);
        Task<DeviceEntity> GetDevice(string userId, string deviceId);
    }
}