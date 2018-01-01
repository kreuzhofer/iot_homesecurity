using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTHs.Devices.Interfaces;
using IoTHs.Api.Shared;
using System;
using System.Threading;
using System.Xml;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Xaml.Automation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using IoTHs.Core;
using IoTHs.Core.Http;
using IoTHs.Core.Queing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTHs.Plugin.HomeMatic
{
    public class HomeMaticDevicePlugin : DevicePluginBase
    {
        private string _name;
        private string _type;
        private List<IDeviceChannel> _channels = new List<IDeviceChannel>();
        private readonly ILogger _log;
        private string _connectionString;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public HomeMaticDevicePlugin(ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<HomeMaticDevicePlugin>();
        }

        public override string Name => _name;
        public override string Type => _type;

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            _connectionString = configuration.Properties["ConnectionString"];
            _name = configuration.Name;
            _type = this.GetType().Name;

            // get all devices
            var httpClient = new LocalHttpClient();
            var resultDevices = await httpClient.Client.GetAsync(_connectionString + "devicelist.cgi");
            if (resultDevices.IsSuccessStatusCode)
            {
                var xmlContent = await resultDevices.Content.ReadAsStringAsync();
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xmlContent);
                foreach (XmlNode deviceNode in xmlDocument.DocumentElement.ChildNodes)
                {
                    var deviceType = deviceNode.Attributes.Cast<XmlAttribute>().Single(n => n.Name == "device_type");
                    var address = deviceNode.Attributes.Cast<XmlAttribute>().Single(n => n.Name == "address");
                    var id = deviceNode.Attributes.Cast<XmlAttribute>().Single(n => n.Name == "ise_id");

                    //.Value == "HM-LC-Bl1-FM")
                }
            }

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
            var httpClient = new LocalHttpClient();
            var functionContent = await httpClient.Client.GetStringAsync(new Uri(_connectionString + "runprogram.cgi?program_id=" + programId));
        }
    }
}