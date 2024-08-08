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
using Elastic.Apm.NetCoreAll;
using Interneuron.Infrastructure.Web.Logging;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers.Util;
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Repository;
using Interneuron.Terminology.BackgroundTaskService.Repository.DBModelsContext;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;
using System.Diagnostics;
using System.Reflection;

namespace Interneuron.Terminology.BackgroundTaskService
{
    public class Program
    {
        //public static void Main(string[] args)
        //{
        //    IHost host = Host.CreateDefaultBuilder(args)
        //        .UseWindowsService(options =>
        //        {
        //            options.ServiceName = "Interneuron.Terminology.BackgroundTaskService";
        //        })
        //        .ConfigureLogging((context, logging) =>
        //        {
        //            // See: https://github.com/dotnet/runtime/issues/47303
        //            logging.AddConfiguration(context.Configuration.GetSection("Logging"));
        //        })
        //        .ConfigureServices(services =>
        //        {
        //            services.AddHostedService<Worker>();
        //        })
        //        .Build();

        //    host.Run();
        //}

        static readonly string Namespace = typeof(Program).Namespace;
        static readonly string AppName = Namespace;
        const string ProgramExceptionMsg = "Program terminated unexpectedly ({ApplicationContext})!";
        const string ProgramInitMsg = "Configuring web host ({ApplicationContext})...";
        const string ProgramStartMsg = "Starting web host ({ApplicationContext})...";

        public static int Main(string[] args)
        {
            var configuration = GetConfiguration();

            Log.Logger = new InterneuronSerilogLoggerService().CreateSerilogLogger(configuration, AppName);
            //Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));

            //CreateHostBuilder(args).Build().Run();
            try
            {
                Log.Information(ProgramInitMsg, AppName);
                var host = BuildHost(configuration, args);
                
                Log.Information(ProgramStartMsg, AppName);
                host.Run();

                return 0;
            }
            catch (System.Exception ex)
            {
                Log.Fatal(ex, ProgramExceptionMsg, AppName);
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }


        public static IHost BuildHost(IConfiguration configuration, string[] args) =>

            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "Interneuron.Terminology.BackgroundTaskService";
                })
                //.UseAllElasticApm()
                .UseSerilog()
                .ConfigureLogging((context, logging) =>
                {
                    // See: https://github.com/dotnet/runtime/issues/47303
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    //logging.AddConfiguration(configuration);
                })
                .ConfigureServices(services =>
                {
                    LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);

                    services.Configure<HostOptions>(hostOptions =>
                    {
                        hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddTransient<LocalFormularyDeltaLoggingWorkerOptions>();
                    services.AddTransient<DMDFileProcessorWorkerOptions>();
                    services.AddTransient<DMDFormularyToLocalFormularyProcessorWorkerOptions>();

                    //services.AddTransient<FormularyImportInvoker>();
                    services.AddTransient<FormularyUtil>();
                    services.AddTransient<DeltaIdentificationHandler>();
                    services.AddTransient<IEntityEventHandler, AuditableEntityHandler>();
                    services.AddTransient<TerminologyDBContext>();
                    //services.AddDbContext<TerminologyDBContext>();
                    services.AddTransient<IUnitOfWork, UnitOfWork>();
                    //services.AddScoped<IUnitOfWork, UnitOfWork>();
                    services.AddTransient(typeof(IFormularyRepository<>), typeof(FormularyRepository<>));
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
                    services.AddTransient<FormularyPostImportProcessHandler>();
                    services.AddTransient<FormularyImportHandler>();
                    services.AddTransient<DMDFileProcessHandler>();
                    services.AddTransient<TerminologyAPIService>();
                    services.AddHostedService<LocalFormularyDeltaLoggingWorker>();
                    services.AddHostedService<DMDFormularyToLocalFormularyProcessorWorker>();
                    services.AddHostedService<DMDFileProcessorWorker>();
                    //services.AddHostedService<Test>();
                    services.AddAutoMapper(GetAssembliesFromBaseDirectory());
                }).Build();



        private static IConfiguration GetConfiguration()
        {
            var environmentName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var contentPath = System.Environment.GetEnvironmentVariable("contentRoot") ?? GetBasePath() ?? Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(contentPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        private static string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }

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
                .Where(refAsm => refAsm.FullName.StartsWith("Interneuron.Terminology.BackgroundTaskService")).ToArray() : null;

        }
    }
}