using System;
using System.Collections.Generic;
using System.Text;
using IoTHs.Api.Shared;

namespace IoTHs.Core.Configuration
{
    public class DeviceConfigurationProvider
    {
        private AppConfigurationModel _model;

        public void SetConfiguration(AppConfigurationModel model)
        {
            _model = model;
        }

        public AppConfigurationModel Configuration
        {
            get { return _model; }
        }
    }
}
