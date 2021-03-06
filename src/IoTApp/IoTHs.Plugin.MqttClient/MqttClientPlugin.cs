﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Core;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;

namespace IoTHs.Plugin.MqttClient
{
    public class MqttClientPlugin : PluginBase
    {
	    private uPLibrary.Networking.M2Mqtt.MqttClient _mqttclient;
	    private string _deviceid;
	    private Dictionary<string, Func<string, KeyValuePair<int, string>>> _methodRegistrations = new Dictionary<string, Func<string, KeyValuePair<int, string>>>();

#pragma warning disable 1998
        public async Task SendMessageToIoTHubAsync(string deviceId, string location, string key, object value)
#pragma warning restore 1998
        {
            try
            {
				string strvalue = null;
				if(value is double || value is float)
				{
					strvalue = $"{((double)value):F}";
				}
				else
				{
					strvalue = $"\"{value.ToString()}\"";
				}

                var payload =
	                $"{{\"deviceid\": \"{deviceId}\", \"location\": \"{location}\", \"channelvalue\": {value}, \"channelkey\": \"{key}\", \"localtimestamp\": \"{DateTime.Now.ToUniversalTime():O}\"}}";

				_mqttclient.Publish($"devices/{_deviceid}/messages/events/", Encoding.UTF8.GetBytes(payload));
				Debug.WriteLine(payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //TODO Log
            }
        }

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            await base.InitializeAsync(configuration);
			try
			{
				_deviceid = configuration.Properties["DeviceId"];
				string deviceSas = configuration.Properties["DeviceSas"];
				string iotHubAddress = configuration.Properties["IotHubAddress"];
				int iotHubPort = Int32.Parse(configuration.Properties["IotHubPort"]);

				// init mqtt client
				_mqttclient = new uPLibrary.Networking.M2Mqtt.MqttClient(iotHubAddress, iotHubPort, true, MqttSslProtocols.TLSv1_2);
				_mqttclient.ConnectionClosed += MqttclientOnConnectionClosed;
				_mqttclient.Connect(_deviceid, $"{iotHubAddress}/{_deviceid}/api-version=2016-11-14", deviceSas);
				_mqttclient.Subscribe(new[] {$"devices/{_deviceid}/messages/devicebound/#"}, new byte[] {0});
				_mqttclient.Subscribe(new[] { "$iothub/methods/POST/#" }, new byte[] { 0 });
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				//TODO Log
			}
		}

	    public void RegisterMethod(string methodName, Func<string, KeyValuePair<int, string>> deviceMethodImpl)
	    {
		    _methodRegistrations.Add(methodName, deviceMethodImpl);
	    }

        public override async Task TeardownAsync()
        {
            await base.TeardownAsync();

			if (_mqttclient != null)
			{
				_mqttclient.MqttMsgPublishReceived -= Mqttclient_MqttMsgPublishReceived;
				_mqttclient.ConnectionClosed -= MqttclientOnConnectionClosed;
				_mqttclient.Disconnect();
				_mqttclient = null;
				_methodRegistrations.Clear();
			}
		}

		private void MqttclientOnConnectionClosed(object sender, EventArgs eventArgs)
	    {
		    Debug.WriteLine("MQTT Connection closed");
	    }

	    private void Mqttclient_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
		{
			try
			{
				var methodPayload = Encoding.UTF8.GetString(e.Message);
				Debug.WriteLine("Message from cloud to device: " + methodPayload);
				var methodCallTopic = "$iothub/methods/POST/";
				if (e.Topic.StartsWith(methodCallTopic)) // direct method call
				{
					var methodCallSubstring = e.Topic.Substring(methodCallTopic.Length);
					var methodName = methodCallSubstring.Split('/')[0];
					var requestId = methodCallSubstring.Split('/')[1].Substring(6);

					Debug.WriteLine($"Method: [{methodName}], Request: [{requestId}]");

					var method = _methodRegistrations[methodName];
					var methodResult = method.Invoke(methodPayload);
					string responseBody = JsonConvert.SerializeObject(methodResult.Value);
					// respond to server
					_mqttclient.Publish($"$iothub/methods/res/{methodResult.Key}/?$rid={requestId}", Encoding.UTF8.GetBytes(responseBody));
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

    }
}
