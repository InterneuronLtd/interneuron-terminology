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
﻿using Interneuron.Terminology.Utility.Handlers.AddFDBClassifications;
using Interneuron.Terminology.Utility.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Interneuron.Terminology.Utility
{
    //sample not in use - to be utilized later
    public class Program
    {
        static ServiceProvider ServiceProvider;
        static IConfigurationRoot Configuration;
        static async Task Main(string[] args)
        {
            Setup();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please select the number to execute");
            Console.WriteLine("1. Add FDB Classifications");
            var selectedOption = Console.ReadLine();

            switch (selectedOption)
            {
                case "1":
                case "1.":
                    var addFDBClassficationHandler = ServiceProvider.GetService<AddFDBClassificationHandler>();
                    addFDBClassficationHandler.Configuration = Configuration;
                    addFDBClassficationHandler.ServiceProvider = ServiceProvider;
                    await addFDBClassficationHandler.Handle();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No selection made. Exiting!!");
                    break;
            }

            Console.ReadLine(); 
        }

        private static void Setup()
        {
            //setup configuration
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
            Configuration = builder.Build();
            var conn = Configuration.GetConnectionString("DBConnection");

            //setup our DI
            ServiceProvider = new ServiceCollection()
                .AddLogging(o => o.AddConsole(c => c.LogToStandardErrorThreshold = LogLevel.Debug))
                .AddDbContextFactory<TerminologyDBContext>(o => o.UseNpgsql(conn))
                .AddTransient<AddFDBClassificationHandler>()
                .BuildServiceProvider();


            var logger = ServiceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

            logger.LogDebug("Starting application");
        }
    }
}