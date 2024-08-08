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
﻿using Interneuron.Caching;
using System.Reflection;

namespace Interneuron.Terminology.BackgroundTask.API.AppCode.Extensions
{
    public static class ServicesExtension
    {
        public static void AddAuthenticationToTerminologyBGService(this IServiceCollection services, IConfiguration configuration)
        {
            var TerminologyConfigSection = configuration.GetSection("TerminologyBackgroundTaskConfig");

            services.AddAuthentication("Bearer")
              .AddIdentityServerAuthentication(options =>
              {
                  options.Authority = TerminologyConfigSection["AuthorizationAuthority"];

                  options.RequireHttpsMetadata = false;

                  options.ApiName = TerminologyConfigSection["AuthorizationAudience"];

                  options.EnableCaching = false;

                  string byPassSSLValidation = TerminologyConfigSection["IgnoreIdentitySeverSSLErrors"];

                  if (!string.IsNullOrWhiteSpace(byPassSSLValidation) && byPassSSLValidation.ToLower() == "true")
                      options.JwtBackChannelHandler = GetHandler();
              });

            //To be uncommented for CBAC - Refer SynapseDynamicAPI
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("CareRecordAPIWriters", builder =>
            //    {
            //        builder.RequireClaim(configuration["SynapseCore:Settings:SynapseRolesClaimType"], configuration["SynapseCore:Settings:DynamicAPIWriteAccessRole"]);
            //        builder.RequireScope(configuration["SynapseCore:Settings:WriteAccessAPIScope"]);
            //    });
            //});
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("CareRecordAPIReaders", builder =>
            //    {
            //        builder.RequireClaim(configuration["SynapseCore:Settings:SynapseRolesClaimType"], configuration["SynapseCore:Settings:DynamicAPIReadAccessRole"]);
            //        builder.RequireScope(configuration["SynapseCore:Settings:ReadAccessAPIScope"]);
            //    });
            //});

        }

        public static void AddCachingToTerminologyBGService(this IServiceCollection services, IConfiguration configuration)
        {
            CacheService.CacheSettings = new CacheSettings(configuration);
        }

        private static HttpClientHandler GetHandler()
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            return handler;
        }

        public static void AddAutoMapperToTerminologyBGService(this IServiceCollection services)
        {
            var assemblies = GetAssembliesFromBaseDirectory();

            services.AddAutoMapper(assemblies);
        }


        //public static void AddCustomFormatters(this IServiceCollection services, Action<MvcOptions> setupAction = null)
        //{
        //    services.AddMvc(options =>
        //    {
        //        options.OutputFormatters.Insert(0, new TerminologyResourceJsonOutputFormatter());

        //        options.RespectBrowserAcceptHeader = true;

        //        setupAction?.Invoke(options);
        //    })
        //    .SetCompatibilityVersion(CompatibilityVersion.Latest);
        //}


        private static Assembly[] GetAssembliesFromBaseDirectory()
        {
            //Load Assemblies
            //Get All assemblies.
            var refAssembyNames = Assembly.GetExecutingAssembly()
                .GetReferencedAssemblies();

            if (refAssembyNames != null)
            {
                var refFilteredAssembyNames = refAssembyNames.Where(refAsm => refAsm.FullName.StartsWith("Interneuron.Terminology.BackgroundTask"));

                //Load referenced assemblies
                foreach (var assemblyName in refFilteredAssembyNames)
                {
                    Assembly.Load(assemblyName);
                }
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            return assemblies != null ? assemblies
                .Where(refAsm => refAsm.FullName.StartsWith("Interneuron.Terminology.BackgroundTask")).ToArray() : null;

        }
    }
}
