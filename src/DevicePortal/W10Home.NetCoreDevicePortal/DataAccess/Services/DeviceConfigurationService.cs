using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.DevicePortal.DataAccess;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;

namespace W10Home.NetCoreDevicePortal.DataAccess.Services
{
	public class DeviceConfigurationService : IDeviceConfigurationService
    {
		private readonly CloudTable _deviceConfigTableRef;

		public DeviceConfigurationService(IConfiguration configuration)
		{
		    var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
            var tableClient = CloudStorageAccount.Parse(connection).CreateCloudTableClient();
			_deviceConfigTableRef = tableClient.GetTableReference("DeviceConfiguration");
			_deviceConfigTableRef.CreateIfNotExistsAsync();
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