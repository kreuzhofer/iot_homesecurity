using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetroLog;
using Microsoft.Practices.ServiceLocation;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using W10Home.Core.Configuration;
using W10Home.Core.Queing;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using Windows.Storage;
using Newtonsoft.Json;

namespace W10Home.App.Shared
{
	internal class FunctionsEngine
    {
		private readonly List<Timer> _timers = new List<Timer>();
        private readonly ILogger _log = LogManagerFactory.DefaultLogManager.GetLogger<FunctionsEngine>();

	    public async void Initialize(DeviceConfigurationModel configuration)
		{
			if (configuration.DeviceFunctionIds == null)
			{
				return;
			}
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
			foreach (var functionId in configuration.DeviceFunctionIds)
			{
                // load function definition from disk
			    // first try to load the configuration file from the LocalFolder
			    var localStorage = ApplicationData.Current.LocalFolder;
			    var file = await localStorage.TryGetItemAsync("function_"+functionId+".json");
			    DeviceFunctionModel function = null;
			    if (file != null) // file exists, continue to deserialize into actual configuration object
			    {
			        // local file content
			        var configFileContent = await FileIO.ReadTextAsync((IStorageFile)file);
			        function = JsonConvert.DeserializeObject<DeviceFunctionModel>(configFileContent);
			    }

                if (function.TriggerType == FunctionTriggerType.RecurringIntervalTimer)
				{
					var script = SetupNewLuaScript(function.Name);
                    // try to compile script
				    try
				    {
				        script.DoString(function.Script);
				    }
				    catch (Exception ex)
				    {
				        _log.Error("Error compiling script " + function.Name, ex);
				        continue; // todo set twin property to report compilation error
				    }
				    var timer = new Timer(state =>
					{
						lock (script)
						{
						    _log.Trace("Running timer triggered function " + function.Name);
							try
                            {
								script.Call(script.Globals["run"]);
							}
							catch (Exception ex)
							{
							    _log.Error("Error running function "+function.Name, ex);
							}						}
					}, null , function.Interval, function.Interval);
					_timers.Add(timer);
				}
				else if (function.TriggerType == FunctionTriggerType.MessageQueue)
				{
					var script = SetupNewLuaScript(function.Name);
					script.DoString(function.Script);
					var task = Task.Factory.StartNew(async () =>
					{
						var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
						do
						{
							if (queue.TryDeque(function.QueueName, out QueueMessage message))
							{
                                _log.Trace("Running message triggered function "+function.Name);
								try
								{
									// call function
									script.Call(script.Globals["run"], message);
								}
								catch (Exception ex)
								{
								    _log.Error("Error running function " + function.Name, ex);
                                }
							}
						    await Task.Delay(1);
						} while (true);
					});
				}
			}
		}

	    private Script SetupNewLuaScript(string name)
	    {
		    var registry = ServiceLocator.Current.GetInstance<IDeviceRegistry>();
		    var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();

		    var script = new Script(CoreModules.Preset_Complete);

		    var registryDynValue = UserData.Create(registry);
		    script.Globals.Set("registry", registryDynValue);
		    var messageQueueDynValue = UserData.Create(queue);
		    script.Globals.Set("queue", messageQueueDynValue);
	        var log = LogManagerFactory.DefaultLogManager.GetLogger("FunctionsEngine|" + name);
	        var logDynValue = UserData.Create(log);
            script.Globals.Set("log", logDynValue);

		    script.Options.DebugPrint = s => { log.Debug(s); };
			return script;
	    }
    }
}
