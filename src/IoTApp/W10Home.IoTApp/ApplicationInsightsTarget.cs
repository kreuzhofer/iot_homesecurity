using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using NLog;
using NLog.Targets;

namespace W10Home.IoTCoreApp
{
    internal class ApplicationInsightsTarget : TargetWithLayout
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsTarget(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            SeverityLevel level = SeverityLevel.Verbose;
            switch (logEvent.Level.Name)
            {
                case "Trace":
                case "Debug":
                    level = SeverityLevel.Verbose;
                    break;
                case "Info":
                    level = SeverityLevel.Information;
                    break;
                case "Warn":
                    level = SeverityLevel.Warning;
                    break;
                case "Error":
                    level = SeverityLevel.Error;
                    break;
                case "Fatal":
                    level = SeverityLevel.Critical;
                    break;
            }
            ITelemetry telemetry = null;
            if (logEvent.Exception == null)
            {
                telemetry = new TraceTelemetry(logEvent.Message, level);
            }
            else
            {
                telemetry = new ExceptionTelemetry(logEvent.Exception);
            }

            _telemetryClient.Track(telemetry);
        }
    }
}