using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.Gpio
{
    public class GpioDevicePlugin : DevicePluginBase
    {
        private Dictionary<int, GpioPin> _activePins = new Dictionary<int, GpioPin>();
        private List<IDeviceChannel> _channels = new List<IDeviceChannel>();

        private void SetPin(int pin, GpioPinValue value)
        {
            _activePins[pin].Write(value);
        }

        public override string Name { get; }
        public override string Type { get; }

        public override IEnumerable<IDeviceChannel> GetChannels()
        {
            return _channels;
        }

        public override Task TeardownAsync()
        {
            throw new System.NotImplementedException();
        }

        public override Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            throw new System.NotImplementedException();
        }
    }
}