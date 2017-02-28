using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Background;
using Restup.Webserver.Http;
using Restup.Webserver.Rest;
using Restup.Webserver.File;
using System.Threading.Tasks;
using W10Home.Plugin.ETATouch;
using MoonSharp.Interpreter;
using Windows.Web.Http;
using Restup.WebServer.Http;
using W10Home.IoTCoreApp.Auth;
using W10Home.Plugin.Twilio;
using Windows.Devices.Gpio;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using W10Home.IoTCoreApp.Controllers;
using W10Home.Core.Queing;
using System.Threading;
using System.Diagnostics;
using W10Home.Core.Configuration;
using W10Home.Plugin.AzureIoTHub;
using W10Home.Interfaces;
using Newtonsoft.Json;
using W10Home.App.Shared;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace W10Home.IoTCoreApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
	    private CoreApp _coreApp;

	    public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // This deferral should have an instance reference, if it doesn't... the GC will
            // come some day, see that this method is not active anymore and the local variable
            // should be removed. Which results in the application being closed.
            _deferral = taskInstance.GetDeferral();

	        _coreApp = new CoreApp();
			await _coreApp.Run();

	        // Dont release deferral, otherwise app will stop
        }


    }
}
