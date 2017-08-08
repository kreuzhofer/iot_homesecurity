using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace W10Home.App.Shared.Lua
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
