using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Api.Shared.CronJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using W10Home.DevicePortal.IotHub;
using W10Home.Interfaces.Configuration;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;

namespace W10Home.NetCoreDevicePortal.Controllers
{
    [Authorize]
    public class FunctionController : ControllerBase
    {
        private IDeviceFunctionService _deviceFunctionService;
        private DeviceManagementService _deviceManagementService;

        public FunctionController(IDeviceService deviceService, IDeviceFunctionService deviceFunctionService,
            DeviceManagementService deviceManagementService) : base(deviceService)
        {
            _deviceFunctionService = deviceFunctionService;
            _deviceManagementService = deviceManagementService;
        }

        public async Task<IActionResult> Edit(string deviceId, string functionName)
        {
            if (!await IsMyDevice(deviceId))
            {
                return NotFound();
            }

            var function = await _deviceFunctionService.GetFunctionAsync(deviceId, functionName);
            return View(function);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DeviceFunctionEntity deviceFunctionEntity)
        {
            if (!await IsMyDevice(deviceFunctionEntity.PartitionKey))
            {
                return NotFound();
            }
            if (String.IsNullOrEmpty(deviceFunctionEntity.RowKey))
            {
                deviceFunctionEntity.RowKey = Guid.NewGuid().ToString();
            }

            if (deviceFunctionEntity.TriggerType == FunctionTriggerType.CronSchedule.ToString())
            {
                if (!new CronSchedule().IsValid(deviceFunctionEntity.CronSchedule))
                {
                    ModelState.AddModelError("CronSchedule", "Invalid cron schedule");
                }
            }

            await _deviceFunctionService.SaveFunctionAsync(deviceFunctionEntity.PartitionKey,
                deviceFunctionEntity.RowKey, deviceFunctionEntity.Name, deviceFunctionEntity.TriggerType, deviceFunctionEntity.Interval,
                deviceFunctionEntity.CronSchedule ,deviceFunctionEntity.QueueName, deviceFunctionEntity.Enabled, deviceFunctionEntity.Script);

            await _deviceManagementService.UpdateFunctionsAndVersionsTwinPropertyAsync(deviceFunctionEntity.PartitionKey);

            return await Edit(deviceFunctionEntity.PartitionKey, deviceFunctionEntity.RowKey);
        }

        public async Task<IActionResult> Add(string deviceId)
        {
            if (!await IsMyDevice(deviceId))
            {
                return NotFound();
            }
            var deviceFunction = new DeviceFunctionEntity
            {
                Enabled = true,
                Interval = 0,
                CronSchedule = "* * * * *",
                Language = FunctionLanguage.Lua.ToString(),
                Name = "New function",
                PartitionKey = deviceId,
                QueueName = "triggerqueue",
                Script = "function run(message) end;",
                TriggerType = FunctionTriggerType.MessageQueue.ToString()
            };
            return View("Edit", deviceFunction);
        }

    }
}