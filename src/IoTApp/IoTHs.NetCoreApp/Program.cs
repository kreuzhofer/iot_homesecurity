using System;
using System.Diagnostics;
using IoTHs.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTHs.NetCoreApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // init IoC
            // see: http://intellitect.com/net-core-dependency-injection/#ActivatorUtilities
            // and https://stackify.com/net-core-dependency-injection/
            // and http://derpturkey.com/vnext-dependency-injection-overview/
            var container = new ServiceCollection();

            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging?tabs=aspnetcore2x#tabpanel_J929VbWwYc_aspnetcore2x
            container.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddRest();
                builder.SetMinimumLevel(LogLevel.Trace);
                //builder.AddFilter<DebugLoggerProvider>("Default", LogLevel.Trace);
                //builder.AddFilter("IoTHs.Plugin.AzureIoTHub", LogLevel.Debug);
            });
        }
    }
}
