using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json;
using W10Home.Interfaces;
using W10Home.Plugin.ABUS.SecVest.Models;
using W10Home.Plugin.ABUS.SecVest.Utils;

namespace W10Home.Plugin.ABUS.SecVest
{
	public class SecVestStatusChannel : SecVestChannel
	{
		public SecVestStatusChannel(HttpClient client, string baseUrl) : base(client, baseUrl)
		{
		}

		public override string Name => "status";

		public override bool IsRead => true;

		public override bool IsWrite => false;

		public override ChannelType ChannelType => ChannelType.None;

		public override object Read()
		{
			try
			{
				var statusTask = GetStatusAsync();
				Task.WaitAll(statusTask);
				return statusTask.Result;

			}
			catch (Exception ex)
			{
				//TODO log
				Debug.WriteLine(ex.Message);
				return null;
			}		}

		public override void Write(object value)
		{
			throw new NotImplementedException();
		}

		private async Task<SecVestStatus> GetStatusAsync()
		{
			var system = await GetSystem();
			var partitions = new List<SecVestPartition>();
			foreach (var systemPartition in system.Partitions)
			{
				var partition = await GetPartitionAsync(systemPartition);
				partitions.Add(partition);
			}
			Debug.WriteLine(system.Name);
			return new SecVestStatus()
			{
				Name = system.Name,
				Partitions = partitions
			};
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

			HttpResponseMessage response = await _client.GetAsync(new Uri(_baseUrl + query));
			if (response.IsSuccessStatusCode)
			{
				partition = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
			}
			return partition;
		}


	}
}
