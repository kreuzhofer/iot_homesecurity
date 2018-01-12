using System;
using System.Collections.Generic;
using System.Diagnostics;
using Restup.Webserver.Attributes;
using Restup.Webserver.Models.Contracts;
using Restup.Webserver.Models.Schemas;

namespace HueBridgeSimulatorTestApp.Controllers
{
	[RestController(InstanceCreationType.PerCall)]
	internal class HueController
	{
		private string _httpServerIpAddress;
		private string _httpServerPort;
		private string _httpServerOptionalSubFolder;
		private string _hueUuid;
		private string _hueSerialNumber;

		public HueController(string httpServerIpAddress, string httpServerPort, string httpServerOptionalSubFolder,
			string hueUuid, string hueSerialNumber)
		{
			_httpServerIpAddress = httpServerIpAddress;
			_httpServerPort = httpServerPort;
			_httpServerOptionalSubFolder = httpServerOptionalSubFolder;
			_hueUuid = hueUuid;
			_hueSerialNumber = hueSerialNumber;
		}

		[UriFormat("/hue/{document}")]
		public IGetResponse Get(string document)
		{
			string content = String.Format(
@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<root xmlns=""urn:schemas-upnp-org:device-1-0"">
	<specVersion>
		<major>1</major>
		<minor>0</minor>
	</specVersion>
	<URLBase>http://{0}:{1}{2}/</URLBase>
	<device>
		<deviceType>urn:schemas-upnp-org:device:Basic:1</deviceType>
		<friendlyName>Philips hue ({0})</friendlyName>
		<manufacturer>Royal Philips Electronics</manufacturer>
		<manufacturerURL>http://www.philips.com</manufacturerURL>
		<modelDescription>Philips hue Personal Wireless Lighting</modelDescription>
		<modelName>Philips hue bridge 2015</modelName>
		<modelNumber>929000226503</modelNumber>
		<modelURL>http://www.meethue.com</modelURL>
		<serialNumber>{4}</serialNumber>
		<UDN>uuid:{3}</UDN>
        <serviceList>
            <service>
                <serviceType>(null)</serviceType>
                <serviceId>(null)</serviceId>
                <controlURL>(null)</controlURL>
                <eventSubURL>(null)</eventSubURL>
                <SCPDURL>(null)</SCPDURL>
         </service>
        </serviceList>
        <presentationURL>index.html</presentationURL>
        <iconList>
            <icon>
                <mimetype>image/png</mimetype>
                <height>48</height>
                <width>48</width>
                <depth>24</depth>
                <url>hue_logo_0.png</url>
            </icon>
            <icon>
                <mimetype>image/png</mimetype>
                <height>120</height>
                <width>120</width>
                <depth>24</depth>
                <url>hue_logo_3.png</url>
            </icon>
        </iconList>
</device>
</root>

", _httpServerIpAddress, _httpServerPort, _httpServerOptionalSubFolder, _hueUuid, _hueSerialNumber);
			Debug.WriteLine("document.xml content: "+content);
			return new GetResponse(GetResponse.ResponseStatus.OK, new Dictionary<string, string>(){{"Content-Type", "text/xml"}}, content);
		}
	}
}