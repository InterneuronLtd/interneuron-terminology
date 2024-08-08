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
using Interneuron.Terminology.BackgroundTaskService.Repository;
using Newtonsoft.Json.Linq;

namespace Interneuron.Terminology.BackgroundTaskService
{
    public class DMDFileProcessorWorkerOptions : IWorkerOptions
    {
        public int RepeatIntervalSeconds { get; set; } = 60;
    }
    public class DMDFileProcessorWorker : InterneuronBackgroundTaskWorker
    {
        private readonly ILogger<DMDFileProcessorWorker> _logger;
        private readonly TerminologyAPIService _terminologyAPIService;
        //private readonly DMDFileProcessHandler _dmdFileProcessor;
        private readonly IConfiguration _configuration;
        private IServiceProvider _serviceProvider;
        private BackgroundTaskAPIModel? _currentTask;

        private readonly HashSet<string> dmdEntitiesToIgnore = new() { "ATC_LOOKUP", "BNF_LOOKUP", "DMD_LOOKUP_AVAILRESTRICT", "DMD_LOOKUP_BASISOFNAME", "DMD_LOOKUP_BASISOFSTRENGTH", "DMD_LOOKUP_CONTROLDRUGCAT", "DMD_LOOKUP_DRUGFORMIND", "DMD_LOOKUP_FORM", "DMD_LOOKUP_INGREDIENT", "DMD_LOOKUP_LICAUTH", "DMD_LOOKUP_ONTFORMROUTE", "DMD_LOOKUP_PRESCRIBINGSTATUS", "DMD_LOOKUP_ROUTE", "DMD_LOOKUP_SUPPLIER", "DMD_LOOKUP_UOM" };
        public DMDFileProcessorWorker(DMDFileProcessorWorkerOptions options, ILogger<DMDFileProcessorWorker> logger, TerminologyAPIService terminologyAPIService, IConfiguration configuration, IServiceProvider serviceProvider) : base(options, terminologyAPIService)
        {
            _logger = logger;
            _terminologyAPIService = terminologyAPIService;
            //_dmdFileProcessor = dmdFileProcessor;
            _configuration = configuration;
            _serviceProvider = serviceProvider;

            OnStopAsync = async () =>
            {
                await RevertToPendingForProcessing("fileuploaded", new List<string> { "dmdfileupload" });
                _logger.LogInformation("Reverting back the files under process to 'fileuploaded' status");
                return;
            };
        }

