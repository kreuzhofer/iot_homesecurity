using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IoTHs.Devices.Interfaces;
using IoTHs.Plugin.ABUS.SecVest.Models;
using Newtonsoft.Json;

namespace IoTHs.Plugin.ABUS.SecVest
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
			}
		}

		public override void Write(object value)
		{
			throw new NotImplementedException();
		}

		public SecVestOutput SetOutput(string outputId, string value)
		{
			try
			{
				var setTask = SetOutputAsync(outputId, value);
				Task.WaitAll(setTask);
				return setTask.Result;
			}
			catch (Exception ex)
			{
				// TODO log
				Debug.WriteLine(ex.Message);
				return null;
			}
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
			await Task.Delay(5000); // give secvest some time :-)
			var outputs = await GetOutputsAsync();
			Debug.WriteLine(system.Name);
			return new SecVestStatus()
			{
				Name = system.Name,
				Partitions = partitions,
				Outputs = outputs
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

		private async Task<List<SecVestOutput>> GetOutputsAsync()
		{
			return await GetRestResultAsync<List<SecVestOutput>>("outputs/");
		}

		private async Task<SecVestOutput> GetOutputAsync(string outputId)
		{
			return await GetRestResultAsync<SecVestOutput>("outputs-" + outputId + "/");
		}

		private async Task<SecVestOutput> SetOutputAsync(string outputId, string value)
		{
			return await PutAsync<SecVestOutput>("outputs-" + outputId + "/", 
@"{ 
	""state"" : """ + value + @""",
}");
		}

		private async Task<T> GetRestResultAsync<T>(string query) where T : new()
		{
			T result = default(T);

			HttpResponseMessage response = await _client.GetAsync(new Uri(_baseUrl + query));
			if (response.IsSuccessStatusCode)
			{
				result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
			}
			return result;
		}

		private async Task<T> PutAsync<T>(string query, string body)
		{
			T result = default(T);

			var content = new StringContent(body, Encoding.UTF8, "application/json");
			var uri = new Uri(_baseUrl + query);
			HttpResponseMessage response = await _client.PutAsync(uri, content);
			if (response.IsSuccessStatusCode)
			{
				result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
			}
			return result;
		}


	}
}
