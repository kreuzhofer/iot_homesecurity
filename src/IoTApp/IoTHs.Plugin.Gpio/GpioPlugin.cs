using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.Gpio
{
    public class GpioPlugin : PluginBase
    {
        private Dictionary<int, GpioPin> _activePins = new Dictionary<int, GpioPin>();
        private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
        private string _name;
        private string _type;

        public override string Name
        {
            get { return _name; }
        }

        public override string Type
        {
            get { return _type; }
        }

        public override IEnumerable<IDeviceChannel> GetChannels()
        {
            return _channels;
        }

        public override async Task TeardownAsync()
        {
        }

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            _name = configuration.Name;
            _type = configuration.Type;
        }

        private void SetPin(int pin, GpioPinValue value)
        {
            _activePins[pin].Write(value);
        }
    }
}