
//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

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