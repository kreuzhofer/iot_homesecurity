using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using W10Home.Interfaces;
using W10Home.Plugin.ABUS.SecVest.Models;
using W10Home.Plugin.ABUS.SecVest.Utils;

namespace W10Home.Plugin.ABUS.SecVest
{
	public class SecVestStatusChannel : SecVestChannel
	{
		public SecVestStatusChannel(HttpClient client) : base(client)
		{
		}

		public override string Name => "status";

		public override bool IsRead => true;

		public override bool IsWrite => false;

		public override ChannelType ChannelType => ChannelType.None;

		public async Task<SecVestStatus> GetStatusAsync()
		{
			var system = await GetSystem();
			Debug.WriteLine(system.Name);
			return new SecVestStatus();
		}

		private async Task<SecVestSystem> GetSystem()
		{
			return await GetRestResultAsync<SecVestSystem>("system/");
		}

		private async Task<SecVestPartition> GetPartitionAsync(string partitionNumber)
		{
			return await GetRestResultAsync<SecVestPartition>("system/partitions-" + partitionNumber + "/");
		}

		private async Task<T> GetRestResultAsync<T>(string query) where T : new()
		{
			T partition = default(T);

			HttpResponseMessage response = await _client.GetAsync(query);
			if (response.IsSuccessStatusCode)
			{
				partition = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
			}
			return partition;
		}
	}
}
