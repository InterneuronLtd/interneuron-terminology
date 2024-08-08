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
﻿using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.NetCoreAll;
using HealthChecks.UI.Client;
using Interneuron.Infrastructure.Web.Exceptions.Handlers;
using Interneuron.Terminology.BackgroundTask.API.AppCode.Core;
using Interneuron.Terminology.BackgroundTask.API.AppCode.Extensions;
using Interneuron.Terminology.BackgroundTask.API.AppCode.Filters;
using Interneuron.Terminology.BackgroundTask.API.AppCode.Infrastructure;
using Interneuron.Terminology.BackgroundTask.API.Repository;
using Interneuron.Web.Logger;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace Interneuron.Terminology.BackgroundTask.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthenticationToTerminologyBGService(Configuration);

            services.AddTransient<APIRequestContextProvider>();

            services.AddTransient((sp) =>
            {
                APIRequestContext.APIRequestContextProvider = sp.GetRequiredService<APIRequestContextProvider>().CreateAPIContext;
                return APIRequestContext.CurrentContext;
            });

            services.AddTransient<BackgroundTaskRepositoryUtil>();

            services.AddMvc(options =>
            {
                options.Filters.Add(new GlobalFilter());
            });

            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = long.MaxValue;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = long.MaxValue; // if don't set default value is: 30 MB
                options.Limits.MaxConcurrentConnections = null;
                options.Limits.MaxRequestBufferSize= long.MaxValue;
                options.Limits.MaxResponseBufferSize= long.MaxValue;
                options.Limits.KeepAliveTimeout = TimeSpan.MaxValue;
            });

            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
                //config.ApiVersionReader = new HeaderApiVersionReader("api-version");
            });


            services.AddVersionedApiExplorer(
                options =>
                {
                    // note: the specified format code will format the version as "'v'major[.minor]"
                    options.GroupNameFormat = "'v'V";
                    options.SubstituteApiVersionInUrl = true;
                    //options.SubstitutionFormat = "'v'V";
                });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllHeaders",
                      builder =>
                      {
                          builder.AllowAnyOrigin()
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
            });

            services.AddHttpContextAccessor();

            services.AddAutoMapperToTerminologyBGService();

            services.AddCachingToTerminologyBGService(Configuration);

            services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

            services.AddSwaggerToTerminologyBGTaskApp(Configuration);
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            app.UseAllElasticApm(Configuration);

            app.UseSwaggerToTerminologyBGTaskApp(Configuration, provider);

            app.UseInterneuronExceptionHandler(options =>
            {
                options.OnExceptionHandlingComplete = (ex, errorId) =>
                {
                    LogException(ex, errorId);
                };
            });


            app.UseCors("AllowAllHeaders");

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            //required to persit the request body (not retained on exception)
            app.UseMiddleware<InterneuronResetRequestBodyStreamMiddleware>();

            app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = LogHelper.EnrichFromRequest);

            app.UseHealthChecks("/liveness", new HealthCheckOptions
            {
                Predicate = r => r.Name.Contains("self")
            });

            app.UseHealthChecks("/hc", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            //This is required when hosted in IIS as in-proc - requestimeout in web.config does not work
            //Reference: https://www.seeleycoder.com/blog/asp-net-core-request-timeout-iis-in-process-mode
            //app.UseMaximumRequestTimeout();

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/", (httpContext) => { httpContext.Response.Redirect("./swagger/index.html", false); return Task.CompletedTask; });
                endpoints.MapControllers();
            });
        }

        private void LogException(Exception ex, string errorId)
        {
            if (Agent.Tracer != null && Agent.Tracer.CurrentTransaction != null)
            {
                ITransaction transaction = Agent.Tracer.CurrentTransaction;
                transaction.SetLabel("ErrorId", errorId);
            }

            if (ex.Message.StartsWith("cannot open database", StringComparison.InvariantCultureIgnoreCase) || ex.Message.StartsWith("a network", StringComparison.InvariantCultureIgnoreCase))
                Log.Logger.ForContext("ErrorId", errorId).Fatal(ex, ex.Message);
            else
                Log.Logger.ForContext("ErrorId", errorId).Error(ex, ex.Message);
        }

        private void OnShutdown()
        {
            try
            {
                Caching.CacheService.Dispose();
            }
            catch { }
        }
    }
}
