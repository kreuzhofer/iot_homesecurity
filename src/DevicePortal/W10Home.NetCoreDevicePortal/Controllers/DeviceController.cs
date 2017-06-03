using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using W10Home.DevicePortal.IotHub;
using W10Home.DevicePortal.DataAccess;
using Newtonsoft.Json;
using System.Text;
using W10Home.NetCoreDevicePortal.DataAccess;

namespace W10Home.NetCoreDevicePortal.Controllers
{
    public class DeviceController : Controller
    {
        private DeviceManagementService _deviceManagementService;
        private IDeviceStateService _deviceStateService;
        private IDeviceConfigurationService _deviceConfigurationService;

        public DeviceController(DeviceManagementService deviceManagementService, IDeviceStateService deviceStateService, IDeviceConfigurationService deviceConfigurationService)
        {
            _deviceManagementService = deviceManagementService;
            _deviceStateService = deviceStateService;
            _deviceConfigurationService = deviceConfigurationService;
        }

        // GET: Device
        public async Task<IActionResult> Index()
        {
            var rm = _deviceManagementService.GlobalRegistryManager;
            var devlist = await rm.GetDevicesAsync(1000);
            var devdatalist = new DeviceDataList();
            foreach (Device dev in devlist)
            {
                devdatalist.Add(new DeviceData(dev));
            }
            return View(devdatalist);
        }

        public async Task<IActionResult> Details(string id)
        {
            var rm = _deviceManagementService.GlobalRegistryManager;
            Device device = await rm.GetDeviceAsync(id);

            var deviceData = new DeviceData(device);
            var configData = await _deviceConfigurationService.LoadConfig(id, "configurationFileUrl");
            if (configData != null)
            {
                deviceData.Configuration = configData.Configuration;
            }
            var deviceStatusList = await _deviceStateService.GetDeviceState(id);
            deviceData.StatusList = deviceStatusList;

            return View(deviceData);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var rm = _deviceManagementService.GlobalRegistryManager;
            Device device = await rm.GetDeviceAsync(id);

            var deviceData = new DeviceData(device);
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
            // save configuration data
            await _deviceConfigurationService.SaveConfig(data.Id, "configurationFileUrl", data.Configuration);

            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        configurationUrl = data.Configuration,
                    }
                }
            };

            // get device management client to send the congfiguration
            var registryManager = _deviceManagementService.GlobalRegistryManager;
            var twin = await registryManager.GetTwinAsync(data.Id);
            await registryManager.UpdateTwinAsync(data.Id, JsonConvert.SerializeObject(patch), twin.ETag);

            return await Edit(data.Id);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string id, string message)
        {
            var client = _deviceManagementService.ServiceClient;
            await client.SendAsync(id, new Message(Encoding.UTF8.GetBytes(message)));
            return Json("message sent");
        }

        [HttpPost]
        public async Task<ActionResult> CallMethod(string id, string method, string payload)
        {
            var client = _deviceManagementService.ServiceClient;
            var c2dmethod = new CloudToDeviceMethod(method);
            c2dmethod.SetPayloadJson("{payload: '" + payload + "'}");
            var result = await client.InvokeDeviceMethodAsync(id, c2dmethod);
            return Json(result.GetPayloadAsJson());
        }

    }
}