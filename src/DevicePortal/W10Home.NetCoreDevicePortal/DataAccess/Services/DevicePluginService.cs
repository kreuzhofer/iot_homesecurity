using Microsoft.Extensions.Configuration;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;

namespace W10Home.NetCoreDevicePortal.DataAccess.Services
{
    public class DevicePluginService : BaseTableService<DevicePluginEntity>
    {
        public DevicePluginService(IConfiguration configuration) : base(configuration)
        {
        }
    }
}