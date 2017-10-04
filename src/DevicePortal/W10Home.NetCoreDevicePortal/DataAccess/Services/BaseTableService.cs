using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using W10Home.NetCoreDevicePortal.DataAccess.Entities;

namespace W10Home.NetCoreDevicePortal.DataAccess.Services
{
    public class BaseTableService<T> : IBaseTableService<T> where T : TableEntity, new()
    {
        protected readonly CloudTable TableRef;

        public BaseTableService(IConfiguration configuration)
        {
            var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
            var tableClient = CloudStorageAccount.Parse(connection).CreateCloudTableClient();
            var typeName = typeof(T).Name;
            var tableName = typeName.Substring(0, typeName.IndexOf("Entity"));
            TableRef = tableClient.GetTableReference(tableName);
            TableRef.CreateIfNotExistsAsync();
        }

        public virtual async Task<T> InsertOrReplaceAsync(T entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);
            var result = await TableRef.ExecuteAsync(operation);
            return result.Result as T;
        }

        public virtual async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var result = await TableRef.ExecuteAsync(operation);
            return result.Result as T;
        }

        public virtual async Task<List<T>> GetAsync(string partitionKey)
        {
            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            var result = await TableRef.ExecuteQuerySegmentedAsync<T>(query, null);
            return result.Results;
        }

        public virtual async Task<bool> DeleteAsync(string partitionKey, string rowKey)
        {
            var entity = await GetAsync(partitionKey, rowKey);
            if (entity != null)
            {
                var operation = TableOperation.Delete(entity);
                var result = await TableRef.ExecuteAsync(operation);
                return result.HttpStatusCode == (int)HttpStatusCode.OK;
            }
            return false;
        }
    }
}