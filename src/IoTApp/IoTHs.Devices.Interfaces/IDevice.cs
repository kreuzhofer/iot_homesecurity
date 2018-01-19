using System.Collections;
using System.Collections.Generic;

namespace IoTHs.Devices.Interfaces
{
    public interface IDevice
    {
        IEnumerable<IDevice> Devices { get; }
        IEnumerable<IDeviceChannel> Channels { get; }
        IDeviceChannel GetChannel(string name);
    }
}