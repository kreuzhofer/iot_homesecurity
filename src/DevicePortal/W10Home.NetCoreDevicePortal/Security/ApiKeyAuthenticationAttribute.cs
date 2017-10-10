using System;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace W10Home.NetCoreDevicePortal.Security
{
    public class ApiKeyAuthenticationAttribute : ActionFilterAttribute
    {
        public string BasicRealm { get; set; }
        protected string ApiKey { get; set; }

        public ApiKeyAuthenticationAttribute(string apikey)
        {
            this.ApiKey = apikey;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            if (actionContext.HttpContext.Request.Headers.All(h => h.Key.ToLower() != "apikey"))
            {
                actionContext.Result = new UnauthorizedResult();
                return;
            }
            var apiKey = actionContext.HttpContext.Request.Headers.SingleOrDefault(h=>h.Key.ToLower() == "apikey").Value.FirstOrDefault();
            if (String.IsNullOrEmpty(apiKey) || apiKey != this.ApiKey)
            {
                actionContext.Result = new UnauthorizedResult();
            }
        }
    }
}