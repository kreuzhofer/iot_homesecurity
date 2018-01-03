using Microsoft.Extensions.Logging;

namespace IoTHs.Core.Logging
{
    public class RestLoggerProvider : ILoggerProvider
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RestLogger(categoryName);
        }
    }
}