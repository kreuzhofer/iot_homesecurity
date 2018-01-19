using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Core.Channels;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.ETATouch
{
	public class EtaTouchPlugin : PluginBase
    {
        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            await base.InitializeAsync(configuration);

			var etatouchUrl = configuration.Properties["ConnectionString"];

            var etaDevice = new EtaDevice();
            await etaDevice.InitializeAsync(new DeviceConfigurationModel()
            {
                Name = "EtaDevice",
                Type = "EtaDevice",
                Properties = new Dictionary<string, string> {{"ConnectionString", etatouchUrl}}
            });
            _devices.Add(etaDevice);
		}
    }
}
