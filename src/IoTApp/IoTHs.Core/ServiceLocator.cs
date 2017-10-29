using System;
using System.Collections.Generic;
using System.Text;

namespace IoTHs.Core
{
    public class ServiceLocator
    {
        private static IServiceProvider _serviceProvider;

        public static void SetLocatorProvider(Func<IServiceProvider> serviceProviderFactory)
        {
            _serviceProvider = serviceProviderFactory();
        }

        public static IServiceProvider Current
        {
            get { return _serviceProvider; }
        }

        public static bool IsLocationProviderSet
        {
            get { return _serviceProvider != null; }
        }
    }
}
