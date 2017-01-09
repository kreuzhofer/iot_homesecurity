using Restup.Webserver.Models.Contracts;
using Restup.WebServer.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W10Home.WebApp.Auth
{
	internal class FixedCredentialsValidator : ICredentialValidator
	{
		public bool Authenticate(string username, string password)
		{
			return username == "user" && password == "pass";
		}
	}
}
