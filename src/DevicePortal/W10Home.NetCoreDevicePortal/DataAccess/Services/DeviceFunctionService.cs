using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;

namespace W10Home.NetCoreDevicePortal.DataAccess.Services
{
    public class DeviceFunctionService : IDeviceFunctionService
    {
        private CloudTable _scriptTableRef;

        public DeviceFunctionService(IConfiguration configuration)
        {
            var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
            var tableClient = CloudStorageAccount.Parse(connection).CreateCloudTableClient();
            _scriptTableRef = tableClient.GetTableReference("DeviceFunction");
            _scriptTableRef.CreateIfNotExistsAsync();
        }

        public async Task SaveFunctionAsync(string deviceId, string functionId, string functionName, string triggerType, int interval, string queueName, bool enabled, string scriptContent)
        {
            DeviceFunctionEntity entity;
            entity = await GetFunctionAsync(deviceId, functionId);
            if (entity == null)
            {
                entity = new DeviceFunctionEntity();
            }
            entity.Name = functionName;
            entity.PartitionKey = deviceId;
            entity.RowKey = functionId;
            entity.Language = "Lua";
            entity.Script = scriptContent;
            entity.Interval = interval;
            entity.QueueName = queueName;
            entity.TriggerType = triggerType;
            entity.Enabled = enabled;
            entity.Version++;
            var operation = TableOperation.InsertOrReplace(entity);
            var result = await _scriptTableRef.ExecuteAsync(operation);
        }

        public async Task<DeviceFunctionEntity> GetFunctionAsync(string deviceId, string functionId)
        {
            var operation = TableOperation.Retrieve<DeviceFunctionEntity>(deviceId, functionId);
            var result = await _scriptTableRef.ExecuteAsync(operation);
            return result.Result as DeviceFunctionEntity;
        }

        public async Task<List<DeviceFunctionEntity>> GetFunctionsAsync(string deviceId)
        {
            TableQuery<DeviceFunctionEntity> query = new TableQuery<DeviceFunctionEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, deviceId));
            var result = await _scriptTableRef.ExecuteQuerySegmentedAsync<DeviceFunctionEntity>(query, null);
            return result.Results;
        }

        public async Task DeleteFunctionAsync(string deviceId, string functionId)
        {
            var entity = await GetFunctionAsync(deviceId, functionId);
            if (entity != null)
            {
                var operation = TableOperation.Delete(entity);
                var result = await _scriptTableRef.ExecuteAsync(operation);
            }
        }
    }
}
