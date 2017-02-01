using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
						Debug.WriteLine(message.ToString());
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

				MessageReceiverLoop(); // launch message loop in the background
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				//TODO Log
			}

		}

		public Task<IEnumerable<IChannel>> GetChannelsAsync()
		{
			throw new NotImplementedException();
		}
	}
}
