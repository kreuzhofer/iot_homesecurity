
//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Web;

namespace W10Home.DevicePortal.Security
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