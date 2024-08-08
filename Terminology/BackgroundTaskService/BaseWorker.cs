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
﻿using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace Interneuron.Terminology.BackgroundTaskService
{
    public abstract class BaseWorker : BackgroundService
    {
        private readonly IWorkerOptions _workerOptions;

        public string WorkerName { get; }
        public Serilog.ILogger Logger { get; }
        public abstract Task DoWorkAsync();
        public Func<Task> OnStopAsync { get; set; }
        public Func<Task> OnExecutionErrorAsync { get; set; }
        //public Func<Task> OnDoWorkExecutionErrorAsync { get; set; }

        public BaseWorker(IWorkerOptions workerOptions)
        {
            _workerOptions = workerOptions;
            WorkerName = GetType().Name;
            Logger = Log.ForContext("Type", WorkerName);
            Logger.Information("Starting {worker}. Runs every {minutes} minutes. All options {@options}", WorkerName, _workerOptions.RepeatIntervalSeconds, _workerOptions);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We catch anything and alert instead of rethrowing")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        Logger.Information("Calling DoWorkAsync");
                        await DoWorkAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        //try
                        //{
                        //    if (OnDoWorkExecutionErrorAsync != null) await OnDoWorkExecutionErrorAsync();
                        //}
                        //catch { }
                        Logger.Error(ex, $"Unhandled exception occurred in the {WorkerName}. Worker will retry after the normal interveral.");
                    }

                    await Task.Delay(_workerOptions.RepeatIntervalSeconds * 1000, stoppingToken).ConfigureAwait(false);
                }
                Logger.Information("Execution ended. Cancelation token cancelled = {IsCancellationRequested}",stoppingToken.IsCancellationRequested);
            }
            catch (Exception ex) when (stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (OnExecutionErrorAsync != null) await OnExecutionErrorAsync();
                }
                catch { }
                Logger.Warning(ex, "Execution Cancelled");
                Logger.Error(ex, "Execution Cancelled");
            }
            catch (Exception ex)
            {
                try
                {
                    if (OnExecutionErrorAsync != null) await OnExecutionErrorAsync();
                }
                catch { }
                Logger.Error(ex, "Unhandeled exception. Execution Stopping");
                Environment.Exit(1);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.Information("Calling WorkOnStopAsync");

                try
                {
                    if (OnStopAsync != null)
                        await OnStopAsync().ConfigureAwait(false);
                }
                catch { }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unhandeled exception while stopping. Execution Stopping");

            }
            await base.StopAsync(cancellationToken);
        }
    }

    public interface IWorkerOptions
    {
        int RepeatIntervalSeconds { get; set; }
    }

    //public class Test : BaseWorker
    //{
    //    public Test(DMDFileProcessorWorkerOptions options): base(options)
    //    {

    //    }
    //    public override Task DoWorkAsync()
    //    {
    //        try
    //        {
    //            throw new Exception("ooooo");
    //        }
    //        catch (Exception ex)
    //        {

    //            throw;
    //        }
    //    }
    //}
}