using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Devices.Interfaces;
using IoTHs.Api.Shared;
using W10Home.Core.Standard;
using System;
using System.Threading;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Xaml.Automation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Microsoft.Practices.ServiceLocation;
using W10Home.Core.Queing;
using MetroLog;

namespace IoTHs.Plugin.HomeMatic
{
    public class HomeMaticDevice : DeviceBase
    {
        private string _name;
        private string _type;
        private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
        private readonly ILogger _log = LogManagerFactory.DefaultLogManager.GetLogger<HomeMaticDevice>();
        private string _connectionString;

        public override string Name => _name;
        public override string Type => _type;

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            _connectionString = configuration.Properties["ConnectionString"];
            _name = configuration.Name;
            _type = this.GetType().Name;

            MessageReceiverLoop(CancellationToken.None);
        }

        public override IEnumerable<IDeviceChannel> GetChannels()
        {
            return _channels.AsEnumerable();
        }

        public override async Task TeardownAsync()
        {
            
        }

        private async void MessageReceiverLoop(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
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
                    _log.Error("MessageReceiverLoop", ex);
                }
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1, cancellationToken);
                }
            } while (!cancellationToken.IsCancellationRequested);
            _log.Trace("Exit MessageReceiverLoop");
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