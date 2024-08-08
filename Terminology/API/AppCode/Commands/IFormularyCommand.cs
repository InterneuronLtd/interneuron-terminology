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
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
using Interneuron.Terminology.Infrastructure.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Commands
{
    public interface IFormularyCommands
    {
        Task<ImportFormularyResultsDTO> ImportByCodes(List<string> dmdCodes, string defaultFormularyStatusCode = TerminologyConstants.FORMULARYSTATUS_FORMULARY, string defaultRecordStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT);

        Task<UpdateFormularyRecordStatusDTO> UpdateFormularyRecordStatus(UpdateFormularyRecordStatusRequest request, Action<List<string>> onUpdate = null);

        Task<UpdateFormularyRecordStatusDTO> BulkUpdateFormularyRecordStatus(UpdateFormularyRecordStatusRequest request);

        Task<CreateEditFormularyDTO> CreateFormulary(CreateEditFormularyRequest request, Action<List<string>> onCreateComplete = null);

        Task<CreateEditFormularyDTO> UpdateFormulary(CreateEditFormularyRequest request, Action<List<string>> onUpdate = null);

        Task<ImportFormularyResultsDTO> FileImport(CreateEditFormularyRequest request);

        void ChangeFDBDataSchema();//TBR

        Task<StatusDTO> ImportAllDMDCodes();

        Task InvokePostImportProcess(Action onComplete = null);

        Task InvokePostImportProcessForCodes(List<string> codes, Action<List<string>> onComplete = null);

        Task<StatusDTO> SaveFormularyUsageStat(List<SaveFormularyUsageStatRequest> request);

        Task<StatusDTO> SeedFormularyUsageStatForEPMA(List<FormularyUsageStatDTO> dtos);

        //Task<UpdateFormularyRecordStatusDTO> UpdateVMPFormularyRecordStatus(UpdateFormularyRecordStatusRequest request);

        //Task<UpdateFormularyRecordStatusDTO> UpdateVTMFormularyRecordStatus(UpdateFormularyRecordStatusRequest request);

        Task UpdateDefaultBNFs();

        Task UpdateBNFsDescription();

        Task AddBNFClassificationsInHierarchy();
        Task FixExistingLocalAndUnlicensedIndications();
        Task FixExistingFDBCodesByDescription(string token);

        Task<bool> GetHeaderRecordsLock(List<string> formularyVersionIds);

        Task TryReleaseHeaderRecordsLock(List<string> formularyVersionIds);
    }
}
