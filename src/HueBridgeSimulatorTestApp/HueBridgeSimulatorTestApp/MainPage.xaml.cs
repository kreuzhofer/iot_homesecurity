using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HueBridgeSimulatorTestApp.Controllers;
using Restup.Webserver.File;
using Restup.Webserver.Http;
using Restup.Webserver.Rest;
using Rssdp;
using Rssdp.Shared;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HueBridgeSimulatorTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Declare \_Publisher as a field somewhere, so it doesn't get GCed after the method finishes.
        private SsdpDevicePublisher _Publisher;

	    private HttpServer _httpServer;
	    private SsdpHueBridgeDevice _device;

	    // Call this method from somewhere to actually do the publish.
        public SsdpHueBridgeDevice PublishDevice()
        {
            // As this is a sample, we are only setting the minimum required properties.
	        var deviceDefinition = new SsdpHueBridgeDevice()
	        {
		        Uuid = Guid.NewGuid().ToString(),
		        Manufacturer = "Me",
		        FriendlyName = "Name",
		        ModelName = "HueBridgeEmulator",
		        HttpServerIpAddress = "192.168.178.48",
				HttpServerPort = "80",
				HttpServerOptionalSubFolder = "/api/hue",
		        MacAddress = "b827eb1cf6c9"
            };
            return deviceDefinition;
        }

        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Get local ip address
        /// </summary>
        /// <returns></returns>
        private string GetLocalIp()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return null;
            var hostname =
                NetworkInformation.GetHostNames()
                    .SingleOrDefault(
                        hn =>
                            hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);

            // the ip address
            return hostname?.CanonicalName;
        }

        private async void startDiscoveryButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Note, you can use deviceDefinition.ToDescriptionDocumentText() to retrieve the data to 
            //return from the Location end point, you just need to get that data to your service
            //implementation somehow. Depends on how you've implemented your service.

            _Publisher = new SsdpDevicePublisher();
	        _device = PublishDevice();
			_Publisher.AddDevice(_device);

	        await StartWebserverAsync();
        }

	    private async Task StartWebserverAsync()
	    {
		    // start local webserver
		    var restRouteHandler = new RestRouteHandler();
		    restRouteHandler.RegisterController<HueController>(_device.HttpServerIpAddress, _device.HttpServerPort, _device.HttpServerOptionalSubFolder, _device.HueUuid, _device.HueSerialNumber);
		    var configuration = new HttpServerConfiguration()
			    .ListenOnPort(80)
			    .RegisterRoute("api", restRouteHandler)
			    .RegisterRoute(new StaticFileRouteHandler(@"Web"))
			    .EnableCors(); // allow cors requests on all origins
		    //  .EnableCors(x => x.AddAllowedOrigin("http://specificserver:<listen-port>"));

		    var httpServer = new HttpServer(configuration);
		    _httpServer = httpServer;

		    await _httpServer.StartServerAsync();
	    }
    }
}
