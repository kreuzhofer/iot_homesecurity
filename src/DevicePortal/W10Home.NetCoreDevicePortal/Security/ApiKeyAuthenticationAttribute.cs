using System;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;

namespace W10Home.NetCoreDevicePortal.Security
{
    public class ApiKeyAuthenticationAttribute : ActionFilterAttribute
    {
        public override async void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (actionContext.HttpContext.Request.Headers.All(h => h.Key.ToLower() != "apikey") ||
                actionContext.HttpContext.Request.Headers.All(h => h.Key.ToLower() != "deviceid"))
            {
                actionContext.Result = new UnauthorizedResult();
                return;
            }
            var apiKey = actionContext.HttpContext.Request.Headers.SingleOrDefault(h => h.Key.ToLower() == "apikey").Value.FirstOrDefault();
            var deviceId = actionContext.HttpContext.Request.Headers.SingleOrDefault(h => h.Key.ToLower() == "deviceid").Value.FirstOrDefault();

            var deviceService = actionContext.HttpContext.RequestServices.GetService<IDeviceService>();
            var device = await deviceService.GetWithApiKeyAsync(deviceId, apiKey);

            base.OnActionExecuting(actionContext);

            if (String.IsNullOrEmpty(apiKey) || apiKey != device.ApiKey || String.IsNullOrEmpty(deviceId) || deviceId != device.RowKey)
            {
                actionContext.Result = new UnauthorizedResult();
            }
        }
    }
}