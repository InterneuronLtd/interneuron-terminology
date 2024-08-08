 //Interneuron synapse

//Copyright(C) 2024 Interneuron Limited

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

//See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interneuron.Web.Logging
{
    //Not working
    //public class LogEnrichmentFilter : IActionFilter
    //{
    //    private readonly IHttpContextAccessor _httpContextAccessor;
    //    private readonly APIRequestContext _requestContext;

    //    public LogEnrichmentFilter(IHttpContextAccessor httpContextAccessor, APIRequestContext requestContext)
    //    {
    //        _httpContextAccessor = httpContextAccessor;
    //        _requestContext = requestContext;
    //    }

    //    public void OnActionExecuting(ActionExecutingContext context)
    //    {
    //        var httpUser = this._httpContextAccessor.HttpContext.User;

    //        if (httpUser.Identity.IsAuthenticated)
    //        {
    //            //var appUser = new AppIdentity(httpUser);
    //            LogContext.PushProperty("UserName1", "appUser.Username");
    //        }
    //        else
    //        {
    //            LogContext.PushProperty("UserName1", "UNKNOWN");
    //        }
    //    }

    //    public void OnActionExecuted(ActionExecutedContext context)
    //    {
    //        // Do nothing
    //    }
    //}

    //public class InterneuronSerilogLoggingMiddleware
    //{
    //    private readonly RequestDelegate next;

    //    public InterneuronSerilogLoggingMiddleware(RequestDelegate next)
    //    {
    //        this.next = next;
    //    }

    //    public async Task Invoke(HttpContext context)
    //    {
    //        var userName = (context != null && context.User != null && context.User.Identity != null && context.User.Identity.Name != null) ? context.User.Identity.Name : "API Client";

    //        //var userName = _requestContext?.TerminologyAPIUser?.UserId;

    //        LogContext.PushProperty("UserName", userName);

    //        //var userClaimDetails = _requestContext?.TerminologyAPIUser?.UserScopes;
    //        //LogContext.PushProperty("ClientDetails", userClaimDetails);

    //        string userClaimDetails = "";

    //        if (context != null && context.User != null && context.User.Claims != null)
    //        {

    //            foreach (var claim in context.User.Claims)
    //            {
    //                var userDetails = userClaimDetails.IsEmpty() ? "" : $"{userClaimDetails};";
    //                userClaimDetails = $"{userDetails} {claim.Type}: {claim.Value}";
    //            }
    //            //LogContext.PushProperty("ClientDetails", userClaimDetails);
    //        }

    //        using (LogContext.PushProperty("ClientDetails", 1234, true))
    //        {
    //            await next.Invoke(context);
    //        }
    //        //await next.Invoke(context);
    //    }
    //}

    // Option 2:
    // Use this to get the minimal log information
    // Change the configuration to 
    // .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    // Reference:
    // https://www.carlrippon.com/adding-useful-information-to-asp-net-core-web-api-serilog-logs/
    // https://blog.datalust.co/smart-logging-middleware-for-asp-net-core/
    //public class SynapseSerilogLoggingMiddleware
    //{
    //    const string MessageTemplate =
    //        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    //    static readonly ILogger Log = Serilog.Log.ForContext<SynapseSerilogLoggingMiddleware>();

    //    readonly RequestDelegate _next;

    //    public SynapseSerilogLoggingMiddleware(RequestDelegate next)
    //    {
    //        if (next == null) throw new ArgumentNullException(nameof(next));
    //        _next = next;
    //    }

    //    public async Task Invoke(HttpContext httpContext)
    //    {
    //        if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

    //        var sw = Stopwatch.StartNew();
    //        try
    //        {
    //            await _next(httpContext);
    //            sw.Stop();

    //            var statusCode = httpContext.Response?.StatusCode;
    //            var level = statusCode > 499 ? LogEventLevel.Error : LogEventLevel.Information;

    //            var log = level == LogEventLevel.Error ? LogForErrorContext(httpContext) : Log;

    //            //Added locally
    //            AddUserDetails(log, httpContext);

    //            log.Write(level, MessageTemplate, httpContext.Request.Method, httpContext.Request.Path, statusCode, sw.Elapsed.TotalMilliseconds);
    //        }
    //        // Never caught, because `LogException()` returns false.
    //        catch (Exception ex) when (LogException(httpContext, sw, ex)) { }
    //    }

    //    static bool LogException(HttpContext httpContext, Stopwatch sw, Exception ex)
    //    {
    //        sw.Stop();

    //        LogForErrorContext(httpContext)
    //            .Error(ex, MessageTemplate, httpContext.Request.Method, httpContext.Request.Path, 500, sw.Elapsed.TotalMilliseconds);

    //        return false;
    //    }

    //    static ILogger LogForErrorContext(HttpContext httpContext)
    //    {
    //        var request = httpContext.Request;

    //        var result = Log
    //            .ForContext("RequestHeaders", request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), destructureObjects: true)
    //            .ForContext("RequestHost", request.Host)
    //            .ForContext("RequestProtocol", request.Protocol);

    //        if (request.HasFormContentType)
    //            result = result.ForContext("RequestForm", request.Form.ToDictionary(v => v.Key, v => v.Value.ToString()));

    //        //Added locally
    //        result = AddUserDetails(result, httpContext);

    //        return result;
    //    }

    //    static ILogger AddUserDetails(ILogger log, HttpContext context)
    //    {
    //        var userName = (context != null && context.User != null && context.User.Identity != null && context.User.Identity.Name != null) ? context.User.Identity.Name : "API Client";

    //        log.ForContext("UserName", userName);

    //        string userClaimDetails = "";

    //        if (context != null && context.User != null && context.User.Claims != null)
    //        {
    //            foreach (var claim in context.User.Claims)
    //            {
    //                userClaimDetails = $"{userClaimDetails}; {claim.Type}: {claim.Value}";
    //            }
    //            log.ForContext("ClientDetails", userClaimDetails);
    //        }

    //        return log;
    //    }
    //}

    public class InterneuronSerilogHttpRequestLogger
    {
        readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public InterneuronSerilogHttpRequestLogger(RequestDelegate next, IConfiguration configuration)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            _next = next;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            if(_configuration == null)
            {
                await _next(httpContext);
                return;
            }
            var isEnabledHttpLogging = _configuration["Logs:EnableHttpLogging"]?.ToLower() == "true" ? true : false;

            if (!isEnabledHttpLogging)
            {
                await _next(httpContext);
                return;
            }

            // Push the user name into the log context so that it is included in all log entries
            LogContext.PushProperty("UserName", httpContext.User.Identity.Name);

            // Getting the request body is a little tricky because it's a stream
            // So, we need to read the stream and then rewind it back to the beginning
            string requestBody = "";
            //httpContext.Request.EnableRewind();
            httpContext.Request.EnableBuffering();
            Stream body = httpContext.Request.Body;
            byte[] buffer = new byte[Convert.ToInt32(httpContext.Request.ContentLength)];
            await httpContext.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            requestBody = Encoding.UTF8.GetString(buffer);
            body.Seek(0, SeekOrigin.Begin);
            httpContext.Request.Body = body;

            Log.ForContext("RequestHeaders", httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), destructureObjects: true)
               .ForContext("RequestBody", requestBody)
               .Debug("Request information {RequestMethod} {RequestPath} information", httpContext.Request.Method, httpContext.Request.Path);


            // The reponse body is also a stream so we need to:
            // - hold a reference to the original response body stream
            // - re-point the response body to a new memory stream
            // - read the response body after the request is handled into our memory stream
            // - copy the response in the memory stream out to the original response stream
            using (var responseBodyMemoryStream = new MemoryStream())
            {
                var originalResponseBodyReference = httpContext.Response.Body;
                httpContext.Response.Body = responseBodyMemoryStream;

                try
                {
                    await _next(httpContext);
                }
                catch (Exception exception)
                {
                    Guid errorId = Guid.NewGuid();
                    Log.ForContext("Type", "Error")
                        .ForContext("Exception", exception, destructureObjects: true)
                        .Error(exception, exception.Message + ". {@errorId}", errorId);

                    var result = JsonConvert.SerializeObject(new { error = "Sorry, an unexpected error has occurred", errorId = errorId });
                    httpContext.Response.ContentType = "application/json";
                    httpContext.Response.StatusCode = 500;
                    await httpContext.Response.WriteAsync(result);
                    return;
                }

                httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
                httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

                Log.ForContext("RequestBody", requestBody)
                   .ForContext("ResponseBody", responseBody)
                   .Debug("Response information {RequestMethod} {RequestPath} {statusCode}", httpContext.Request.Method, httpContext.Request.Path, httpContext.Response.StatusCode);

                await responseBodyMemoryStream.CopyToAsync(originalResponseBodyReference);
            }
        }
    }
}

