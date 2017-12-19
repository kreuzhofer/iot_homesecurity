using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;

namespace W10Home.NetCoreDevicePortal.DataAccess.Services
{
    public class DeviceService : BaseTableService<DeviceEntity>, IDeviceService
    {
        public DeviceService(IConfiguration configuration) : base(configuration)
        {
        }

        public override async Task<DeviceEntity> GetAsync(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<DeviceEntity>(partitionKey, rowKey);
            var result = await TableRef.ExecuteAsync(operation);
            var entity = result.Result as DeviceEntity;
            if (entity != null && entity.Deleted)
                return null;
            return entity;
        }

        public override async Task<List<DeviceEntity>> GetAsync(string partitionKey)
        {
            var keyCondition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            var deletedNotTrueCondition = TableQuery.GenerateFilterConditionForBool("Deleted", QueryComparisons.NotEqual, true);
            var finalCondition = TableQuery.CombineFilters(keyCondition, TableOperators.And, deletedNotTrueCondition);
            TableQuery<DeviceEntity> query = new TableQuery<DeviceEntity>().Where(finalCondition);
            var result = await TableRef.ExecuteQuerySegmentedAsync<DeviceEntity>(query, null);
            return result.Results;
        }

        public async Task<DeviceEntity> GetWithApiKeyAsync(string deviceId, string apiKey)
        {
            var keyCondition = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, deviceId);
            var sharedKeyCondition = TableQuery.GenerateFilterCondition("ApiKey", QueryComparisons.Equal, apiKey);
            var finalCondition = TableQuery.CombineFilters(keyCondition, TableOperators.And, sharedKeyCondition);
            TableQuery<DeviceEntity> query = new TableQuery<DeviceEntity>().Where(finalCondition);
            var result = await TableRef.ExecuteQuerySegmentedAsync<DeviceEntity>(query, null);
            return result.Results.FirstOrDefault();
        }
    }
}