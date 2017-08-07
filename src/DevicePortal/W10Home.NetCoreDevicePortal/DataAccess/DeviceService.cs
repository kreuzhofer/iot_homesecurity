using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.Models;

namespace W10Home.NetCoreDevicePortal.DataAccess
{
    public class DeviceService : IDeviceService
    {
        private CloudTable _deviceTableRef;

        public DeviceService(IConfiguration configuration)
        {
            var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
            var tableClient = CloudStorageAccount.Parse(connection).CreateCloudTableClient();
            _deviceTableRef = tableClient.GetTableReference("Device");
            _deviceTableRef.CreateIfNotExistsAsync();
        }

        public async Task<IEnumerable<DeviceEntity>> GetDevicesForUserAsync(string userId)
        {
            TableQuery<DeviceEntity> query = new TableQuery<DeviceEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId));
            var result = await _deviceTableRef.ExecuteQuerySegmentedAsync<DeviceEntity>(query, null);
            return result.Results;
        }

        public async Task<DeviceEntity> GetDevice(string userId, string deviceId)
        {
            var operation = TableOperation.Retrieve<DeviceEntity>(userId, deviceId);
            var result = await _deviceTableRef.ExecuteAsync(operation);
            return result.Result as DeviceEntity;
        }
    }
}