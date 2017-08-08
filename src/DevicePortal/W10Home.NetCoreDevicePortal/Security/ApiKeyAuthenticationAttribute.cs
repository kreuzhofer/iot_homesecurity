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

            //var req = actionContext.ActionArguments;

            //if (null != req.RequestUri.Query)
            //{
            //    var apikey = HttpUtility.ParseQueryString(req.RequestUri.Query).Get("api_key");
            //    if (!String.IsNullOrEmpty(apikey))
            //    {
            //        if (apikey == ApiKey) return;
            //    }
            //}
            //actionContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
    }
}