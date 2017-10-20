using System;
using System.Collections.Generic;
using System.Text;
using IoTHs.Core;
using IoTHs.Core.Queing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace W10Home.App.Shared.Logging
{
    internal class IotHubTarget : TargetWithLayout
    {
        public IotHubTarget()
        {
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (ServiceLocator.IsLocationProviderSet)
            {
                ServiceLocator.Current.GetService<IMessageQueue>().Enqueue("iothublog", logEvent.Level.Name, this.RenderLogEvent(this.Layout, logEvent)
                    , "json");
            }
        }
    }
}
