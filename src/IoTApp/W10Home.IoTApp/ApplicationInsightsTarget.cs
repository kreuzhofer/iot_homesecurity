using MetroLog;
using MetroLog.Layouts;
using MetroLog.Targets;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace W10Home.IoTCoreApp
{
    internal class ApplicationInsightsTarget : SyncTarget
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsTarget(Layout layout) : base(layout)
        {
        }

        public ApplicationInsightsTarget(TelemetryClient telemetryClient) : base(new SingleLineLayout())
        {
            _telemetryClient = telemetryClient;
        }

        protected override void Write(LogWriteContext context, LogEventInfo entry)
        {
            SeverityLevel level = SeverityLevel.Verbose;
            switch (entry.Level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    level = SeverityLevel.Verbose;
                    break;
                case LogLevel.Info:
                    level = SeverityLevel.Information;
                    break;
                case LogLevel.Warn:
                    level = SeverityLevel.Warning;
                    break;
                case LogLevel.Error:
                    level = SeverityLevel.Error;
                    break;
                case LogLevel.Fatal:
                    level = SeverityLevel.Critical;
                    break;
            }
            ITelemetry telemetry = null;
            if (entry.Exception == null)
            {
                telemetry = new TraceTelemetry(entry.Message, level);
            }
            else
            {
                telemetry = new ExceptionTelemetry(entry.Exception);
            }

            _telemetryClient.Track(telemetry);
        }
    }
}