using IoTHs.Api.Shared;

namespace IoTHs.Core.Queing
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
