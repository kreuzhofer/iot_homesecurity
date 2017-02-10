
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

namespace W10Home.DevicePortal.IotHub
{

    public static class DevicesManagementSingleton
    {
        static RegistryManager _globalRegistryManager;
		private static ServiceClient _client;

		public static RegistryManager GlobalRegistryManager
        {
            get
            {
                if (_globalRegistryManager == null)
                {
                    _globalRegistryManager = RegistryManager.CreateFromConnectionString(ConfigurationManager.ConnectionStrings["IotHub"].ConnectionString);
                }
	            return _globalRegistryManager;
            }
        }

	    public static ServiceClient ServiceClient
	    {
		    get
		    {
				if (_client == null)
				{
					_client = ServiceClient.CreateFromConnectionString(ConfigurationManager.ConnectionStrings["IotHub"].ConnectionString);
				}
			    return _client;
		    }
		}
    }
}