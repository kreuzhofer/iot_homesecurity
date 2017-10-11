using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace W10Home.NetCoreDevicePortal.Hubs
{
    /// <summary>
    /// See https://blogs.msdn.microsoft.com/webdev/2017/09/14/announcing-signalr-for-asp-net-core-2-0/ for details about .net Core 2.0 SignalR
    /// </summary>
    [Authorize]
    public class LogHub : Hub
    {
        private CloudQueueClient _queueClient;

        public LogHub(IConfiguration configuration)
        {
            if (_queueClient == null)
            {
                var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
                _queueClient = CloudStorageAccount.Parse(connection).CreateCloudQueueClient();
            }
        }

        public IObservable<LogMessage> Logs(string deviceId)
        {
            return Observable.Create(async (IObserver<LogMessage> observer) =>
            {
                var queue = _queueClient.GetQueueReference("log-" + deviceId);
                if (await queue.ExistsAsync())
                {
                    while (true)
                    {
                        var message = await queue.GetMessageAsync();
                        if (message != null)
                        {
                            var logMessage = JsonConvert.DeserializeObject<LogMessage>(message.AsString);
                            observer.OnNext(logMessage);
                            await queue.DeleteMessageAsync(message);
                            await Task.Delay(1);
                        }
                        else
                        {
                            await Task.Delay(1000);
                        }
                    }
                }
            });
        }
    }
}
