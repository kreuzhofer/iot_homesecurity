using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.DevicePortal.Models;

namespace W10Home.NetCoreDevicePortal.DataAccess
{
	public class DeviceStateService : IDeviceStateService
	{
		private readonly CloudTable _deviceStateTableRef;

		public DeviceStateService(IConfiguration configuration)
		{
			var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
			var tableClient = CloudStorageAccount.Parse(connection).CreateCloudTableClient();
			_deviceStateTableRef = tableClient.GetTableReference("DeviceState");
			_deviceStateTableRef.CreateIfNotExistsAsync();
		}

		public async Task<DeviceStateEntity> GetDeviceState(string deviceId, string channelKey)
		{
			var operation = TableOperation.Retrieve<DeviceStateEntity>(deviceId, channelKey);
			var result = await _deviceStateTableRef.ExecuteAsync(operation);
			return result.Result as DeviceStateEntity;
		}

		public async Task<List<DeviceStateEntity>> GetDeviceState(string deviceId)
		{
			TableQuery<DeviceStateEntity> query = new TableQuery<DeviceStateEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, deviceId));
			var result = await _deviceStateTableRef.ExecuteQuerySegmentedAsync<DeviceStateEntity>(query, null);
			return result.Results;
		}

	}
}