using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Core
{
    public abstract class DeviceBase : IDevice
    {
        private string _name;
        private string _type;
        protected List<IDevice> _devices;
        protected List<IDeviceChannel> _channels;

        public string Name
        {
            get { return _name; }
        }

        public string Type
        {
            get { return _type; }
        }

        public IEnumerable<IDevice> Devices
        {
            get { return _devices; }
        }

        public IEnumerable<IDeviceChannel> Channels
        {
            get { return _channels; }
        }

        public IDeviceChannel GetChannel(string name)
        {
            return Channels.SingleOrDefault(c => c.Name == name);
        }

        protected DeviceBase()
        {
            _devices = new List<IDevice>();
            _channels = new List<IDeviceChannel>();
        }

        public virtual async Task InitializeAsync(DeviceConfigurationModel configuration)
        {
            _name = configuration.Name;
            _type = configuration.Type;
        }

        public virtual async Task TearDownAsync()
        {
            foreach (var device in Devices)
            {
                await device.TearDownAsync();
            }
            _devices.Clear();
            _devices = null;
        }
    }
}