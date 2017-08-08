using System.Configuration;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;

namespace W10Home.DevicePortal.IotHub
{

    public class DeviceManagementService
    {
        public DeviceManagementService(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        static RegistryManager _globalRegistryManager;
		private static ServiceClient _client;
        private IConfiguration _configuration;

        public RegistryManager GlobalRegistryManager
        {
            get
            {
                if (_globalRegistryManager == null)
                {
                    var config = _configuration.GetSection("ConnectionStrings")["IotHub"];
                    _globalRegistryManager = RegistryManager.CreateFromConnectionString(config);
                }
	            return _globalRegistryManager;
            }
        }

	    public ServiceClient ServiceClient
	    {
		    get
		    {
				if (_client == null)
				{
				    var config = _configuration.GetSection("ConnectionStrings")["IotHub"];
                    _client = ServiceClient.CreateFromConnectionString(config);
				}
			    return _client;
		    }
		}
    }
}