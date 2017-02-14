using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using W10Home.DevicePortal.Security;

namespace W10Home.DevicePortal.Controllers.api
{
	public class DeviceRegistrationController : ApiController
	{
		// POST api/<controller>
		[ApiKeyAuthentication("cd65d126-28c4-49d0-a921-ad688d35f02a", BasicRealm = "key required")]
		public JsonResult<RegistrationRequest> Post([FromBody]string id)
		{
			return Json(RegistrationRequestCache.GetNewPin(id));
		}
	}
}