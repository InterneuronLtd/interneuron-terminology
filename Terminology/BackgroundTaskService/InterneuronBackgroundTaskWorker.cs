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
﻿using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Interneuron.Terminology.BackgroundTaskService
{
    public abstract class InterneuronBackgroundTaskWorker: BaseWorker 
    {
        private readonly TerminologyAPIService _terminologyAPIService;

        public InterneuronBackgroundTaskWorker(IWorkerOptions workerOptions, TerminologyAPIService terminologyAPIService): base(workerOptions)
        {
            _terminologyAPIService = terminologyAPIService;
        }
        protected async Task AddTaskForNextStep(string taskName, string taskStatusName, object taskStepDetail, string correlationTaskId = null)
        {
            var jObj = JObject.FromObject(taskStepDetail);
            JArray arr = new() { jObj };

            var newImportDMDToFormularyTask = new BackgroundTaskAPIModel()
            {
                Name = taskName,
                Status = taskStatusName,
                StatusCd = 1,
                Detail = arr.ToString(),
                CorrelationTaskId = correlationTaskId
            };

            await _terminologyAPIService.CreateTerminologyBGTask(newImportDMDToFormularyTask);
        }

        protected async Task UpdateStatus(int seq, string status, short statusCd, string stepName, string reasonFor, BackgroundTaskAPIModel rec, string processText, string processError)
        {
            var request = JsonConvert.DeserializeObject<BackgroundTaskAPIModel>(JsonConvert.SerializeObject(rec));
            request.StatusCd = statusCd;
            request.Status = status;
            
            if (processText != null)
                processText = processText.Length <= 200 ? processText : processText.Substring(processText.Length - 200);//get only the last 200 chars

            var jObj = JObject.FromObject(new { seq = seq, stepname = stepName, reason = reasonFor, processmessage = processText, processerror = processError });

            JArray arr = (rec == null) ? new() : (rec.Detail.TryParseToJArray() ?? new());

            arr.Add(jObj);
            request.Detail = arr.ToString();

            await _terminologyAPIService.UpdateTerminologyBGTask(request);
        }

        protected async Task<bool> CheckIfImportIsProcessing(List<string> taskNamesToCheckFor)
        {
            var getTaskByNameResponse = await _terminologyAPIService.GetTaskByNames(taskNamesToCheckFor, 2);//2=processing

            var isImportUnderProcessing = getTaskByNameResponse?.Data?.Any(rec => rec.StatusCd == 2);//any record in processing status

            return isImportUnderProcessing == true;
        }

        protected async Task<List<BackgroundTaskAPIModel>?> PickIfTasksAvailableForProcessAndMarkStatusAsProcessing(List<string> taskNamesToLookFor, string statusNameToBeUpdated )
        {
            var request = new GetTaskByNamesAndUpdateStatusRequestAPIModel
            {
                StatusCdToUpdate = 2,
                StatusToUpdate = statusNameToBeUpdated,
                StatusCodeToLookFor = 1,
                TaskNamesToLookFor = taskNamesToLookFor
            };

            var getTaskByNameResponse = await _terminologyAPIService.GetTaskByNamesAndUpdateStatus(request);

            return getTaskByNameResponse?.Data;
        }

        protected async Task RevertToPendingForProcessing(string statusToUpdate, List<string> taskNamesToLookFor)
        {
            var request = new GetTaskByNamesAndUpdateStatusRequestAPIModel
            {
                StatusCdToUpdate = 1,
                StatusToUpdate = statusToUpdate,
                StatusCodeToLookFor = 2,
                TaskNamesToLookFor = taskNamesToLookFor
            };

            await _terminologyAPIService.GetTaskByNamesAndUpdateStatus(request);
        }
    }
}