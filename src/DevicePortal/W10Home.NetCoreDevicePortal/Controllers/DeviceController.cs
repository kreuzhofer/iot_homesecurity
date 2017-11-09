using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using W10Home.DevicePortal.IotHub;
using Newtonsoft.Json;
using System.Text;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Configuration;
using W10Home.NetCoreDevicePortal.DataAccess;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;
using W10Home.NetCoreDevicePortal.DataAccess.Services;
using W10Home.NetCoreDevicePortal.Models;

namespace W10Home.NetCoreDevicePortal.Controllers
{
    [Authorize]
    public class DeviceController : Controller
    {
        private DeviceManagementService _deviceManagementService;
        private IDeviceStateService _deviceStateService;
        private IDeviceConfigurationService _deviceConfigurationService;
        private IDeviceFunctionService _deviceFunctionService;
        private IConfiguration _configuration;
        private DevicePluginService _devicePluginService;
        private IDeviceService _deviceService;

        public DeviceController(IConfiguration configuration, DeviceManagementService deviceManagementService, IDeviceStateService deviceStateService, 
            IDeviceConfigurationService deviceConfigurationService, IDeviceFunctionService deviceFunctionService, DevicePluginService devicePluginService,
            IDeviceService deviceService)
        {
            _deviceManagementService = deviceManagementService;
            _deviceStateService = deviceStateService;
            _deviceConfigurationService = deviceConfigurationService;
            _deviceFunctionService = deviceFunctionService;
            _configuration = configuration;
            _devicePluginService = devicePluginService;
            _deviceService = deviceService;
        }

        // GET: Device
        public async Task<IActionResult> Index()
        {
            var userId = User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var userDevices = await _deviceService.GetAsync(userId);

            var rm = _deviceManagementService.GlobalRegistryManager;
            var devdatalist = new DeviceDataList();
            foreach (var device in userDevices)
            {
                var iotDevice = await rm.GetDeviceAsync(device.RowKey);
                devdatalist.Add(new DeviceData(iotDevice, device));
            }

            return View(devdatalist);
        }

