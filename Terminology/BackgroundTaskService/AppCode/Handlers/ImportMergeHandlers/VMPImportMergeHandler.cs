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
﻿using AutoMapper;
using Interneuron.Common.Extensions;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;

namespace Interneuron.Terminology.BackgroundTaskService.API.AppCode.Commands.ImportMergeHandlers
{
    public class VMPImportMergeHandler : ImportMergeHandler
    {
        private IMapper _mapper;
        private DMDDetailResultDTO _dMDDetailResultDTO;
        private FormularyHeader _formularyDAO;
        private FormularyHeader _existingFormulary;

        public VMPImportMergeHandler(IMapper mapper, DMDDetailResultDTO dMDDetailResultDTO, FormularyHeader formularyDAO, FormularyHeader existingFormulary)
        {
            _mapper = mapper;
            _dMDDetailResultDTO = dMDDetailResultDTO;

            _formularyDAO = formularyDAO;
            _existingFormulary = existingFormulary;
        }

        public override void MergeFromExisting()
        {
            //MMC-477 - FormularyId changes - FormularyId should be new for every new import
            //_formularyDAO.FormularyId = _existingFormulary.FormularyId;
            //_formularyDAO.VersionId = _existingFormulary.VersionId + 1;
            _formularyDAO.MetaInfoJson = _existingFormulary.MetaInfoJson;
            _formularyDAO.IsDmdDeleted = _existingFormulary.IsDmdDeleted;

            // _existingFormulary.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE ? TerminologyConstants.RECORDSTATUS_ACTIVE : TerminologyConstants.RECORDSTATUS_DRAFT;

            MergeFormularyAdditionalCodes();
            MergeFormularyDetail();
        }

        private void MergeFormularyAdditionalCodes()
        {
            if (!_existingFormulary.FormularyAdditionalCode.IsCollectionValid()) return;

            _formularyDAO.FormularyAdditionalCode = _formularyDAO.FormularyAdditionalCode ?? new List<FormularyAdditionalCode>();

            var customManualSourceItems = _existingFormulary.FormularyAdditionalCode.Where(rec => (string.Compare(rec.CodeType, "identification", true) == 0) && (rec.Source == TerminologyConstants.MANUAL_DATA_SRC || rec.Source.IsEmpty())).ToList();

            customManualSourceItems?.Each(rec =>
            {
                FormularyAdditionalCode additionalCode = new();
                additionalCode.AdditionalCode = rec.AdditionalCode;
                additionalCode.AdditionalCodeDesc = rec.AdditionalCodeDesc;
                additionalCode.AdditionalCodeSystem = rec.AdditionalCodeSystem;
                additionalCode.Attr1 = rec.Attr1;
                additionalCode.FormularyVersion = rec.FormularyVersion;
                additionalCode.CodeType = rec.CodeType;
                additionalCode.MetaJson = rec.MetaJson;
                additionalCode.Tenant = rec.Tenant;
                additionalCode.Source = TerminologyConstants.MANUAL_DATA_SRC;

                _formularyDAO.FormularyAdditionalCode.Add(additionalCode);
            });
        }

        private void MergeFormularyDetail()
        {
            if (!_existingFormulary.FormularyDetail.IsCollectionValid() || !_formularyDAO.FormularyDetail.IsCollectionValid()) return;

            var existingDetailFromDb = _existingFormulary.FormularyDetail.First();

            var _formularyDetailFromSrc = _formularyDAO.FormularyDetail.First();

            //if (!(existingDetailFromDb.Prescribable == false && string.Compare(existingDetailFromDb.PrescribableSource, TerminologyConstants.DMD_DATA_SRC, true) == 0))
            if (string.Compare(existingDetailFromDb.PrescribableSource ?? "", TerminologyConstants.DMD_DATA_SRC, true) != 0)
            {
                _formularyDetailFromSrc.Prescribable = existingDetailFromDb.Prescribable;
                _formularyDetailFromSrc.PrescribableSource = existingDetailFromDb.PrescribableSource;
            }

            _formularyDetailFromSrc.WitnessingRequired = existingDetailFromDb.WitnessingRequired;
        }
    }
}
