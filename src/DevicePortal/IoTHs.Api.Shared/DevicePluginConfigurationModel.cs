﻿using System.Collections.Generic;

namespace IoTHs.Api.Shared
{
    public class DevicePluginConfigurationModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }
}