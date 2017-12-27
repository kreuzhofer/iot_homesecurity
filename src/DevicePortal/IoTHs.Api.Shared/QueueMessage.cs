using System;
using Newtonsoft.Json;

namespace IoTHs.Api.Shared
{
    public class QueueMessage
    {
        public static QueueMessage Create()
        {
            return new QueueMessage();
        }
        public QueueMessage() { }
		public QueueMessage(string key, string value, string tag)
		{
			this.Key = key;
			this.Value = value;
			this.Tag = tag;
		    this.Timestamp = DateTime.UtcNow;
		}

        public string Key { get; set; }
		public string Value { get; set; }
		public string Tag { get; set; }
        public DateTime Timestamp { get; set; }
		public override string ToString()
        {
		    return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

    }
}
