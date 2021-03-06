﻿using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace IoTHs.Core.Http
{
    public class LocalHttpClient
    {
        public LocalHttpClient()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback;
            Client = new HttpClient(handler);
        }

        public LocalHttpClient(string bearerToken) : this()
        {
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + bearerToken);
        }

        public HttpClient Client { get; }

        private bool ServerCertificateCustomValidationCallback(HttpRequestMessage httpRequestMessage, X509Certificate2 x509Certificate2, X509Chain arg3, SslPolicyErrors arg4)
        {
            return true;
        }
    }
}