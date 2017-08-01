
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Devices;
using W10Home.DevicePortal.Models;
using W10Home.NetCoreDevicePortal.Models;

namespace W10Home.DevicePortal.IotHub
{
    public class DeviceData : IComparable<DeviceData>
    {
        public string Id { get; set; }
        public string ConnectionState { get; set; }
        public DateTime LastActivityTime { get; set; }
        public DateTime LastConnectionStateUpdatedTime { get; set; }
        public DateTime LastStateUpdatedTime { get; set; }
        public int MessageCount { get; set; }
        public string State { get; set; }
        public string SuspensionReason { get; set; }

		// Additional properties
		public string Configuration { get; set; }
	    public List<DeviceStateEntity> StateList { get; set; }
        public List<DeviceFunctionEntity> DeviceFunctions { get; set; }

// Url for config file

	    public int CompareTo(DeviceData other)
        {
            return string.Compare(this.Id, other.Id, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return $"Device ID = {this.Id}, ConnState = {this.ConnectionState}, ActivityTime = {this.LastActivityTime}, LastConnState = {this.LastConnectionStateUpdatedTime}, LastStateUpdatedTime = {this.LastStateUpdatedTime}, MessageCount = {this.MessageCount}, State = {this.State}, SuspensionReason = {this.SuspensionReason}\r\n";
        }

		public DeviceData()
		{ }
        public DeviceData(Device dev)
        {
            Id = dev.Id;
            ConnectionState = dev.ConnectionState.ToString();
            LastActivityTime = dev.LastActivityTime;
            LastConnectionStateUpdatedTime = dev.ConnectionStateUpdatedTime;
            LastStateUpdatedTime = dev.StatusUpdatedTime;
            MessageCount = dev.CloudToDeviceMessageCount;
            State = dev.Status.ToString();
            SuspensionReason = dev.StatusReason;
        }
    }
}
    