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
using Interneuron.Common.Extensions;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Extensions;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers;
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Repository;
using Newtonsoft.Json.Linq;

namespace Interneuron.Terminology.BackgroundTaskService
{
    public class DMDFormularyToLocalFormularyProcessorWorkerOptions : IWorkerOptions
    {
        public int RepeatIntervalSeconds { get; set; } = 60;//can be from config
    }

    public class DMDFormularyToLocalFormularyProcessorWorker : InterneuronBackgroundTaskWorker
    {
        private readonly ILogger<DMDFormularyToLocalFormularyProcessorWorker> _logger;
        private readonly TerminologyAPIService _terminologyAPIService;
        private readonly IConfiguration _configuration;
        //private readonly FormularyImportHandler _formularyImportHandler;
        //private readonly FormularyPostImportProcessHandler _formularyPostImportProcessHandler;
        private IServiceProvider _serviceProvider;
        private string? _fileName;
        //private string? _correlationTaskId;
        private BackgroundTaskAPIModel? _currentTask;
        private DMDLookupProvider _dmpLookupProvider;

        public DMDFormularyToLocalFormularyProcessorWorker(DMDFormularyToLocalFormularyProcessorWorkerOptions options, ILogger<DMDFormularyToLocalFormularyProcessorWorker> logger, IConfiguration configuration, TerminologyAPIService terminologyAPIService,   IServiceProvider serviceProvider) : base(options, terminologyAPIService)
        {
            _logger = logger;
            _terminologyAPIService = terminologyAPIService;
            _configuration = configuration;
            //_formularyImportHandler = formularyImportHandler;
            //_formularyPostImportProcessHandler = formularyPostImportProcessHandler;
            _serviceProvider = serviceProvider;

            OnStopAsync = async () =>
            {
                await RevertToPendingForProcessing("initializeforimport", new List<string> { "importdmdtoformulary" });
                _logger.LogInformation("Reverting back the local formulary import process back to 'initializeforimport' status.");
                return;
            };
        }

        public override async Task DoWorkAsync()
        {
            if (string.Compare(_configuration?["TerminologyBackgroundTaskConfig:EnableDMDUpdateToLocalFormulary"], "true", true) != 0)
                return;
            _currentTask = null;

            try
            {
                var isImportProcessing = await CheckIfImportIsProcessing(new List<string> { "importdmdtoformulary" });

                if (isImportProcessing) return;

                var pendingRecords = await PickIfTasksAvailableForProcessAndMarkStatusAsProcessing(new List<string> { "importdmdtoformulary" }, "processingtolocalformulary");

                if (!pendingRecords.IsCollectionValid()) return;

                await ProcessPendingRecords(pendingRecords);

                _dmpLookupProvider = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                if (_currentTask != null)
                {
                    try
                    {
                        await UpdateStatus(3, "processingdmdcodesfailed", 4, "processingdmdcodes", $"Exception thrown during the execution of the worker {WorkerName}", _currentTask, string.Empty, ex.ToString());
                    }
                    catch { }
                }
            }
        }
        
        private async Task ProcessPendingRecords(List<BackgroundTaskAPIModel>? pendingRecords)
        {
            if (!pendingRecords.IsCollectionValid()) return;

            _dmpLookupProvider = await DMDLookupProvider.CreateAsync(_terminologyAPIService);
            //_formularyImportHandler.getDMDLookupProvider = () => dmpLookupProvider;
            //_formularyPostImportProcessHandler.getDMDLookupProvider = () => _dmpLookupProvider;

            foreach (var rec in pendingRecords)
            {
                if (rec == null || rec.Detail.IsEmpty()) continue;

                _currentTask = rec;

                var detail = rec.Detail?.TryParseToJArray()?.FirstOrDefault(det => det != null && det["seq"] != null && det["seq"]?.Value<string>() == "1");

                var dmdCodesAsStr = "";
                _fileName = "";
                //_correlationTaskId = "";

                if (detail != null)
                {
                    //get historic codes also here
                    dmdCodesAsStr = detail["codes"]?.Value<string>();
                    _fileName = detail["filename"]?.Value<string>();
                    //_correlationTaskId = detail["correlationTaskId"]?.Value<string>();
                }

                var dmdCodes = dmdCodesAsStr?.Split("|")?.ToList();

                dmdCodes = dmdCodes?.Where(rec => rec.IsNotEmpty())?.ToList();

                if (dmdCodesAsStr.IsEmpty() || !dmdCodes.IsCollectionValid())
                {
                    var failedMsg = "Unable to find the dmd codes for task: " + rec.TaskId;
                    await UpdateStatus(3, "processingdmdcodesfailed", 4, "processingdmdcodes", "Unable to find the dmd codes for task: " + rec.TaskId, rec, string.Empty, string.Empty);

                    _logger.LogError(failedMsg);
                    _logger.LogInformation(failedMsg);
                    continue;
                }

                dmdCodes = dmdCodes.Distinct().ToList();

                //Execute in batches
                await InvokeImportByCodes(dmdCodes, rec);
            }
        }

