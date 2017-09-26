using System.Collections.Generic;
using IoTHs.Plugin.ABUS.SecVest.Models;

namespace IoTHs.Plugin.ABUS.SecVest
{
	public class SecVestStatus
	{
		public string Name { get; set; }
		public List<SecVestPartition> Partitions { get; set; }
		public List<SecVestOutput> Outputs { get; set; }
	}
}