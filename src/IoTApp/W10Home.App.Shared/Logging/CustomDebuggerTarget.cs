using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NLog;
using NLog.Targets;

namespace W10Home.App.Shared.Logging
{
    [Target("CustomDebugger")]
    internal class CustomDebuggerTarget : TargetWithLayoutHeaderAndFooter
    {
        public CustomDebuggerTarget() { }

        public CustomDebuggerTarget(string name)
        {
            this.Name = name;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = this.Layout.Render(logEvent);
            Debug.WriteLine(message);
        }
    }
}
