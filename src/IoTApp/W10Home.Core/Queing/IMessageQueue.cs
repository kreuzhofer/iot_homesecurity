using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoTHs.Api.Shared;

namespace W10Home.Core.Queing
{
    public interface IMessageQueue
    {
        void Enqueue(string queue, QueueMessage message);
	    void Enqueue(string queue, string key, object value, string tag);
		void Enqueue(string queue, string key, string value, string tag = null);
		bool TryDeque(string queue, out QueueMessage message);
		bool TryPeek(string queue, out QueueMessage message);
		bool IsEmpty(string queue);
    }
}
