using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IoTHs.Core;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.ABUS.SecVest
{
    public class SecvestDevice : DeviceBase
    {
        private HttpClient _client;
        private string _baseUrl;

        public SecvestDevice(HttpClient client, string baseUrl)
        {
            _client = client;
            _baseUrl = baseUrl;
            _channels.Add(new SecVestStatusChannel(_client, _baseUrl));
        }
    }
}