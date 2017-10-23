﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using W10Home.Interfaces;
using W10Home.Interfaces.Configuration;
using IoTHs.Api.Shared;
using IoTHs.Devices.Interfaces;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using IoTHs.Core;
using IoTHs.Core.Queing;
using IoTHs.Plugin.AzureIoTHub;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using W10Home.App.Shared.Lua;
using W10Home.Core;

namespace W10Home.App.Shared
{
	internal class FunctionsEngine
    {
        private readonly ILogger _log;
        private readonly List<FunctionInstance> _functions = new List<FunctionInstance>();
        CancellationTokenSource _cancellationTokenSource;
        private ILoggerFactory _loggerFactory;

        public FunctionsEngine(ILoggerFactory loggerFactory)
        {
            UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
            _log = loggerFactory.CreateLogger<FunctionsEngine>();
            _loggerFactory = loggerFactory;
        }

        public async void Initialize(DeviceConfigurationModel configuration)
		{
			if (configuration.DeviceFunctionIds == null)
			{
				return;
			}
			foreach (var functionId in configuration.DeviceFunctionIds)
			{
			    var functionInstance = await SetupFunction(functionId);
			    if (functionInstance != null)
			    {
			        _functions.Add(functionInstance);
			    }
			}
            _cancellationTokenSource = new CancellationTokenSource();
            // setup message receiver loop
            MessageReceiverLoop(_cancellationTokenSource.Token);
        }

        public void Shutdown()
        {
            _log.LogTrace("Shutdown functions engine");
            _cancellationTokenSource.Cancel();
            foreach (var functionInstance in _functions.ToList())
            {
                UnloadFunction(functionInstance.FunctionId);
            }
        }

