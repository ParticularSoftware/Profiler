﻿using System.Web.Http.Filters;
using NServiceBus.Profiler.Desktop.Management;

namespace NServiceBus.Profiler.FunctionalTests.ServiceControlStub
{
    public class VersionInformationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);
            actionExecutedContext.Response.Headers.Add(DefaultManagementService.ServiceControlHeaders.ParticularVersion, "1.0.0-stub");
        }
    }
}