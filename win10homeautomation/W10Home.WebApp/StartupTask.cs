using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Restup.Webserver.Http;
using Restup.Webserver.Rest;
using Restup.Webserver.File;
using System.Threading.Tasks;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace W10Home.WebApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private HttpServer _httpServer;

        private BackgroundTaskDeferral _deferral;

#pragma warning disable IDE1006 // Naming Styles
        public async void Run(IBackgroundTaskInstance taskInstance)
#pragma warning restore IDE1006 // Naming Styles
        {
            // This deferral should have an instance reference, if it doesn't... the GC will
            // come some day, see that this method is not active anymore and the local variable
            // should be removed. Which results in the application being closed.
            _deferral = taskInstance.GetDeferral();

            // configure IoT Hub plugin


            var restRouteHandler = new RestRouteHandler();

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
                .RegisterRoute(new StaticFileRouteHandler(@"Web"))
                .EnableCors(); // allow cors requests on all origins
            //  .EnableCors(x => x.AddAllowedOrigin("http://specificserver:<listen-port>"));

            var httpServer = new HttpServer(configuration);
            _httpServer = httpServer;

            await httpServer.StartServerAsync();

            // Dont release deferral, otherwise app will stop
        }
    }
}
