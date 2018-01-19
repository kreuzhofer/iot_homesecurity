using System;
using System.Net.Http;
using System.Threading.Tasks;
using IoTHs.Core.Http;
using IoTHs.Devices.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IoTHs.Core.Authentication
{
    public class ApiAuthenticationService : IApiAuthenticationService
    {
        private IAzureIoTHubPlugin _iotHub;
        private ILogger<ApiAuthenticationService> _log;

        public ApiAuthenticationService(IAzureIoTHubPlugin ioTHub, ILoggerFactory loggerFactory)
        {
            _iotHub = ioTHub;
            _log = loggerFactory.CreateLogger<ApiAuthenticationService>();
        }

        public async Task<string> GetTokenAsync()
        {
            // create client token
            var tokenRequestUrl = _iotHub.ServiceBaseUrl + "ApiAuthentication/";
            _log.LogDebug("GetTokenAsync|Get api token");
            var httpClient = new LocalHttpClient();
            httpClient.Client.DefaultRequestHeaders.Add("apikey", _iotHub.ApiKey);
            httpClient.Client.DefaultRequestHeaders.Add("deviceid", _iotHub.DeviceId);
            var tokenResponse = await httpClient.Client.PostAsync(new Uri(tokenRequestUrl), null);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException(tokenResponse.ReasonPhrase);
            }
            // get token from response
            var tokenReponseContent = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenJsonObj = JsonConvert.DeserializeObject(tokenReponseContent);
            string token = tokenJsonObj.token;
            return token;
        }
    }
}