        private async Task<FunctionInstance> SetupFunction(string functionId)
        {
            var functionInstance =
                new FunctionInstance(functionId)
                {
                    CancellationTokenSource = new CancellationTokenSource()
                };

            // load function definition from disk
            var function = await LoadFunctionFromStorageAsync(functionId);
            if (function == null)
            {
                return null;
            }

            if (!function.Enabled)
            {
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
                    _log.LogError(ex, "Error compiling script " + function.Name);
                    return null;
                }
                var timer = new Timer(state =>
                {
                    lock (script)
                    {
                        _log.LogTrace("Running timer triggered function " + function.Name);
                        try
                        {
                            script.Call(script.Globals["run"]);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "Error running function " + function.Name);
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
                    _log.LogError(ex, "Error compiling script " + function.Name);
                    return null;
                }
                var task = Task.Factory.StartNew(async () =>
                {
                    var queue = ServiceLocator.Current.GetService<IMessageQueue>();
                    do
                    {
                        if (queue.TryDeque(function.QueueName, out QueueMessage message))
                        {
                            _log.LogTrace("Running message triggered function " + function.Name);
                            try
                            {
                                // call function
                                script.Call(script.Globals["run"], message);
                            }
                            catch (Exception ex)
                            {
                                _log.LogError(ex, "Error running function " + function.Name);
                            }
                        }
                        await Task.Delay(IoTHsConstants.MessageLoopDelay, functionInstance.CancellationTokenSource.Token);
                    } while (!functionInstance.CancellationTokenSource.IsCancellationRequested);
                }, functionInstance.CancellationTokenSource.Token);
                functionInstance.LuaScript = script;
            }
            return functionInstance;
        }

        private async Task<DeviceFunctionModel> LoadFunctionFromStorageAsync(string functionId)
        {
            // first try to load the function file from the LocalFolder
            var localStorage = ApplicationData.Current.LocalFolder;
            var file = await localStorage.TryGetItemAsync("function_" + functionId + ".json");
            DeviceFunctionModel function = null;
            if (file != null) // file exists, continue to deserialize into actual object
            {
                // local file content
                var configFileContent = await FileIO.ReadTextAsync((IStorageFile) file);
                function = JsonConvert.DeserializeObject<DeviceFunctionModel>(configFileContent);
                if (function == null)
                {
                    _log.LogError("Invalid function file for function " + functionId);
                    return function;
                }
            }
            else
            {
                _log.LogError("Function file not found for function " + functionId);
                return function;
            }
            return function;
        }

        private Script SetupNewLuaScript(string name)
	    {
		    var registry = ServiceLocator.Current.GetService<IDeviceRegistry>();
		    var queue = ServiceLocator.Current.GetService<IMessageQueue>();

		    var script = new Script(CoreModules.Preset_Complete);

		    var registryDynValue = UserData.Create(registry);
		    script.Globals.Set("registry", registryDynValue);
		    var messageQueueDynValue = UserData.Create(queue);
		    script.Globals.Set("queue", messageQueueDynValue);
	        var log = _loggerFactory.CreateLogger("FunctionsEngine|" + name);
	        var logDynValue = UserData.Create(log);
            script.Globals.Set("log", logDynValue);

		    script.Options.DebugPrint = s => { log.LogDebug(s); };
			return script;
	    }

        private async Task ReloadFunction(string functionId)
        {
            _log.LogTrace("Reloading function "+functionId);
            UnloadFunction(functionId);
            await Task.Delay(250); // grace time
            var newFunction = await SetupFunction(functionId);
            if (newFunction != null)
            {
                _functions.Add(newFunction);
                _log.LogTrace("New version of function " + functionId + " started");
            }
            else
            {
                _log.LogError("Error loading new version of function " + functionId);
            }
        }

        private void UnloadFunction(string functionId)
        {
            if (_functions.Any(f => f.FunctionId == functionId))
            {
                _log.LogTrace("Unloading old instance of function " + functionId);
                var removeFunction = _functions.Single(f => f.FunctionId == functionId);
                _functions.Remove(removeFunction);
                if (removeFunction.Timer != null)
                {
                    removeFunction.Timer.Dispose();
                }
                removeFunction.CancellationTokenSource.Cancel();
            }
        }

        private async void MessageReceiverLoop(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    var queue = ServiceLocator.Current.GetService<IMessageQueue>();
                    if (queue.TryDeque("functionsengine", out QueueMessage queuemessage))
                    {
                        if (queuemessage.Key == "reloadfunction")
                        {
                            await ReloadFunction(queuemessage.Value);
                        }
                        if (queuemessage.Key == "checkversionsandupdate")
                        {
                            await CheckVersionsAndUpdateAsync(queuemessage.Value);
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

        private async Task CheckVersionsAndUpdateAsync(string functionsAndVersions)
        {
            string[] functionVersionPairs = functionsAndVersions.Split(',');
            var configuration = ServiceLocator.Current.GetService<DeviceConfigurationModel>();
            var baseUrl = configuration.ServiceBaseUrl;
            var deviceId = configuration.DeviceId;

            var apiKey = ServiceLocator.Current.GetService<IDeviceRegistry>().GetDevice<AzureIoTHubDevice>("iothub").ApiKey;

            foreach (var functionVersionPair in functionVersionPairs)
            {
                var functionId = functionVersionPair.Split(':')[0];
                var functionVersion = int.Parse(functionVersionPair.Split(':')[1]);

                var localFunctionVersion = await LoadFunctionFromStorageAsync(functionId);
                if (localFunctionVersion == null || (localFunctionVersion.Version<functionVersion))
                {
                    // download function code from webserver
                    var aHBPF = new HttpBaseProtocolFilter();
                    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
                    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                    aHBPF.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
                    var client = new HttpClient(aHBPF);
                    client.DefaultRequestHeaders.Add("apikey", apiKey);
                    var functionContent = await client.GetStringAsync(new Uri(baseUrl + "DeviceFunction/" + deviceId + "/" + functionId));
                    // store function file to disk
                    var localStorage = ApplicationData.Current.LocalFolder;
                    string filename = "function_" + functionId + ".json";
                    var file = await localStorage.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(file, functionContent);

                    // (re)load function
                    await ReloadFunction(functionId);
                }
            }
        }
    }
}