namespace IoTHs.Plugin.ABUS.SecVest.Models
{
    public class SecVestPartition
    {
        public string id { get; set; }

        public string state { get; set; }
        public string name { get; set; }
        public string[] zones { get; set; }

        public SecVestPartition(string id, string state, string name, string[] zones)
        {
            this.id = id;
            this.state = state;
            this.name = name;
            this.zones = zones;
        }

        public SecVestPartition() { }
    }
}
