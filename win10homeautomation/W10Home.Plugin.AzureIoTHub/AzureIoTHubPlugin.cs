using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W10Home.Core.Interfaces;

namespace W10Home.Plugin.AzureIoTHub
{
    public class AzureIoTHubPlugin : IDevice
    {
        private DeviceClient deviceClient;
        public AzureIoTHubPlugin(string IoTHubConnectionString)
        {
            try
            {
                // Instantiate the Azure IoT Hub device client
                deviceClient = DeviceClient.CreateFromConnectionString(IoTHubConnectionString);
                
                MessageReceiverLoop(); // launch message loop in the background
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //TODO Log
            }
        }

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
                    DateTime.Now.ToLocalTime().ToString() +
                    "\"}";

                var msg = new Message(Encoding.UTF8.GetBytes(payload));

                await deviceClient.SendEventAsync(msg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //TODO Log
            }
        }

		public Task InitializeAsync()
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<IChannel>> GetChannelsAsync()
		{
			throw new NotImplementedException();
		}
	}
}
