﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using W10Home.Interfaces.Configuration;

namespace IoTHs.Api.Shared
{
    public class DeviceFunctionModel
    {
        public string DeviceId { get; set; }
        public string FunctionId { get; set; }
        public string Name { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public FunctionTriggerType TriggerType { get; set; }
        public string QueueName { get; set; }
        public int Interval { get; set; }
        public string Script { get; set; }
        public string Language { get; set; }
        public bool Enabled { get; set; }
    }
}