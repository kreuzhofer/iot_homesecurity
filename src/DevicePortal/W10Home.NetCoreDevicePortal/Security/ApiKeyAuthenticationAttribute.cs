using System;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace W10Home.NetCoreDevicePortal.Security
{
    public class ApiKeyAuthenticationAttribute : ActionFilterAttribute
    {
        public string BasicRealm { get; set; }
        protected string ApiKey { get; set; }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var configuration = actionContext.HttpContext.RequestServices.GetService<IConfiguration>();
            this.ApiKey = configuration["ApiKey"];

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