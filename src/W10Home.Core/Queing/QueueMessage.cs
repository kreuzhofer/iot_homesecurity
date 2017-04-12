using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W10Home.Core.Queing
{
    public class QueueMessage
    {
		public QueueMessage() { }
		public QueueMessage(string key, string value, string tag)
		{
			this.Key = key;
			this.Value = value;
			this.Tag = tag;
		}

		public string Key { get; set; }
		public string Value { get; set; }
		public string Tag { get; set; }
		public override string ToString()
        {
		    return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

    }
}
