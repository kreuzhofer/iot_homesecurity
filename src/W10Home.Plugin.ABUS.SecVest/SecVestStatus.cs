using System.Collections.Generic;
using W10Home.Plugin.ABUS.SecVest.Models;

namespace W10Home.Plugin.ABUS.SecVest
{
	public class SecVestStatus
	{
		public string Name { get; set; }
		public List<SecVestPartition> Partitions { get; set; }
	}
}