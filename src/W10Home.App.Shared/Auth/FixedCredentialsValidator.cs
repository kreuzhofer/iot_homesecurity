using Restup.Webserver.Models.Contracts;
using Restup.WebServer.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace W10Home.IoTCoreApp.Auth
{
	internal class FixedCredentialsValidator : ICredentialValidator
	{
		public IAsyncOperation<bool> AuthenticateAsync(string username, string password)
		{
			return Task.FromResult(username == "user" && password == "pass").AsAsyncOperation();
		}
	}
}