        public override async Task DoWorkAsync()
        {
            if (string.Compare(_configuration?["TerminologyBackgroundTaskConfig:EnableDMDFileUploadToDMDFormulary"], "true", true) != 0)
                return;

            _currentTask = null;

            try
            {
                var isImportProcessing = await CheckIfImportIsProcessing(new List<string> { "dmdfileupload" });

                if (isImportProcessing) return;

                var pendingRecords = await PickIfTasksAvailableForProcessAndMarkStatusAsProcessing(new List<string> { "dmdfileupload" }, "processingfile");

                if (!pendingRecords.IsCollectionValid()) return;

                await ProcessPendingRecords(pendingRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                if (_currentTask != null)
                {
                    try
                    {
                        await UpdateStatus(3, "processingfilefailed", 4, "processingfile", $"Exception thrown during the execution of the worker {WorkerName}", _currentTask, string.Empty, ex.ToString());
                    }
                    catch { }
                }
            }
        }

        private async Task ProcessPendingRecords(List<BackgroundTaskAPIModel>? pendingRecords)
        {
            if (!pendingRecords.IsCollectionValid()) return;

            foreach (var rec in pendingRecords)
            {
                if (rec == null || rec.Detail.IsEmpty()) continue;
                
                _currentTask = rec;

                var detail = rec.Detail?.TryParseToJArray()?.FirstOrDefault(det => det != null && det["seq"] != null && det["seq"]?.Value<string>() == "1");

                var filePath = "";
                var fileName = "";
                var bonusFileName = "";
                var bnfFileName = "";
                var bnfLkpfilename = "";

                var fileNameWithPath = "";

                if (detail != null)
                {
                    filePath = detail["filepath"]?.Value<string>();
                    fileName = detail["filename"]?.Value<string>();
                    bonusFileName = detail["bonusfilename"]?.Value<string>();
                    bnfFileName = detail["bnffilename"]?.Value<string>();
                    bnfLkpfilename = detail["bnfLkpfilename"]?.Value<string>();

                    //This is not required
                    fileNameWithPath = detail["filenamewithpath"]?.Value<string>();
                }

                if (!CommonUtil.DoesFileExists(filePath, fileName))
                {
                    await UpdateStatus(3, "processingfilefailed", 4, "processingfile", "Unable to find the fileName or File Path", rec, string.Empty, string.Empty);
                    var failedMsg = "Unable to find the file path or file name for task: " + rec.TaskId;
                    _logger.LogError(failedMsg);
                    _logger.LogInformation(failedMsg);

                    continue;
                }

                var scope = _serviceProvider.CreateScope();
                var svp = scope.ServiceProvider;
                var dmdFileProcessor = svp.GetService<DMDFileProcessHandler>();

                if (string.Compare(_configuration["TerminologyBackgroundTaskConfig:UseAsDMDBrowser"] ?? "false", "true", true) == 0)
                {
                    var unitOfWork = svp.GetService<IUnitOfWork>();
                    await unitOfWork.TerminologyRepository.TruncateTerminologies();
                    await unitOfWork.FormularyHeaderFormularyRepository.TruncateFormularies();
                }

                var (isSuccess, processText, processError) = dmdFileProcessor.ProcessDMDFile(filePath, fileName, bonusFileName, bnfFileName, bnfLkpfilename, null);
                
                if (scope != null) scope.Dispose();

                if (isSuccess)
                {
                    //More time to get sync log data. Moving this line after that
                    //await UpdateStatus(3, "processingfilesuccess", 3, "processingfile", "Successfully processed file", rec, processText, processError);
                    _logger.LogInformation("Successfully processed file for task: " + rec.TaskId);

                    await AddTaskForFomularyImports(rec, fileName, dmdBonusFileName: bonusFileName, bnfFileName, processText, processError);

                    continue;
                }

                await UpdateStatus(3, "processingfilefailed", 4, "processingfile", "Unable to find the fileName or File Path", rec, processText, processError);
                var msg = $"Failed to process file {filePath}-{fileName} for task: {rec.TaskId}, Error: {processError ?? ""}";
                _logger.LogError(msg);
                _logger.LogInformation(msg);
            }
        }

        private async Task AddTaskForFomularyImports(BackgroundTaskAPIModel rec, string fileName = null,string dmdBonusFileName = null, string bnfFileName = null, string? processText = null, string? processError = null)
        {
            //var pendingDMDForFormularyImportResp = await _terminologyAPIService.GetDMDPendingSyncLogs();

            //if (pendingDMDForFormularyImportResp == null || pendingDMDForFormularyImportResp.Data == null || !pendingDMDForFormularyImportResp.Data.IsCollectionValid())
            //{
            //    var msg = $"Could not find any DMD to import after file processing {rec.TaskId}";
            //    _logger.LogError(msg);
            //    _logger.LogInformation(msg);
            //    return;
            //}
            var pendingDMDForFormularyImport = new List<DmdSyncLog>();

            for (int syncLogIndex = 1; syncLogIndex < int.MaxValue; syncLogIndex++)
            {
                var pendingDMDForFormularyImportResp = await _terminologyAPIService.GetDMDPendingSyncLogsByPagination(syncLogIndex, 200);

                if (pendingDMDForFormularyImportResp != null && (pendingDMDForFormularyImportResp.StatusCode == StatusCode.Fail || (pendingDMDForFormularyImportResp.ErrorMessages.IsCollectionValid())))
                {
                    _logger.LogError("Unable to fetch the DMD Sync Log data for TaskId = " + rec.TaskId);
                    await UpdateStatus(3, "processingfilefailed", 4, "processingfile", "Unable to fetch the DMD Sync Log data", rec, processText, processError);
                    return;
                }

                if (pendingDMDForFormularyImportResp == null || pendingDMDForFormularyImportResp.Data == null || !pendingDMDForFormularyImportResp.Data.IsCollectionValid())
                    break;

                pendingDMDForFormularyImport.AddRange(pendingDMDForFormularyImportResp.Data);
            }

            await UpdateStatus(3, "processingfilesuccess", 3, "processingfile", "Successfully processed file", rec, processText, processError);

            if (!pendingDMDForFormularyImport.IsCollectionValid())
            {
                var msg = $"Could not find any DMD to import after file processing {rec.TaskId}";
                _logger.LogError(msg);
                _logger.LogInformation(msg);
                return;
            }

            var dmdsPendingForFormularyImport = new List<DmdSyncLog>();// pendingDMDForFormularyImport;// pendingDMDForFormularyImportResp.Data;

            foreach(var dmdSyncData in pendingDMDForFormularyImport)
            {
                if (dmdSyncData == null || dmdSyncData.DmdEntityName.IsEmpty()) continue;
                var ucDMDEntityName = dmdSyncData.DmdEntityName.ToUpperInvariant();
                if (dmdEntitiesToIgnore.Contains(ucDMDEntityName)) continue;
                dmdsPendingForFormularyImport.Add(dmdSyncData);
            }

            if (!dmdsPendingForFormularyImport.IsCollectionValid())
            {
                var msg = $"Could not find any DMD to import after file processing {rec.TaskId}";
                _logger.LogError(msg);
                _logger.LogInformation(msg);
                return;
            }

            if (string.Compare(_configuration?["TerminologyBackgroundTaskConfig:EnableDMDUpdateToLocalFormulary"], "true", true) != 0)
                return;

            //TODO: After history data available
            //'codes' below shoould be only current and not marked 'hisotrical'
            //Add 'hisorical' codes not added previously to this task as a seperate parameter
            var codes = string.Join("|", dmdsPendingForFormularyImport.Select(rec => rec.DmdId).Distinct());

            await AddTaskForNextStep("importdmdtoformulary", "initializeforimport", new { seq = 1, stepname = "initializeforimport", codes = codes, correlationTaskId = rec.TaskId, fileName = fileName, otherFiles = new string[] { dmdBonusFileName, bnfFileName } }, rec.TaskId);
        }


    }
}