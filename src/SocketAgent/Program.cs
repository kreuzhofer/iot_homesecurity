using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using Topshelf;

namespace SocketAgent
{
	class Program
	{
		const string ServiceName = "SocketService";

		static LoggingConfiguration CreateLoggingConfiguration()
		{
			var log = new LoggingConfiguration();
			var layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}";

			var targetConsole = new ColoredConsoleTarget { Layout = layout };
			log.AddTarget("console", targetConsole);
			log.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, targetConsole));

			var targetLogfile = new FileTarget
			{
				FileName = "${basedir}/" + ServiceName + "-${machinename}-{#####}.log",
				Layout = layout,
				ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
				ArchiveEvery = FileArchivePeriod.Minute
			};
			log.AddTarget("logfile", targetLogfile);
			log.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, targetLogfile));

			var targetTrace = new TraceTarget();
			log.AddTarget("trace", targetTrace);
			log.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, targetTrace));

			//var azureTarget = new AzureAppendBlobTarget
			//{
			//    ConnectionString = AzureConnectionString,
			//    Layout = layout,
			//    Name = "azure",
			//    BlobName = ServiceName + "-${machinename}.log",
			//    Container = $"logs-{Environment.MachineName.ToLower()}"
			//};
			//log.AddTarget("azure", azureTarget);
			//log.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, azureTarget));

			return log;
		}

		static void Main(string[] args)
		{
			LogManager.Configuration = CreateLoggingConfiguration();

			HostFactory.Run(x =>
			{
				x.Service<SocketService>(instance => instance
						.ConstructUsing(() => new SocketService(1))
						.WhenStarted(s => s.Start())
						.WhenStopped(s => s.Stop())
					);
				x.SetDisplayName("Socket Forwarding Service");
				x.SetServiceName(ServiceName);
				x.SetDescription("A service that relays packets from port a to port b.");
				x.StartAutomatically();
				x.RunAsLocalService();
				x.UseNLog();
			});
		}
	}
}
