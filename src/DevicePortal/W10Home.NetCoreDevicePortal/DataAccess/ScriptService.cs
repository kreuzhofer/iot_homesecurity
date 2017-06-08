using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace W10Home.NetCoreDevicePortal.DataAccess
{
    public class ScriptService
    {
        private CloudBlobContainer _containerReference;

        public ScriptService(IConfiguration configuration)
        {
            var connection = configuration.GetSection("ConnectionStrings")["DevicePortalStorageAccount"];
            var blobClient = CloudStorageAccount.Parse(connection).CreateCloudBlobClient();
            _containerReference = blobClient.GetContainerReference("scripts");
            _containerReference.CreateIfNotExistsAsync();
        }

        public void SaveScript(string srciptId, string scriptContent)
        {

        }
    }
}
