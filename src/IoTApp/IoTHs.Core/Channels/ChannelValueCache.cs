using System.Collections.Generic;

namespace IoTHs.Core.Channels
{
    public class ChannelValueCache
    {
        private Dictionary<string, string> _cache = new Dictionary<string, string>();

        public void Set(string key, string value)
        {
            if (_cache.ContainsKey(key))
            {
                _cache[key] = value;
            }
            else
            {
                _cache.Add(key, value);
            }
        }

        public string Get(string key)
        {
            if (_cache.ContainsKey(key))
            {
                return _cache[key];
            }
            else
            {
                return null;
            }
        }
    }
}