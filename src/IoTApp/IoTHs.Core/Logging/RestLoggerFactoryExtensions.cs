using Microsoft.Extensions.Logging;

namespace IoTHs.Core.Logging
{
    public static class RestLoggerFactoryExtensions
    {
        public static ILoggingBuilder AddRest(this ILoggingBuilder factory)
        {
            factory.AddProvider(new RestLoggerProvider());
            return factory;
        }
    }
}