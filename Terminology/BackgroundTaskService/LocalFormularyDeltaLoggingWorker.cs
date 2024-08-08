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
﻿using Interneuron.Common.Extensions;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers;

namespace Interneuron.Terminology.BackgroundTaskService
{
    public class LocalFormularyDeltaLoggingWorkerOptions : IWorkerOptions
    {
        public int RepeatIntervalSeconds { get; set; } = 60;
    }

    public class LocalFormularyDeltaLoggingWorker : InterneuronBackgroundTaskWorker
    {
        private readonly ILogger<DMDFormularyToLocalFormularyProcessorWorker> _logger;
        private readonly IConfiguration _configuration;
        private IServiceProvider _serviceProvider;

        // private readonly DeltaIdentificationHandler _deltaIdentificationHandler;
        BackgroundTaskAPIModel? _currentTask = null;

        public LocalFormularyDeltaLoggingWorker(LocalFormularyDeltaLoggingWorkerOptions options,ILogger<DMDFormularyToLocalFormularyProcessorWorker> logger, IConfiguration configuration, TerminologyAPIService terminologyAPIService, IServiceProvider serviceProvider) : base(options, 
            terminologyAPIService)
        {
            _logger = logger;
            _configuration = configuration;
            //_deltaIdentificationHandler = deltaIdentificationHandler;
            _serviceProvider = serviceProvider;

            //OnDoWorkExecutionErrorAsync = () =>
            //{
            //    if (_currentTask == null) return Task.CompletedTask;

            //    return UpdateStatus(3, "processingformularydeltalogfaile", 4, "logformularydelta", "failed delta logs to Local Formulary", _currentTask, string.Empty, string.Empty);
            //};

            OnStopAsync = async () =>
            {
                await RevertToPendingForProcessing("initializeforformularydeltalog", new List<string> { "logformularydelta" });
                _logger.LogInformation("Reverting back the delta log process back to 'initializeforformularydeltalog' status.");
                return;
            };
        }

        public override async Task DoWorkAsync()
        {
            if (string.Compare(_configuration?["TerminologyBackgroundTaskConfig:EnableDMDDeltaLog"], "true", true) != 0) return;

            _currentTask = null;

            try
            {
                var isDeltaLoggingProcessing = await CheckIfImportIsProcessing(new List<string> { "logformularydelta" });

                if (isDeltaLoggingProcessing) return;

                var pendingRecords = await PickIfTasksAvailableForProcessAndMarkStatusAsProcessing(new List<string> { "logformularydelta" }, "loggingdeltatolocalformulary");

                if (!pendingRecords.IsCollectionValid()) return;

                foreach (var rec in pendingRecords)
                {
                    _currentTask = rec;

                    //Identify deltas
                    var scope = _serviceProvider.CreateScope();
                    var svp = scope.ServiceProvider;
                    var deltaIdentificationHandler = svp.GetService<DeltaIdentificationHandler>();
                    //await deltaIdentificationHandler.PersistDeltas(new List<string> { "39561711000001104" });//to test
                    await deltaIdentificationHandler.PersistDeltas();

                    if (scope != null) scope.Dispose();

                    await UpdateStatus(3, "processingformularydeltalogsuccess", 3, "logformularydelta", "Successfully updated delta logs to Local Formulary", rec, string.Empty, string.Empty);
                    _logger.LogInformation("Successfully saved delta for task: " + rec.TaskId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                if (_currentTask != null)
                {
                    try
                    {
                        await UpdateStatus(3, "processingformularydeltalogfailed", 4, "logformularydelta", $"Exception thrown during the execution of the worker {WorkerName}", _currentTask, string.Empty, ex.ToString());
                    }
                    catch { }
                }
            }
        }
    }
}