        public async Task<IActionResult> Details(string id)
        {
            var userDevice = await GetMyDevice(id);
            if (userDevice == null)
            {
                return NotFound(); // no matter if this device does not exist or you just don't have the access rights. Don't give a hint...
            }

            var rm = _deviceManagementService.GlobalRegistryManager;
            Device device = await rm.GetDeviceAsync(id);

            var deviceData = new DeviceData(device, userDevice);
            var deviceStateList = await _deviceStateService.GetDeviceState(id);
            deviceStateList.ForEach(i=>i.LocalTimestamp = i.LocalTimestamp.ToLocalTime());
            deviceData.StateList = deviceStateList;

            var deviceFunctions = await _deviceFunctionService.GetFunctionsAsync(id);
            deviceData.DeviceFunctions = deviceFunctions;

            var devicePlugins = await _devicePluginService.GetAsync(id);
            deviceData.DevicePlugins = devicePlugins;

            return View(deviceData);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var userDevice = await GetMyDevice(id);
            if (userDevice == null)
            {
                return NotFound(); // no matter if this device does not exist or you just don't have the access rights. Don't give a hint...
            }

            var rm = _deviceManagementService.GlobalRegistryManager;
            Device device = await rm.GetDeviceAsync(id);

            var deviceData = new DeviceData(device, userDevice);
            return View(deviceData);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DeviceData data)
        {
            if (!await IsMyDevice(data.Id))
            {
                return NotFound();
            }

            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        serviceBaseUrl = _configuration["ExternalBaseUrl"],
                        apikey = _configuration["ApiKey"]
                    }
                }
            };

            // get device management client to send the twin update
            var registryManager = _deviceManagementService.GlobalRegistryManager;
            // todo temporary fix, see https://github.com/Azure/azure-iot-sdk-csharp/issues/213
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                CheckAdditionalContent = false
            };
            var twin = await registryManager.GetTwinAsync(data.Id);
            await registryManager.UpdateTwinAsync(data.Id, JsonConvert.SerializeObject(patch), twin.ETag);

            // if the device is offline, queue a message to update next time it gets online
            var device = await _deviceManagementService.GlobalRegistryManager.GetDeviceAsync(data.Id);
            if (device.ConnectionState == DeviceConnectionState.Disconnected)
            {
                var message = new
                {
                    queue = "management",
                    key = "restart",
                    value = "now"
                };
                var content = JsonConvert.SerializeObject(message);
                var bytes = Encoding.UTF8.GetBytes(content);
                await _deviceManagementService.ServiceClient.SendAsync(data.Id, new Message(bytes));
            }

            return await Edit(data.Id);
        }

        public async Task<IActionResult> EditFunction(string deviceId, string functionName)
        {
            if (!await IsMyDevice(deviceId))
            {
                return NotFound();
            }

            var function = await _deviceFunctionService.GetFunctionAsync(deviceId, functionName);
            return View(function);
        }

        [HttpPost]
        public async Task<IActionResult> EditFunction(DeviceFunctionEntity deviceFunctionEntity)
        {
            if (!await IsMyDevice(deviceFunctionEntity.PartitionKey))
            {
                return NotFound();
            }

            await _deviceFunctionService.SaveFunctionAsync(deviceFunctionEntity.PartitionKey,
                deviceFunctionEntity.RowKey, deviceFunctionEntity.Name, deviceFunctionEntity.TriggerType, deviceFunctionEntity.Interval,
                deviceFunctionEntity.QueueName, deviceFunctionEntity.Enabled, deviceFunctionEntity.Script);

            await _deviceManagementService.UpdateFunctionsAndVersionsTwinPropertyAsync(deviceFunctionEntity.PartitionKey);

            return await EditFunction(deviceFunctionEntity.PartitionKey, deviceFunctionEntity.RowKey);
        }

        #region Ajax methods

        [HttpPost]
        public async Task<IActionResult> SendMessage(string id, string message)
        {
            if (!await IsMyDevice(id))
            {
                return NotFound();
            }
            var client = _deviceManagementService.ServiceClient;
            await client.SendAsync(id, new Message(Encoding.UTF8.GetBytes(message)));
            return Json("message sent");
        }

        //[HttpPost]
        //public async Task<ActionResult> CallMethod(string id, string method, string payload)
        //{
        //    var client = _deviceManagementService.ServiceClient;
        //    var c2dmethod = new CloudToDeviceMethod(method);
        //    c2dmethod.SetPayloadJson("{payload: '" + payload + "'}");
        //    var result = await client.InvokeDeviceMethodAsync(id, c2dmethod);
        //    return Json(result.GetPayloadAsJson());
        //}

        [HttpPost]
        public async Task<ActionResult> Create(string name)
        {
            var client = _deviceManagementService.GlobalRegistryManager;
            var iotDevice = await client.AddDeviceAsync(new Device(Guid.NewGuid().ToString()));

            var userId = User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var device = new DeviceEntity
            {
                PartitionKey = userId,
                Name = name,
                RowKey = iotDevice.Id,
                ApiKey = Guid.NewGuid().ToString()
            };
            await _deviceService.InsertOrReplaceAsync(device);

            return Json("success");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var userDevice = await GetMyDevice(id);
            if (userDevice == null)
            {
                return NotFound(); // no matter if this device does not exist or you just don't have the access rights. Don't give a hint...
            }

            var registry = _deviceManagementService.GlobalRegistryManager;
            var iotDevice = await registry.GetDeviceAsync(id);
            iotDevice.Status = DeviceStatus.Disabled;
            await registry.UpdateDeviceAsync(iotDevice);

            userDevice.Deleted = true;
            userDevice.DeletedAt = DateTime.UtcNow;
            await _deviceService.InsertOrReplaceAsync(userDevice);

            return Json("success");
        }

        [HttpPost]
        public async Task<IActionResult> GetFunction(string deviceId, string functionId)
        {
            var userDevice = await GetMyDevice(deviceId);
            if (userDevice == null)
            {
                return NotFound(); // no matter if this device does not exist or you just don't have the access rights. Don't give a hint...
            }
            var result = await _deviceFunctionService.GetFunctionAsync(deviceId, functionId);
            return Json(result.ToDeviceFunctionModel());
        }

        #endregion

        /// <summary>
        /// Checks whether the given device id exists and is assigned to the current user
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private async Task<bool> IsMyDevice(string deviceId)
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
        private async Task<DeviceEntity> GetMyDevice(string deviceId)
        {
            var userId = User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var userDevice = await _deviceService.GetAsync(userId, deviceId);
            return userDevice;
        }

    }
}