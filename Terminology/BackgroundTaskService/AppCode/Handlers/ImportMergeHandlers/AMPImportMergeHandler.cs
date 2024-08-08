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
    public class AMPImportMergeHandler : ImportMergeHandler
    {
        private IMapper _mapper;
        private DMDDetailResultDTO _dMDDetailResultDTO;
        private FormularyHeader _formularyDAO;
        private FormularyHeader _existingFormulary;

        public AMPImportMergeHandler(IMapper mapper, DMDDetailResultDTO dMDDetailResultDTO, FormularyHeader formularyDAO, FormularyHeader existingFormulary)
        {
            _mapper = mapper;
            _dMDDetailResultDTO = dMDDetailResultDTO;

            _formularyDAO = formularyDAO;
            _existingFormulary = existingFormulary;
        }

        public override void MergeFromExisting()
        {
            //_formularyDAO.FormularyId = _existingFormulary.FormularyId;//MMC-477:let the callee decide
            //_formularyDAO.VersionId = _existingFormulary.VersionId + 1;
            //_formularyDAO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT;
            _formularyDAO.MetaInfoJson = _existingFormulary.MetaInfoJson;
            _formularyDAO.IsDmdDeleted = _existingFormulary.IsDmdDeleted;

            MergeFormularyLocalRoutes();
            MergeFormularyAdditionalCodes();
            MergeFormularyDetail();
        }

        private void MergeFormularyLocalRoutes()
        {
            var activeFormularyLocalRoutes = _existingFormulary.FormularyLocalRouteDetail;
            var formularyDetail = _formularyDAO.FormularyDetail.First();

            _formularyDAO.FormularyLocalRouteDetail = _formularyDAO.FormularyLocalRouteDetail ?? new List<FormularyLocalRouteDetail>();

            //var routesWithPrevCodeLkp = DMDLookupProvider?._routesLkpWithAllAttributesForPrevCode;

            foreach (var res in activeFormularyLocalRoutes)
            {
                //mmc-612 - copy localunlicensed only
                if (res.RouteFieldTypeCd != TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED) continue;

                var routesCd = res.RouteCd;
                var routesDesc = res.RouteDesc;

                //if (routesWithPrevCodeLkp.IsCollectionValid() && routesCd.IsNotEmpty())
                //{
                //    if (routesWithPrevCodeLkp.ContainsKey(res.RouteCd))
                //    {
                //        routesDesc = routesWithPrevCodeLkp[res.RouteCd].Desc;
                //        routesCd = routesWithPrevCodeLkp[res.RouteCd].Cd;
                //    }
                //}
                var localRoute = new FormularyLocalRouteDetail
                {
                    Createdby = res.Createdby,
                    Createddate = DateTime.Now.ToUniversalTime(),
                    Createdtimestamp = DateTime.Now,
                    FormularyVersionId = formularyDetail.FormularyVersionId,
                    RouteCd = routesCd,
                    RouteDesc = routesDesc,
                    RouteFieldTypeCd = res.RouteFieldTypeCd,
                    RowId = Guid.NewGuid().ToString(),
                    Source = res.Source,
                    Updatedby = res.Updatedby,
                    Updateddate = DateTime.Now.ToUniversalTime(),
                    Updatedtimestamp = DateTime.Now
                };

                _formularyDAO.FormularyLocalRouteDetail.Add(localRoute);
            }
        }

        private void MergeFormularyAdditionalCodes()
        {
            if (!_existingFormulary.FormularyAdditionalCode.IsCollectionValid()) return;

            _formularyDAO.FormularyAdditionalCode = _formularyDAO.FormularyAdditionalCode ?? new List<FormularyAdditionalCode>();

            //var customManualSourceItems = _existingFormulary.FormularyAdditionalCode.Where(rec => (string.Compare(rec.CodeType, "identification", true) == 0) && (rec.Source == TerminologyConstants.MANUAL_DATA_SRC || rec.Source.IsEmpty())).ToList();

            //MMC-477 - Any custom to be copied back
            var customManualSourceItems = _existingFormulary.FormularyAdditionalCode.Where(rec => (rec.Source == TerminologyConstants.MANUAL_DATA_SRC || rec.Source.IsEmpty())).ToList();

            customManualSourceItems?.Each(rec =>
            {
                FormularyAdditionalCode additionalCode = new();
                additionalCode.AdditionalCode = rec.AdditionalCode;
                additionalCode.AdditionalCodeDesc = rec.AdditionalCodeDesc;
                additionalCode.AdditionalCodeSystem = rec.AdditionalCodeSystem;
                additionalCode.Attr1 = rec.Attr1;
                additionalCode.FormularyVersion = rec.FormularyVersion;
                additionalCode.CodeType= rec.CodeType;
                additionalCode.MetaJson=rec.MetaJson;
                additionalCode.Tenant = rec.Tenant;
                additionalCode.Source = TerminologyConstants.MANUAL_DATA_SRC;

                _formularyDAO.FormularyAdditionalCode.Add(additionalCode);
            });
        }

        private void MergeFormularyDetail()
        {
            if (!_existingFormulary.FormularyDetail.IsCollectionValid() || !_formularyDAO.FormularyDetail.IsCollectionValid()) return;

            var activeFormularyDetail = _existingFormulary.FormularyDetail.First();

            var formularyDetail = _formularyDAO.FormularyDetail.First();

            //var formularyDetail = new FormularyDetail();

            formularyDetail.RnohFormularyStatuscd = activeFormularyDetail.RnohFormularyStatuscd;
            formularyDetail.LocalLicensedUse = activeFormularyDetail.LocalLicensedUse;
            formularyDetail.LocalUnlicensedUse = activeFormularyDetail.LocalUnlicensedUse;
            formularyDetail.RoundingFactorCd = activeFormularyDetail.RoundingFactorCd;
            formularyDetail.CustomWarning = activeFormularyDetail.CustomWarning;
            formularyDetail.Reminder = activeFormularyDetail.Reminder;
            formularyDetail.Endorsement = activeFormularyDetail.Endorsement;
            formularyDetail.MedusaPreparationInstructions = activeFormularyDetail.MedusaPreparationInstructions;
            formularyDetail.TitrationTypeCd = activeFormularyDetail.TitrationTypeCd;
            formularyDetail.Diluent = activeFormularyDetail.Diluent;
            formularyDetail.ClinicalTrialMedication = activeFormularyDetail.ClinicalTrialMedication;
            formularyDetail.CriticalDrug = activeFormularyDetail.CriticalDrug;
            formularyDetail.IsGastroResistant = activeFormularyDetail.IsGastroResistant;
            formularyDetail.IsModifiedRelease = activeFormularyDetail.IsModifiedRelease;
            formularyDetail.ExpensiveMedication = activeFormularyDetail.ExpensiveMedication;
            formularyDetail.HighAlertMedication = activeFormularyDetail.HighAlertMedication;
            formularyDetail.HighAlertMedicationSource = activeFormularyDetail.HighAlertMedicationSource;
            formularyDetail.IvToOral = activeFormularyDetail.IvToOral;
            formularyDetail.NotForPrn = activeFormularyDetail.NotForPrn;
            formularyDetail.IsBloodProduct = activeFormularyDetail.IsBloodProduct;
            formularyDetail.IsDiluent = activeFormularyDetail.IsDiluent;
            formularyDetail.Prescribable = activeFormularyDetail.Prescribable == null ? false : activeFormularyDetail.Prescribable;
            formularyDetail.PrescribableSource = activeFormularyDetail.PrescribableSource;
            formularyDetail.OutpatientMedicationCd = activeFormularyDetail.OutpatientMedicationCd;
            formularyDetail.IgnoreDuplicateWarnings = activeFormularyDetail.IgnoreDuplicateWarnings;
            formularyDetail.IsCustomControlledDrug = activeFormularyDetail.IsCustomControlledDrug;
            formularyDetail.IsPrescriptionPrintingRequired = activeFormularyDetail.IsPrescriptionPrintingRequired;
            formularyDetail.IsIndicationMandatory = activeFormularyDetail.IsIndicationMandatory;
            formularyDetail.WitnessingRequired = activeFormularyDetail.WitnessingRequired;
            //formularyHeader.FormularyDetail.Add(formularyDetail);

        }
    }
}