using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using W10Home.Core.Configuration;
using W10Home.Core.Queing;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;
using W10Home.Plugin.ABUS.SecVest;
using W10Home.Plugin.AzureIoTHub;

namespace W10Home.App.Shared
{
	internal class FunctionsEngine
    {
		private readonly List<Timer> _timers = new List<Timer>();

	    public void Initialize(RootConfiguration configuration)
		{
			if (configuration.Functions == null)
			{
				return;
			}
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
			foreach (var function in configuration.Functions)
			{
				if (function.TriggerType == FunctionTriggerType.RecurringIntervalTimer)
				{
					var timerFunction = (RecurringIntervalTriggeredFunctionDeclaration)function;
					var script = SetupNewScript();
					script.DoString(timerFunction.Code);
					var timer = new Timer(state =>
					{
						lock (script)
						{
							try
							{
								script.Call(script.Globals["run"]);
							}
							catch (Exception ex)
							{
								Debug.WriteLine(ex.Message);
								//todo log
							}						}
					}, null ,timerFunction.Interval, timerFunction.Interval);
					_timers.Add(timer);
				}
				else if (function.TriggerType == FunctionTriggerType.MessageQueue)
				{
					var queueFunction = (QueueMessageTriggeredFunctionDeclaration) function;
					var script = SetupNewScript();
					script.DoString(queueFunction.Code);
					var task = Task.Factory.StartNew(() =>
					{
						var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
						do
						{
							if (queue.TryDeque(queueFunction.QueueName, out QueueMessage message))
							{
								try
								{
									// call function
									script.Call(script.Globals["run"], message);
								}
								catch (Exception ex)
								{
									Debug.WriteLine(ex.Message);
									//todo log
								}
							}
						} while (true);
					});
				}
			}
		}

	    private Script SetupNewScript()
	    {
		    var registry = ServiceLocator.Current.GetInstance<IDeviceRegistry>();
		    var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();

		    var script = new Script(CoreModules.Preset_Complete);

		    var registryDynValue = UserData.Create(registry);
		    script.Globals.Set("registry", registryDynValue);
		    var messageQueueDynValue = UserData.Create(queue);
		    script.Globals.Set("queue", messageQueueDynValue);

		    script.Options.DebugPrint = s => { Debug.WriteLine(s); };
			return script;
	    }
    }
}
