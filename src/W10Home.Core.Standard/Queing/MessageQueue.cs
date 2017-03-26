using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W10Home.Core.Queing
{
    public class MessageQueue : IMessageQueue
    {
        private ConcurrentDictionary<string, ConcurrentQueue<QueueMessage>> _queues = new ConcurrentDictionary<string, ConcurrentQueue<QueueMessage>>();

        public void Enqueue(string queue, QueueMessage message)
        {
            if(!_queues.ContainsKey(queue))
            {
                _queues.TryAdd(queue, new ConcurrentQueue<QueueMessage>());
            }
            _queues[queue].Enqueue(message);
        }

        public bool TryDeque(string queue, out QueueMessage message)
        {
            if(!_queues.ContainsKey(queue))
            {
                message = null;
                return false;
            }
            return _queues[queue].TryDequeue(out message);
        }

        public bool IsEmpty(string queue)
        {
            if(!_queues.ContainsKey(queue))
            {
                return false;
            }
            return _queues[queue].IsEmpty;
        }
    }
}
