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
using Interneuron.Terminology.CacheService.Extensions;
using Interneuron.Terminology.CacheService.Infrastructure;
using Serilog;

//var configuration = GetConfiguration();

//Log.Logger = new InterneuronSerilogLoggerService().CreateSerilogLogger(configuration, AppName);

////CreateHostBuilder(args).Build().Run();
//try
//{
//    Log.Information(ProgramInitMsg, AppName);
//    var host = BuildWebHost(configuration, args);

//    Log.Information(ProgramStartMsg, AppName);
//    host.Run();

//    return 0;
//}
//catch (System.Exception ex)
//{
//    Log.Fatal(ex, ProgramExceptionMsg, AppName);
//    return 1;
//}
//finally
//{
//    Log.CloseAndFlush();
//}

string Namespace = typeof(Program).Namespace;
string AppName = Namespace;
//const string ProgramExceptionMsg = "Program terminated unexpectedly ({ApplicationContext})!";
//const string ProgramInitMsg = "Configuring web host ({ApplicationContext})...";
//const string ProgramStartMsg = "Starting web host ({ApplicationContext})...";

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new InterneuronSerilogLoggerService().CreateSerilogLogger(builder.Configuration, AppName);
// Add services to the container.

builder.Services.AddCachingToTerminology(builder.Configuration);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


