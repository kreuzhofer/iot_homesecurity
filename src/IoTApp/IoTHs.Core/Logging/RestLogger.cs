using System;
using IoTHs.Core.Queing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTHs.Core.Logging
{
    public class RestLogger : ILogger
    {
        private string _categoryName;

        public RestLogger(string categoryName)
        {
            _categoryName = categoryName;
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
            if (ServiceLocator.IsLocationProviderSet)
            {
                ServiceLocator.Current.GetService<IMessageQueue>().Enqueue("iothublog", logLevel.ToString(), message, _categoryName);
            }
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