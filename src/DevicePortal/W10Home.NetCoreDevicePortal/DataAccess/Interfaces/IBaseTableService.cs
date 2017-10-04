using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace W10Home.NetCoreDevicePortal.DataAccess.Services
{
    public interface IBaseTableService<T> where T : TableEntity, new()
    {
        Task<T> InsertOrReplaceAsync(T entity);
        Task<T> GetAsync(string partitionKey, string rowKey);
        Task<List<T>> GetAsync(string partitionKey);
        Task<bool> DeleteAsync(string partitionKey, string rowKey);
    }
}