using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Background;
using Restup.Webserver.Http;
using Restup.Webserver.Rest;
using Restup.Webserver.File;
using System.Threading.Tasks;
using W10Home.Plugin.AzureIoTHub;
using W10Home.Plugin.ETATouch;
using MoonSharp.Interpreter;
using Windows.Web.Http;
using Restup.WebServer.Http;
using W10Home.WebApp.Auth;
using W10Home.Plugin.Twilio;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace W10Home.WebApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private HttpServer _httpServer;

        private BackgroundTaskDeferral _deferral;
        private AzureIoTHubPlugin _iotHub;
        private ETATouchDevice _eta;
		private TwilioDevice _twilio;

#pragma warning disable IDE1006 // Naming Styles
		public async void Run(IBackgroundTaskInstance taskInstance)
#pragma warning restore IDE1006 // Naming Styles
        {
            // This deferral should have an instance reference, if it doesn't... the GC will
            // come some day, see that this method is not active anymore and the local variable
            // should be removed. Which results in the application being closed.
            _deferral = taskInstance.GetDeferral();

            // configure IoT Hub plugin
            _iotHub = new AzureIoTHubPlugin("iothubconnectionstring");

            // get data from ETA
            _eta = new ETATouchDevice("etatouchipadress");

			List<TwilioSmsChannelConfiguration> channelConfigurations = new List<TwilioSmsChannelConfiguration>();
			channelConfigurations.Add(new TwilioSmsChannelConfiguration("fromnumber", "tonumber"));
			_twilio = new TwilioDevice("accountsid", "authtoken", channelConfigurations);

			//await (await _twilio.GetChannelsAsync()).Single(c => c.Name == "SMS").SendMessageAsync("Homeautomation starting...");

			// start background worker that collects and forwards data
			BackgroundWorker(_eta, _iotHub);

			var authProvider = new BasicAuthorizationProvider("Login", new FixedCredentialsValidator());

			var restRouteHandler = new RestRouteHandler(authProvider);

            //restRouteHandler.RegisterController<AsyncControllerSample>();
            //restRouteHandler.RegisterController<FromContentControllerSample>();
            //restRouteHandler.RegisterController<PerCallControllerSample>();
            //restRouteHandler.RegisterController<SimpleParameterControllerSample>();
            //restRouteHandler.RegisterController<SingletonControllerSample>();
            //restRouteHandler.RegisterController<ThrowExceptionControllerSample>();
            //restRouteHandler.RegisterController<WithResponseContentControllerSample>();

            var configuration = new HttpServerConfiguration()
                .ListenOnPort(80)
                .RegisterRoute("api", restRouteHandler)
                .RegisterRoute(new StaticFileRouteHandler(@"Web", authProvider))
                .EnableCors(); // allow cors requests on all origins
            //  .EnableCors(x => x.AddAllowedOrigin("http://specificserver:<listen-port>"));

            var httpServer = new HttpServer(configuration);
            _httpServer = httpServer;

            await httpServer.StartServerAsync();

            // Dont release deferral, otherwise app will stop
        }

		private async void BackgroundWorker(ETATouchDevice eta, AzureIoTHubPlugin iotHub)
		{
			do
			{
				var menu = await _eta.GetMenuStructureFromEtaAsync();
				var value = await eta.GetValueFromEtaValuePathAsync(menu, "/Sys/Eingänge/Außentemperatur");
				double degrees = (double)value.Value / (double)value.ScaleFactor;
				await iotHub.SendMessageToIoTHubAsync("homecontroller", "home", "outdoortemp", degrees);
				await Task.Delay(60*1000);
			} while (true);
		}

		private void BackgroundScriptRunner(List<TreeItem> menu)
        {
			//do
			//{
			//	Script.RunFile("Scripts\\main.lua");
			//}
			//while (true);
		}
    }
}
