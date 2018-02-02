using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace W10Home.IoTCoreApp.Logging
{
    internal static class ApplicationInsightsLoggerFactoryExtension
    {
        internal static ILoggingBuilder AddApplicationInsights(this ILoggingBuilder factory, TelemetryClient telemetryClient)
        {
            factory.AddProvider(new ApplicationInsightsLoggerProvider(telemetryClient));
            return factory;
        }
    }
}