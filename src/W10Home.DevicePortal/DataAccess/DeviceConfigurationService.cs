using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.DevicePortal.Models;

namespace W10Home.DevicePortal.DataAccess
{
	public class DeviceConfigurationService
	{
		private readonly CloudTable _deviceConfigTableRef;

		public DeviceConfigurationService()
		{
			var connection = CloudConfigurationManager.GetSetting("DevicePortalStorageAccount");
			var tableClient = CloudStorageAccount.Parse(connection).CreateCloudTableClient();
			_deviceConfigTableRef = tableClient.GetTableReference("DeviceConfiguration");
			_deviceConfigTableRef.CreateIfNotExists();
		}

		public async Task SaveConfig(string deviceId, string configurationKey, string configurationJson)
		{
			var entity = new DeviceConfigurationEntity()
			{
				PartitionKey = deviceId,
				RowKey = configurationKey,
				Configuration = configurationJson
			};
			var operation = TableOperation.InsertOrReplace(entity);
			await _deviceConfigTableRef.ExecuteAsync(operation);
		}

		public async Task<DeviceConfigurationEntity> LoadConfig(string deviceId, string configurationKey)
		{
			var operation = TableOperation.Retrieve<DeviceConfigurationEntity>(deviceId, configurationKey);
			var result = await _deviceConfigTableRef.ExecuteAsync(operation);
			return result.Result as DeviceConfigurationEntity;
		}
	}
}