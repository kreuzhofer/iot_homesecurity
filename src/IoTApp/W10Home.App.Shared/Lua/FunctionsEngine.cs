using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using W10Home.App.Shared.Lua;

namespace W10Home.App.Shared
{
	internal class FunctionsEngine
    {
        private readonly ILogger _log = LogManagerFactory.DefaultLogManager.GetLogger<FunctionsEngine>();
        private readonly List<FunctionInstance> _functions = new List<FunctionInstance>();
	    public async void Initialize(DeviceConfigurationModel configuration, CancellationToken cancellationToken)
		{
			if (configuration.DeviceFunctionIds == null)
			{
				return;
			}
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
			foreach (var functionId in configuration.DeviceFunctionIds)
			{
			    var functionInstance = await SetupFunction(functionId);
			    if (functionInstance != null)
			    {
			        _functions.Add(functionInstance);
			    }
			}
            // setup message receiver loop
            MessageReceiverLoop(cancellationToken);
        }

        private async Task<FunctionInstance> SetupFunction(string functionId)
        {
            var functionInstance =
                new FunctionInstance(functionId)
                {
                    CancellationTokenSource = new CancellationTokenSource()
                };

            // load function definition from disk
            // first try to load the configuration file from the LocalFolder
            var localStorage = ApplicationData.Current.LocalFolder;
            var file = await localStorage.TryGetItemAsync("function_" + functionId + ".json");
            DeviceFunctionModel function = null;
            if (file != null) // file exists, continue to deserialize into actual configuration object
            {
                // local file content
                var configFileContent = await FileIO.ReadTextAsync((IStorageFile) file);
                function = JsonConvert.DeserializeObject<DeviceFunctionModel>(configFileContent);
                if (function == null)
                {
                    _log.Error("Invalid function file for function " + functionId);
                    return null;
                }
            }
            else
            {
                _log.Error("Function file not found for function " + functionId);
                return null;
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
                    return null;
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
                            _log.Error("Error running function " + function.Name, ex);
                        }
                    }
                }, null, function.Interval, function.Interval);
                functionInstance.Timer = timer;
                functionInstance.LuaScript = script;
            }
            else if (function.TriggerType == FunctionTriggerType.MessageQueue)
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
                    return null;
                }
                var task = Task.Factory.StartNew(async () =>
                {
                    var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
                    do
                    {
                        if (queue.TryDeque(function.QueueName, out QueueMessage message))
                        {
                            _log.Trace("Running message triggered function " + function.Name);
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
                        await Task.Delay(1, functionInstance.CancellationTokenSource.Token);
                    } while (!functionInstance.CancellationTokenSource.IsCancellationRequested);
                }, functionInstance.CancellationTokenSource.Token);
                functionInstance.LuaScript = script;
            }
            return functionInstance;
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

        private async Task ReloadFunction(string functionId)
        {
            _log.Trace("Reloading function "+functionId);
            if (_functions.Any(f => f.FunctionId == functionId))
            {
                _log.Trace("Unloading old instance of function " + functionId);
                var removeFunction = _functions.Single(f => f.FunctionId == functionId);
                _functions.Remove(removeFunction);
                removeFunction.CancellationTokenSource.Cancel();
                if (removeFunction.Timer != null)
                {
                    removeFunction.Timer.Dispose();
                }
            }
            await Task.Delay(250); // grace time
            var newFunction = await SetupFunction(functionId);
            if (newFunction != null)
            {
                _functions.Add(newFunction);
                _log.Trace("New version of function " + functionId + " started");
            }
            else
            {
                _log.Error("Error loading new version of function " + functionId);
            }
        }

        private async void MessageReceiverLoop(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    var queue = ServiceLocator.Current.GetInstance<IMessageQueue>();
                    if (queue.TryDeque("functionsengine", out QueueMessage queuemessage))
                    {
                        if (queuemessage.Key == "reloadfunction")
                        {
                            await ReloadFunction(queuemessage.Value);
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
    }
}
