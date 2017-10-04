using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using W10Home.DevicePortal.IotHub;
using Newtonsoft.Json;
using System.Text;
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
            var userId = User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var userDevice = await _deviceService.GetAsync(userId, id);
            if (userDevice == null)
            {
                return NotFound(); // no matter if this device does not exist or you just don't have the access rights. Don't give a hint...
            }

            var rm = _deviceManagementService.GlobalRegistryManager;
            Device device = await rm.GetDeviceAsync(id);

            var deviceData = new DeviceData(device, userDevice);
            var configData = await _deviceConfigurationService.LoadConfig(id, "configurationFileUrl");
            if (configData != null)
            {
                deviceData.Configuration = configData.Configuration;
            }
            var deviceStateList = await _deviceStateService.GetDeviceState(id);
            deviceData.StateList = deviceStateList;

            var deviceFunctions = await _deviceFunctionService.GetFunctionsAsync(id);
            deviceData.DeviceFunctions = deviceFunctions;

            var devicePlugins = await _devicePluginService.GetAsync(id);
            deviceData.DevicePlugins = devicePlugins;

            return View(deviceData);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var userId = User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var userDevice = await _deviceService.GetAsync(userId, id);
            if (userDevice == null)
            {
                return NotFound(); // no matter if this device does not exist or you just don't have the access rights. Don't give a hint...
            }

            var rm = _deviceManagementService.GlobalRegistryManager;
            Device device = await rm.GetDeviceAsync(id);

            var deviceData = new DeviceData(device, userDevice);
            var configData = await _deviceConfigurationService.LoadConfig(id, "configurationFileUrl");
            if (configData != null)
            {
                deviceData.Configuration = configData.Configuration;
            }

            return View(deviceData);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DeviceData data)
        {
            await _deviceConfigurationService.SaveConfig(data.Id, "configurationFileUrl", data.Configuration);

            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        configurationUrl = _configuration["ExternalBaseUrl"]
                    }
                }
            };

            // get device management client to send the congfiguration
            var registryManager = _deviceManagementService.GlobalRegistryManager;
            var twin = await registryManager.GetTwinAsync(data.Id);
            await registryManager.UpdateTwinAsync(data.Id, JsonConvert.SerializeObject(patch), twin.ETag);

            return await Edit(data.Id);
        }

        public async Task<IActionResult> EditFunction(string deviceId, string functionName)
        {
            var function = await _deviceFunctionService.GetFunctionAsync(deviceId, functionName);
            return View(function);
        }

        [HttpPost]
        public async Task<IActionResult> EditFunction(DeviceFunctionEntity deviceFunctionEntity)
        {
            await _deviceFunctionService.SaveFunctionAsync(deviceFunctionEntity.PartitionKey,
                deviceFunctionEntity.RowKey, deviceFunctionEntity.Name, deviceFunctionEntity.TriggerType, deviceFunctionEntity.Interval,
                deviceFunctionEntity.QueueName, deviceFunctionEntity.Enabled, deviceFunctionEntity.Script);

            var functions = await _deviceFunctionService.GetFunctionsAsync(deviceFunctionEntity.PartitionKey);
            string functionsAndVersions = "";
            foreach (var function in functions)
            {
                functionsAndVersions += function.RowKey + ":" + function.Version + ",";
            }
            functionsAndVersions = functionsAndVersions.TrimEnd(',');

            // update device twin
            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        functions = new
                        {
                            versions = functionsAndVersions,
                            baseUrl = _configuration["ExternalBaseUrl"]
                        }
                    }
                }
            };
            var registryManager = _deviceManagementService.GlobalRegistryManager;
            var twin = await registryManager.GetTwinAsync(deviceFunctionEntity.PartitionKey);
            await registryManager.UpdateTwinAsync(deviceFunctionEntity.PartitionKey, JsonConvert.SerializeObject(patch), twin.ETag);

            return await EditFunction(deviceFunctionEntity.PartitionKey, deviceFunctionEntity.RowKey);
        }



#region Ajax methods

        //[HttpPost]
        //public async Task<IActionResult> SendMessage(string id, string message)
        //{
        //    var client = _deviceManagementService.ServiceClient;
        //    await client.SendAsync(id, new Message(Encoding.UTF8.GetBytes(message)));
        //    return Json("message sent");
        //}

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

            // save configuration data
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
            var userId = User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var userDevice = await _deviceService.GetAsync(userId, id);
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

        #endregion

    }
}