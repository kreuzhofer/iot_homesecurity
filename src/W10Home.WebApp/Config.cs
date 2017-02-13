using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W10Home.IoTCoreApp
{
	internal static class Config
	{
		// Azure IoT Hub Configuration
		public const string AZURE_IOT_HUB_DEVICEID = "homecontroller";
		public const string AZURE_IOT_HUB_DEVICESAS = "SharedAccessSignature sr=dkreuzhiothub01.azure-devices.net%2Fdevices%2Fhomecontroller&sig=GzojltaSXcERk2n81MmZdfKv6GxbqrfoLW%2BdIigafW4%3D&se=1487002325";
		public const string AZURE_IOT_HUB_ADDRESS = "dkreuzhiothub01.azure-devices.net";
		public const string AZURE_IOT_HUB_PORT = "8883";

		public const string ETA_TOUCH_URL = "http://192.168.178.4:8080";
		public const string TWILIO_ACCOUNT_SID = "AC27aae66087f2918db2373a2835ae6cd8";
		public const string TWILIO_AUTH_TOKEN = "eb03c814ec1bf43862f108af0df7dc9d";
		public const string TWILIO_OUTGOING_PHONE = "+4915735986581";
		public const string TWILIO_RECEIVER_PHONE = "+4915144063507";
	}
}
