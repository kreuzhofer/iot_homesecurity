using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using IoTHs.Api.Shared;
using IoTHs.Core;
using IoTHs.Devices.Interfaces;

namespace IoTHs.Plugin.Gpio
{
    public class GpioPlugin : PluginBase
    {
        private Dictionary<int, GpioPin> _pins = new Dictionary<int, GpioPin>();

        public override async Task TeardownAsync()
        {
            await base.TeardownAsync();

            foreach (var gpioPin in _pins)
            {
                gpioPin.Value.Dispose();
            }
            _pins.Clear();
            _pins = null;
        }

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            await base.InitializeAsync(configuration);

            _pins = GetPins(configuration, "pins");
        }

        private Dictionary<int, GpioPin> GetPins(DevicePluginConfigurationModel configuration, string configKey)
        {
            var result = new Dictionary<int, GpioPin>();

            string[] pinConfigArray = null;
            if (configuration.Properties.ContainsKey(configKey))
            {
                pinConfigArray = configuration.Properties[configKey].Split(',');
            }

            if (pinConfigArray != null)
            {
                foreach (var pinConfigValue in pinConfigArray)
                {
                    var pinPair = pinConfigValue.Split(':');
                    if (pinPair.Length == 2 && 
                        Int32.TryParse(pinPair[0], out int pinNumber) && 
                        Enum.TryParse(pinPair[1], out GpioPinDriveMode driveMode))
                    {
                        var pin = GpioController.GetDefault().OpenPin(pinNumber);
                        pin.SetDriveMode(driveMode);
                        result.Add(pinNumber, pin);
                    }
                }
            }
            return result;
        }

        private void SetPin(int pin, GpioPinValue value)
        {
            _pins[pin].Write(value);
        }
    }
}