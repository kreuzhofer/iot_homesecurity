using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace W10Home.IoTCoreApp.Logging
{
    internal class ApplicationInsightsLogger : ILogger
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsLogger(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            var message = formatter(state, exception);
            if (exception != null && message != null)
            {
                message += "\n" + exception.Message;
            }

            SeverityLevel level = SeverityLevel.Verbose;
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    level = SeverityLevel.Verbose;
                    break;
                case LogLevel.Information:
                    level = SeverityLevel.Information;
                    break;
                case LogLevel.Warning:
                    level = SeverityLevel.Warning;
                    break;
                case LogLevel.Error:
                    level = SeverityLevel.Error;
                    break;
                case LogLevel.Critical:
                    level = SeverityLevel.Critical;
                    break;
            }
            ITelemetry telemetry = null;
            if (exception == null)
            {
                telemetry = new TraceTelemetry(message, level);
            }
            else
            {
                telemetry = new ExceptionTelemetry(exception);
            }

            _telemetryClient.Track(telemetry);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            //TODO not working with async
            return null;
        }
    }
}