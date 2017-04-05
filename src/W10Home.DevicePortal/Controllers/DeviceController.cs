using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using W10Home.DevicePortal.DataAccess;
using W10Home.DevicePortal.IotHub;

namespace W10Home.DevicePortal.Controllers
{
    public class DeviceController : Controller
    {
        // GET: Device
		public async Task<ActionResult> Index()
		{
			var rm = DevicesManagementSingleton.GlobalRegistryManager;
			var devlist = await rm.GetDevicesAsync(1000);
			var devdatalist = new DeviceDataList();
			foreach (Device dev in devlist)
			{
				devdatalist.Add(new DeviceData(dev));
			}
			return View(devdatalist);
		}

	    public async Task<ActionResult> Details(string id)
	    {
		    var rm = DevicesManagementSingleton.GlobalRegistryManager;
		    Device device = await rm.GetDeviceAsync(id);

			var deviceData = new DeviceData(device);
			var configService = new DeviceConfigurationService();
		    var configData = await configService.LoadConfig("daniel", id);
		    if (configData != null)
		    {
			    deviceData.Configuration = configData.Configuration;
		    }

		    return View(new DeviceData(device));
	    }

		public async Task<ActionResult> Edit(string id)
		{
			var rm = DevicesManagementSingleton.GlobalRegistryManager;
			Device device = await rm.GetDeviceAsync(id);

			var deviceData = new DeviceData(device);
			var configService = new DeviceConfigurationService();
			var configData = await configService.LoadConfig("daniel", id);
			if (configData != null)
			{
				deviceData.Configuration = JsonConvert.DeserializeObject<string>(configData.Configuration);
			}

			return View(deviceData);
		}

		[HttpPost]
	    public async Task<ActionResult> Edit(DeviceData data)
	    {
			// save configuration data
			var configService = new DeviceConfigurationService();
		    await configService.SaveConfig("daniel", data.Id, JsonConvert.SerializeObject(data.Configuration));
			return await Edit(data.Id);
	    }

		[HttpPost]
	    public async Task<ActionResult> SendMessage(string id, string message)
	    {
		    var client = DevicesManagementSingleton.ServiceClient;
		    await client.SendAsync(id, new Message(Encoding.UTF8.GetBytes(message)));
		    return Json("message sent");
	    }

		[HttpPost]
		public async Task<ActionResult> CallMethod(string id, string method, string payload)
		{
			var client = DevicesManagementSingleton.ServiceClient;
			var c2dmethod = new CloudToDeviceMethod(method);
			c2dmethod.SetPayloadJson("{payload: '"+payload+"'}");
			var result = await client.InvokeDeviceMethodAsync(id, c2dmethod);
			return Json(result.GetPayloadAsJson());
		}
	}
}