        private async Task InvokeImportByCodes(List<string> dmdCodes, BackgroundTaskAPIModel rec, List<string> historicCodes = null)
        {
            await ExecuteImportByCodes(dmdCodes, rec, historicCodes);

            //execute for all codes
            var scope = _serviceProvider.CreateScope();
            var svp = scope.ServiceProvider;

            var formularyPostImportProcessHandler = svp.GetService<FormularyPostImportProcessHandler>();
            formularyPostImportProcessHandler.getDMDLookupProvider = () => _dmpLookupProvider;

            await formularyPostImportProcessHandler.InvokePostImportProcessForCodes(dmdCodes);
            //await _formularyPostImportProcessHandler.InvokePostImportProcessForCodes(dmdCodes);
            if (scope != null) scope.Dispose();

            var batchsizeForStatusUpdate = 500;

            var batchedRequestsForStatusUpdate = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < dmdCodes.Count; reqIndex += batchsizeForStatusUpdate)
            {
                var batches = dmdCodes.Skip(reqIndex).Take(batchsizeForStatusUpdate);
                batchedRequestsForStatusUpdate.Add(batches.ToList());
            }

            foreach (var batchedRequest in batchedRequestsForStatusUpdate)
            {
                await _terminologyAPIService.UpdateFormularySyncStatus(batchedRequest);
            }

            var scopeA = _serviceProvider.CreateScope();
            var svpA = scopeA.ServiceProvider;

            if (string.Compare(_configuration["TerminologyBackgroundTaskConfig:UseAsDMDBrowser"] ?? "false", "true", true) == 0)
            {
                var unitOfWorkA = svpA.GetService<IUnitOfWork>();
                await unitOfWorkA.FormularyHeaderFormularyRepository.UpdateAllStatusAsActive();
            }
            if (scopeA != null) scopeA.Dispose();

            await AddTaskForNextStep("logformularydelta", "initializeforformularydeltalog", new { seq = 1, stepname = "initializeforformularydeltalog", correlationTaskId = rec.TaskId,//_correlationTaskId, 
                fileName = _fileName }, rec.TaskId);

            await UpdateStatus(3, "processingdmdcodessuccess", 3, "processingdmdcodes", "Successfully saved dmd details from DM+D formulary to Local Formulary", rec, string.Empty, string.Empty);

            _logger.LogInformation("Successfully saved dmd details from DM+D formulary to Local Formulary for task: " + rec.TaskId);

        }

        private async Task ExecuteImportByCodes(List<string> dmdCodes, BackgroundTaskAPIModel rec, List<string> historicCodes)
        {
            var configuredBatchSize = _configuration.GetSection("TerminologyBackgroundTaskConfig").GetValue<int>("BulkImportBatchSize");

            var batchsize = configuredBatchSize;

            var batchedRequests = new List<List<string>>();
            for (var reqIndex = 0; reqIndex < dmdCodes.Count; reqIndex += batchsize)
            {
                var batches = dmdCodes.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToList());
            }
            var consolidatedErrors = new List<List<string>>();

            var failedCnt = 0;
            var failedErrors = new List<string>();
            //can be parallelized later -- depends on uow invocation
            foreach (var batchedReq in batchedRequests)
            {
                var scope = _serviceProvider.CreateScope();
                var svp = scope.ServiceProvider;

                var formularyImportHandler = svp.GetService<FormularyImportHandler>();

                formularyImportHandler.getDMDLookupProvider = () => _dmpLookupProvider;

                var response = await formularyImportHandler.ImportByCodes(batchedReq, defaultFormularyStatusCode: TerminologyConstants.FORMULARYSTATUS_NONFORMULARY, defaultRecordStatusCode: TerminologyConstants.RECORDSTATUS_DRAFT);

                if (scope != null) scope.Dispose();

                if (response.Errors.IsCollectionValid())
                {
                    failedCnt++;
                    failedErrors.AddRange(response.Errors);
                    var errorMsg = "Error importing the DMD details to Local formulary for task: " + rec.TaskId;

                    //await UpdateStatus(3, "processingdmdcodesfailed", 4, "processingdmdcodes", errorMsg, rec, string.Empty, string.Join("| ", response.Errors));

                    _logger.LogError(errorMsg);
                    _logger.LogInformation(errorMsg);
                    //return;
                }
            }

            if(failedErrors.IsCollectionValid() && batchedRequests.Count == failedCnt)
            {
                var errorMsg = "Error importing the DMD details to Local formulary for task: " + rec.TaskId;

                await UpdateStatus(3, "processingdmdcodesfailed", 4, "processingdmdcodes", errorMsg, rec, string.Empty, string.Join("| ", failedErrors));

                _logger.LogError(errorMsg);
                _logger.LogInformation(errorMsg);
                return;
            }

            //Historic codes handling here
            //var response = await _formularyImportHandler.ImportByHistoricCodes(historicCodes, defaultFormularyStatusCode: TerminologyConstants.FORMULARYSTATUS_NONFORMULARY, defaultRecordStatusCode: TerminologyConstants.RECORDSTATUS_DRAFT);
        }
    }
}