﻿using System.Collections.Generic;
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
    public class HomeMaticPlugin : PluginBase
    {
        private readonly ILogger _log;
        private string _connectionString;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public HomeMaticPlugin(ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<HomeMaticPlugin>();
        }

        public override async Task InitializeAsync(DevicePluginConfigurationModel configuration)
        {
            await base.InitializeAsync(configuration);

            _connectionString = configuration.Properties["ConnectionString"];

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

        public override async Task TeardownAsync()
        {
            await base.TeardownAsync();

            _cancellationTokenSource.Cancel();
        }

        public object GetDatapointValue(string datapointId)
        {
            var httpClient = new LocalHttpClient();
            var task = httpClient.Client.GetStringAsync(new Uri(_connectionString + "state.cgi?datapoint_id=" + datapointId));
            Task.WaitAll(task);
            var result = task.Result;
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(result);
            var value = xmlDocument.DocumentElement.FirstChild.Attributes["value"].Value;
            float floatResult;
            if (float.TryParse(value, out floatResult))
            {
                return floatResult;
            }
            return value;
        }

        private async void MessageReceiverLoop(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    var queue = ServiceLocator.Current.GetService<IMessageQueue>();
                    if (queue.TryDeque(Name, out QueueMessage queuemessage))
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