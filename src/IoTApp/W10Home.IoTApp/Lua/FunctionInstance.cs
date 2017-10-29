using System.Threading;
using MoonSharp.Interpreter;

namespace W10Home.IoTCoreApp.Lua
{
    internal class FunctionInstance
    {
        public FunctionInstance(string functionId)
        {
            FunctionId = functionId;
        }

        public string FunctionId { get; set; }

        public Script LuaScript { get; set; }
        public Timer Timer { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }

    }
}
