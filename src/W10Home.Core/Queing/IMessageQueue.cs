using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W10Home.Core.Queing
{
    public interface IMessageQueue
    {
        void Enqueue(string queue, QueueMessage message);
	    void Enqueue(string queue, string key, object value);
        bool TryDeque(string queue, out QueueMessage message);
		bool TryPeek(string queue, out QueueMessage message);
		bool IsEmpty(string queue);
    }
}
