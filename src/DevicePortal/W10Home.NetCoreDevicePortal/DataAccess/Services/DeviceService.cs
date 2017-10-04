using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;

namespace W10Home.NetCoreDevicePortal.DataAccess.Services
{
    public class DeviceService : BaseTableService<DeviceEntity>, IDeviceService
    {
        public DeviceService(IConfiguration configuration) : base(configuration)
        {
        }
    }
}