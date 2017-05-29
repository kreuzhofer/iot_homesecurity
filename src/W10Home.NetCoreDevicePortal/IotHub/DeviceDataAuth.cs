
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

using System;
using System.Text;
using Microsoft.Azure.Devices;

namespace W10Home.DevicePortal.IotHub
{
    public class DeviceDataAuth: DeviceData
    {
        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }
        public string ConnectionString { get; set; }

        public DeviceDataAuth(Device dev) :base (dev)
        {
            PrimaryKey = dev.Authentication.SymmetricKey.PrimaryKey;
            SecondaryKey = dev.Authentication.SymmetricKey.SecondaryKey;
        }
        public String SetDeviceConnectionString(string IotHubConnectionString)
        {
            StringBuilder deviceConnectionString = new StringBuilder();

            var hostName = String.Empty;
            var tokenArray = IotHubConnectionString.Split(';');
            for (int i = 0; i < tokenArray.Length; i++)
            {
                var keyValueArray = tokenArray[i].Split('=');
                if (keyValueArray[0] == "HostName")
                {
                    hostName = tokenArray[i] + ';';
                    break;
                }
            }

            if (!String.IsNullOrWhiteSpace(hostName))
            {
                deviceConnectionString.Append(hostName);
                deviceConnectionString.AppendFormat("DeviceId={0}", Id);

                deviceConnectionString.AppendFormat(";SharedAccessKey={0}", PrimaryKey);
            }
            ConnectionString = deviceConnectionString.ToString();
            return ConnectionString;
        }

    }
}