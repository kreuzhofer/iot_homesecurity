using System;
using System.Threading;
using IoTHs.Api.Shared.CronJobs;
using MoonSharp.Interpreter;

namespace IoTHs.Core.Lua
{
    public class FunctionInstance
    {
        public FunctionInstance(string functionId)
        {
            FunctionId = functionId;
        }

        public string FunctionId { get; set; }
        public string Name { get; set; }

        public Script LuaScript { get; set; }
        public Timer Timer { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public CronSchedule CronSchedule { get; set; }
        public DateTime LastMinute { get; set; }
        public bool IsRunning { get; set; }
    }
}
