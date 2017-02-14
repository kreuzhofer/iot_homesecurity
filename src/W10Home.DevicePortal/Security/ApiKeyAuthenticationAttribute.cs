
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

using System;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

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

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            var req = actionContext.Request;

            if (null != req.RequestUri.Query)
            {
                var apikey = HttpUtility.ParseQueryString(req.RequestUri.Query).Get("api_key");
                if (!String.IsNullOrEmpty(apikey))
                {
                    if (apikey == ApiKey) return;
                }
            }
            actionContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
    }
}