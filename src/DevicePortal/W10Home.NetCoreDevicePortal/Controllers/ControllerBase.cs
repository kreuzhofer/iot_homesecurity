using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;

namespace W10Home.NetCoreDevicePortal.Controllers
{
    public class ControllerBase : Controller
    {
        protected IDeviceService _deviceService;

        public ControllerBase(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        /// <summary>
        /// Checks whether the given device id exists and is assigned to the current user
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        protected async Task<bool> IsMyDevice(string deviceId)
        {
            var userId = User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var userDevice = await _deviceService.GetAsync(userId, deviceId);
            return userDevice != null;
        }

        /// <summary>
        /// Checks if the device is owned by the current user. Returns the device if it exists and is assigned to the user account.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        protected async Task<DeviceEntity> GetMyDevice(string deviceId)
        {
            var userId = User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var userDevice = await _deviceService.GetAsync(userId, deviceId);
            return userDevice;
        }
    }
}