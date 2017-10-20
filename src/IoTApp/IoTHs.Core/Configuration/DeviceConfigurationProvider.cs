using System;
using System.Collections.Generic;
using System.Text;
using IoTHs.Api.Shared;

namespace IoTHs.Core.Configuration
{
    public class DeviceConfigurationProvider
    {
        private DeviceConfigurationModel _model;

        public void SetConfiguration(DeviceConfigurationModel model)
        {
            _model = model;
        }

        public DeviceConfigurationModel Configuration
        {
            get { return _model; }
        }
    }
}
