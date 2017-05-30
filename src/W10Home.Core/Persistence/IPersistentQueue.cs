using System;
using System.Collections.Generic;
using System.Text;

namespace W10Home.Core.Persistence
{
    public interface IPersistentQueue
    {
        void Enqueue(string queue, string message);
        bool TryDeque(string queue, out string message);
        bool TryPeek(string queue, out string message);
        bool IsEmpty(string queue);
    }
}
