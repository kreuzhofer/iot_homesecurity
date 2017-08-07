﻿using System.Threading.Tasks;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;

namespace W10Home.DevicePortal.DataAccess
{
    public interface IDeviceConfigurationService
    {
        Task<DeviceConfigurationEntity> LoadConfig(string deviceId, string configurationKey);
        Task SaveConfig(string deviceId, string configurationKey, string configurationJson);
    }
}