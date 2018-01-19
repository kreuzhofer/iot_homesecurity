using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using IoTHs.Api.Shared.CronJobs;
using IoTHs.Core.Http;
using IoTHs.Core.Queing;
using IoTHs.Devices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Newtonsoft.Json;

namespace IoTHs.Core.Lua
{
    public class FunctionsEngine
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
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
            foreach (var functionInstance in _functions.ToList())
            {
                UnloadFunction(functionInstance.FunctionId);
            }
        }

        public IEnumerable<FunctionInstance> Functions
        {
            get { return _functions; }
        }

        /// <summary>
        /// Loads the given function from disk and creates the function object depending on the function type
        /// </summary>
        /// <param name="functionId">id of the function to load.</param>
        /// <returns>a <see cref="FunctionInstance"/> instance</returns>
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
            functionInstance.Name = function.Name;

            if (!function.Enabled)
            {
                return null;
            }

            // Setup script logger and compile script to find any syntax errors upfront
            ILogger scriptLogger;
            var script = SetupNewLuaScript(function.Name, functionId, out scriptLogger);
            // try to compile script
            try
            {
                script.DoString(function.Script);
            }
            catch (SyntaxErrorException ex)
            {
                scriptLogger.LogError(ex, "Syntax:"+ex.DecoratedMessage);
                return null;
            }
            catch (Exception ex)
            {
                scriptLogger.LogError(ex, "Error compiling script " + function.Name);
                return null;
            }
            functionInstance.LuaScript = script;

            if (function.TriggerType == FunctionTriggerType.RecurringIntervalTimer)
            {
                var timer = new Timer(state =>
                {
                    lock (script)
                    {
                        scriptLogger.LogTrace("Running timer triggered function " + function.Name);
                        try
                        {
                            script.Call(script.Globals["run"]);
                        }
                        catch (Exception ex)
                        {
                            scriptLogger.LogError(ex, "Error running function " + function.Name);
                        }
                    }
                }, null, function.Interval, function.Interval);
                functionInstance.Timer = timer;
            }
            else if (function.TriggerType == FunctionTriggerType.CronSchedule)
            {
                functionInstance.LastMinute = DateTime.Now;
                functionInstance.IsRunning = false;
                functionInstance.CronSchedule = new CronSchedule(function.CronSchedule);
                var timer = new Timer(state =>
                {
                    var func = (FunctionInstance) state;
                    var newMinute = DateTime.Now;
                    if (newMinute.Minute != func.LastMinute.Minute && func.CronSchedule.IsTime(newMinute))
                    {
                        func.LastMinute = newMinute;
                        if (!func.IsRunning)
                        {
                            func.IsRunning = true;
                            scriptLogger.LogTrace("Running cronschedule triggered function " + function.Name);
                            try
                            {
                                script.Call(script.Globals["run"]);
                            }
                            catch (Exception ex)
                            {
                                scriptLogger.LogError(ex, "Error running function " + function.Name);
                            }
                            finally
                            {
                                func.IsRunning = false;
                            }
                        }
                        else
                        {
                            scriptLogger.LogWarning("Still running ronschedule triggered function " + function.Name+ ". Better check your schedule.");
                        }
                    }
                }, functionInstance, 30000, 30000);
                functionInstance.Timer = timer;
            }
            else if (function.TriggerType == FunctionTriggerType.MessageQueue)
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    var queue = ServiceLocator.Current.GetService<IMessageQueue>();
                    do
                    {
                        if (queue.TryDeque(function.QueueName, out QueueMessage message))
                        {
                            scriptLogger.LogTrace("Running message triggered function " + function.Name);
                            try
                            {
                                // call function
                                script.Call(script.Globals["run"], message);
                            }
                            catch (Exception ex)
                            {
                                scriptLogger.LogError(ex, "Error running function " + function.Name);
                            }
                        }
                        await Task.Delay(IoTHsConstants.MessageLoopDelay, functionInstance.CancellationTokenSource.Token);
                    } while (!functionInstance.CancellationTokenSource.IsCancellationRequested);
                }, functionInstance.CancellationTokenSource.Token);
            }
            return functionInstance;
        }

        private async Task<DeviceFunctionModel> LoadFunctionFromStorageAsync(string functionId)
        {
            // first try to load the function file from the LocalFolder
            
            var localStorage = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var filePath = Path.Combine(localStorage, "function_" + functionId + ".json");
            DeviceFunctionModel function = null;
            if (File.Exists(filePath)) // file exists, continue to deserialize into actual object
            {
                // local file content
                var configFileContent = File.ReadAllText(filePath);
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

        private Script SetupNewLuaScript(string name, string functionId, out ILogger scriptLogger)
	    {
		    var registry = ServiceLocator.Current.GetService<IDeviceRegistry>();
		    var queue = ServiceLocator.Current.GetService<IMessageQueue>();

		    var script = new Script(CoreModules.Preset_Complete);

		    var registryDynValue = UserData.Create(registry);
		    script.Globals.Set("registry", registryDynValue);
		    var messageQueueDynValue = UserData.Create(queue);
		    script.Globals.Set("queue", messageQueueDynValue);
	        var log = _loggerFactory.CreateLogger("DeviceFunction." + functionId);
	        var logDynValue = UserData.Create(log);
            script.Globals.Set("log", logDynValue);
            script.Globals.Set("DateTime", UserData.Create(new DateTime()));

		    script.Options.DebugPrint = s => { log.LogDebug(s); };

            script.Globals.Set("QueueMessage", UserData.Create(new QueueMessage()));
            scriptLogger = log;
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
            var iotHub = ServiceLocator.Current.GetService<IAzureIoTHubPlugin>();
            var baseUrl = iotHub.ServiceBaseUrl;
            var deviceId = iotHub.DeviceId;

            var apiKey = ServiceLocator.Current.GetService<IDeviceRegistry>().GetDevice<IAzureIoTHubPlugin>("iothub").ApiKey;

            // create client token
            var tokenClient = new LocalHttpClient();
            var tokenRequestUrl = baseUrl + "ApiAuthentication/";
            _log.LogDebug("Get api token");
            tokenClient.Client.DefaultRequestHeaders.Add("apikey", apiKey);
            tokenClient.Client.DefaultRequestHeaders.Add("deviceid", deviceId);
            var tokenResponse = await tokenClient.Client.PostAsync(new Uri(tokenRequestUrl), null);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException(tokenResponse.ReasonPhrase);
            }
            // get token from response
            var tokenReponseContent = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenJsonObj = JsonConvert.DeserializeObject(tokenReponseContent);
            string token = tokenJsonObj.token;

            foreach (var functionVersionPair in functionVersionPairs)
            {
                var functionId = functionVersionPair.Split(':')[0];
                var functionVersion = int.Parse(functionVersionPair.Split(':')[1]);

                var localFunctionVersion = await LoadFunctionFromStorageAsync(functionId);
                if (localFunctionVersion == null || (localFunctionVersion.Version<functionVersion))
                {
                    // download function code from webserver
                    var client = new LocalHttpClient();
                    client.Client.DefaultRequestHeaders.Add("Authorization", "Bearer "+token);
                    var functionContent = await client.Client.GetStringAsync(new Uri(baseUrl + "DeviceFunction/" + deviceId + "/" + functionId));
                    // store function file to disk
                    var localStorage = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var filePath = Path.Combine(localStorage, "function_" + functionId + ".json");
                    File.WriteAllText(filePath, functionContent);

                    // (re)load function
                    await ReloadFunction(functionId);
                }
            }
        }
    }
}
