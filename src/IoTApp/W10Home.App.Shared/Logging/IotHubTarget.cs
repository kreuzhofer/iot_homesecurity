using System;
using System.Collections.Generic;
using System.Text;
using MetroLog;
using MetroLog.Layouts;
using MetroLog.Targets;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using W10Home.Core.Queing;

namespace W10Home.App.Shared.Logging
{
    internal class IotHubTarget : SyncTarget
    {
        public IotHubTarget(Layout layout) : base(layout)
        {
        }

        public IotHubTarget() : base(new SingleLineLayout())
        {
        }

        protected override void Write(LogWriteContext context, LogEventInfo entry)
        {
            if (ServiceLocator.IsLocationProviderSet)
            {
                var entrySerialized = entry.ToJson();
                ServiceLocator.Current.GetInstance<IMessageQueue>().Enqueue("iothublog", entry.Level.ToString(), entrySerialized
                    , "json");
            }
        }
    }
}
