using System.Threading.Tasks;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.DataAccess.Services;

namespace W10Home.NetCoreDevicePortal.DataAccess.Interfaces
{
    public interface IDeviceService : IBaseTableService<DeviceEntity>
    {
        Task<DeviceEntity> GetWithApiKeyAsync(string deviceId, string apiKey);
    }
}