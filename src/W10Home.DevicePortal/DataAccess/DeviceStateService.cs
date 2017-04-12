using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.DevicePortal.Models;

namespace W10Home.DevicePortal.DataAccess
{
	public class DeviceStateService
	{
		private readonly CloudTable _deviceStateTableRef;

		public DeviceStateService()
		{
			var connection = CloudConfigurationManager.GetSetting("DevicePortalStorageAccount");
			var tableClient = CloudStorageAccount.Parse(connection).CreateCloudTableClient();
			_deviceStateTableRef = tableClient.GetTableReference("DeviceState");
			_deviceStateTableRef.CreateIfNotExists();
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
			var result = _deviceStateTableRef.ExecuteQuery(query);
			return result.ToList();
		}

	}
}