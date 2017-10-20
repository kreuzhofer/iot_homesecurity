using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Devices.Interfaces;
using IoTHs.Api.Shared;
using System;
using System.Threading;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Xaml.Automation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using IoTHs.Core;
using IoTHs.Core.Queing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTHs.Plugin.HomeMatic
{
    public class HomeMaticDevice : DeviceBase
    {
        private string _name;
        private string _type;
        private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
        private readonly ILogger _log;
        private string _connectionString;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public HomeMaticDevice(ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<HomeMaticDevice>();
        }

        public override string Name => _name;
        public override string Type => _type;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _connectionString = configuration.Properties["ConnectionString"];
            _name = configuration.Name;
            _type = this.GetType().Name;

            MessageReceiverLoop(_cancellationTokenSource.Token);
        }

        public override IEnumerable<IDeviceChannel> GetChannels()
        {
            return _channels.AsEnumerable();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task TeardownAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _cancellationTokenSource.Cancel();
        }

        private async void MessageReceiverLoop(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    var queue = ServiceLocator.Current.GetService<IMessageQueue>();
                    if (queue.TryDeque(_name, out QueueMessage queuemessage))
                    {
                        if (queuemessage.Key == "runprogram")
                        {
                            await RunProgram(queuemessage.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "MessageReceiverLoop");
                }
                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(IoTHsConstants.MessageLoopDelay, cancellationToken);
                    }
                    catch
                    {
                        // gulp
                    }
                }
            } while (!cancellationToken.IsCancellationRequested);
            _log.LogTrace("Exit MessageReceiverLoop");
        }

        private async Task RunProgram(string programId)
        {
            var aHBPF = new HttpBaseProtocolFilter();
            aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            var functionContent = await new HttpClient(aHBPF).GetStringAsync(new Uri(_connectionString + "runprogram.cgi?program_id=" + programId));
        }
    }
}