﻿using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Configuration;
using W10Home.Interfaces;

namespace W10Home.Plugin.AzureIoTHub
{
    public class AzureIoTHubDevice : IDevice
    {
        private DeviceClient deviceClient;
 
        private async void MessageReceiverLoop()
        {
			do
			{
				try
				{
					var message = await deviceClient.ReceiveAsync();
					if (message != null)
					{
						var reader = new StreamReader(message.BodyStream);
						var bodyString = await reader.ReadToEndAsync();
						Debug.WriteLine(bodyString);
						await deviceClient.CompleteAsync(message);
					}

				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					//TODO Log
				}
			} while (true);
        }

        public async Task SendMessageToIoTHubAsync(string deviceId, string location, string key, object value)
        {
            try
            {
				string strvalue = null;
				if(value is double || value is float)
				{
					strvalue = $"{((double)value).ToString("F")}";
				}
				else
				{
					strvalue = $"\"{value.ToString()}\"";
				}

                var payload = "{\"deviceId\": \"" +
                    deviceId +
                    "\", \"location\": \"" +
                    location +
                    "\", \"channelValue\": " +
                    value +
                    ", \"channelKey\": \""+ key + 
                    "\", \"localTimestamp\": \"" +
                    DateTime.Now.ToLocalTime().ToString("s") +
                    "\"}";

                var msg = new Message(Encoding.UTF8.GetBytes(payload));

                await deviceClient.SendEventAsync(msg);
				Debug.WriteLine(payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //TODO Log
            }
        }

		public async Task InitializeAsync(IDeviceConfiguration configuration)
		{
			try
			{
				// Instantiate the Azure IoT Hub device client
				deviceClient = DeviceClient.CreateFromConnectionString(configuration.Properties["ConnectionString"]);
				await deviceClient.SetMethodHandlerAsync("configure", HandleConfigureMethod, null);

				MessageReceiverLoop(); // launch message loop in the background
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				//TODO Log
			}

		}

		private async Task<MethodResponse> HandleConfigureMethod(MethodRequest methodRequest, object userContext)
		{
			Debug.WriteLine("HandleConfigureMethod called");
			return new MethodResponse(0);
		}

		public Task<IEnumerable<IChannel>> GetChannelsAsync()
		{
			throw new NotImplementedException();
		}

	    public async Task Teardown()
	    {
		    if (deviceClient != null)
		    {
			    await deviceClient.CloseAsync();
				deviceClient.Dispose();
			    deviceClient = null;
		    }
	    }
    }
}
