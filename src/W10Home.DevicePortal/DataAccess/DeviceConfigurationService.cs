using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.DevicePortal.Models;

namespace W10Home.DevicePortal.DataAccess
{
	public class DeviceConfigurationService
	{
		private readonly CloudTableClient _tableClient;
		private readonly CloudTable _deviceConfigTableRef;

		public DeviceConfigurationService()
		{
			var connection = CloudConfigurationManager.GetSetting("DevicePortalStorageAccount");
			_tableClient = CloudStorageAccount.Parse(connection).CreateCloudTableClient();
			_deviceConfigTableRef = _tableClient.GetTableReference("DeviceConfiguration");
			_deviceConfigTableRef.CreateIfNotExists();
		}

		public async Task SaveConfig(string accountId, string deviceId, string configurationJson)
		{
			var entity = new DeviceConfigurationEntity()
			{
				PartitionKey = accountId,
				RowKey = deviceId,
				Configuration = configurationJson
			};
			var operation = TableOperation.InsertOrReplace(entity);
			await _deviceConfigTableRef.ExecuteAsync(operation);
		}

		public async Task<DeviceConfigurationEntity> LoadConfig(string accountId, string deviceId)
		{
			var operation = TableOperation.Retrieve<DeviceConfigurationEntity>(accountId, deviceId);
			var result = await _deviceConfigTableRef.ExecuteAsync(operation);
			return result.Result as DeviceConfigurationEntity;
		}
	}
}