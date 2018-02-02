using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace W10Home.IoTCoreApp.Logging
{
    internal class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private TelemetryClient _telemetryClient;

        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(_telemetryClient);
        }
    }
}