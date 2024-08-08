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
using Interneuron.Terminology.API.AppCode.Extensions;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Model.Search;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Queries
{
    public class RuleBoundVMPFormularyBuilder : RuleBoundBaseFormularyBuilder
    {
        private List<FormularyHeader> _childActiveAMPFormularies = new List<FormularyHeader>();
        private List<FormularyBasicSearchResultModel> _childFormularies = new List<FormularyBasicSearchResultModel>();
        private ConcurrentBag<FormularyDTO> _childFormulariesDTO = new ConcurrentBag<FormularyDTO>();
        private ConcurrentBag<FormularyCustomWarningDTO> _childCustomWarningsDTO = new ConcurrentBag<FormularyCustomWarningDTO>();
        private ConcurrentBag<FormularyReminderDTO> _childRemindersDTO = new ConcurrentBag<FormularyReminderDTO>();
        private ConcurrentBag<string> _childEndorsementsDTO = new ConcurrentBag<string>();
        private ConcurrentBag<string> _childBlackTrianglesDTO = new ConcurrentBag<string>();
        private ConcurrentBag<FormularyAdditionalCodeDTO> _childAdditionalCodesDTO = new ConcurrentBag<FormularyAdditionalCodeDTO>();
        private ConcurrentBag<FormularyRouteDetailDTO> _childRouteDetailsDTO = new ConcurrentBag<FormularyRouteDetailDTO>();
        private ConcurrentBag<FormularyLocalRouteDetailDTO> _childLocalRouteDetailsDTO = new ConcurrentBag<FormularyLocalRouteDetailDTO>();
        private ConcurrentBag<FormularyDetailDTO> _childFormulariesDetailDTO = new ConcurrentBag<FormularyDetailDTO>();
        private ConcurrentBag<FormularyIngredientDTO> _childIngredientsDTO = new ConcurrentBag<FormularyIngredientDTO>();
        private ConcurrentBag<FormularyLookupItemDTO> _childLicensedIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<FormularyLookupItemDTO> _childUnlicensedIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<FormularyLookupItemDTO> _childLocalLicensedIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<FormularyLookupItemDTO> _childLocalUnlicensedIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<FormularyLookupItemDTO> _childContraIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<string> _childMedusaPreparationInstructionsDTO = new ConcurrentBag<string>();
        private ConcurrentBag<string> _childNiceTADTO = new ConcurrentBag<string>();
        private ConcurrentBag<FormularyLookupItemDTO> _childSideEffectsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<bool> _childCriticalDrugFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<bool> _childUnlicensedMedicationFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<FormularyLookupItemDTO> _childCautionsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<bool> _childClinicalTrialFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<bool> _childIVTOOralFlagsDTO = new ConcurrentBag<bool>();

        private ConcurrentBag<bool> _childEMAAdditonalFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<bool> _childExpensiveMedicationFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<bool> _childHighAlertMedicationFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<bool> _childNotForPRNFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<bool> _childOutpatientMedicationFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<string> _childPrescribingstatusDTO = new ConcurrentBag<string>();
        private ConcurrentBag<bool> _childIgnoreDuplicateWarningsFlagsDTO = new ConcurrentBag<bool>();
        private ConcurrentBag<FormularyLookupItemDTO> _childTitrationTypeDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<bool?> _childBloodProductFlagsDTO = new ConcurrentBag<bool?>();
        private ConcurrentBag<bool?> _childDiluentFlagsDTO = new ConcurrentBag<bool?>();
        private ConcurrentBag<bool?> _childGastroResistantFlagsDTO = new ConcurrentBag<bool?>();
        private ConcurrentBag<bool?> _childMdifiedReleaseFlagsDTO = new ConcurrentBag<bool?>();
        private ConcurrentBag<bool?> _childCustomComtrolledDrugFlagsDTO = new ConcurrentBag<bool?>();
        private ConcurrentBag<bool?> _childPrescriptionPrintingRequiredFlagsDTO = new ConcurrentBag<bool?>();
        private ConcurrentBag<string> _childRecordsStatusDTO = new ConcurrentBag<string>();
        private ConcurrentBag<FormularyLookupItemDTO> _childDiluentsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<bool?> _childIndicationMandatoryFlagsDTO = new ConcurrentBag<bool?>();
        private ConcurrentBag<string> _childModifiedReleaseDTO = new ConcurrentBag<string>();
        private ComparisonConfig _config;

        public RuleBoundVMPFormularyBuilder(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _config = GetHeaderConfig();
        }
        public override async Task CreateBase(FormularyHeader formularyDAO)
        {
            await base.CreateBase(formularyDAO);

            await FillAMPProperties();

            //AssignRecordStatus();

            //_formularyDTO.RecStatusCode = GetHighestPrecedenceRecordStatusCode();
        }


        private async Task FetchChildAMPFormularies()
        {
            if (base.DescendentFormularies.IsCollectionValid())
            {
                _childActiveAMPFormularies = base.DescendentFormularies.Where(rec => string.Compare(rec.ProductType, "AMP", true) == 0 && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)?.ToList();
                return;
            }

            //MMC-477 -- formularyid changes
            /*
            var repo = _provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

            var nodes = await repo.GetFormularyDescendentForCodes(new string[] { _formularyDAO.Code });

            if (!nodes.IsCollectionValid()) return;

            _childFormularies = nodes.ToList();

            var childIds = nodes.Where(rec => string.Compare(rec.ProductType, "AMP", true) == 0 && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)?.Select(rec => rec.FormularyVersionId)?.ToList();

            if (!childIds.IsCollectionValid()) return;

            var formularyRepo = _provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            _childActiveAMPFormularies = formularyRepo.GetFormularyListForIds(childIds)?.ToList();
            */
        }

        private async Task FillAMPProperties()
        {
            await FetchChildAMPFormularies();

            if (!_childActiveAMPFormularies.IsCollectionValid()) return;

            _childActiveAMPFormularies.AsParallel().Each(child =>
            {
                var header = _mapper.Map<FormularyDTO>(child);

                var detail = child.FormularyDetail.FirstOrDefault();

                if (detail != null)
                {
                    header.Detail = _mapper.Map<FormularyDetailDTO>(detail);

                    _childFormulariesDetailDTO.Add(header.Detail);

                    header.Detail.CustomWarnings?.Each(rec => { _childCustomWarningsDTO.Add(rec); });
                    header.Detail.Reminders?.Each(rec => { _childRemindersDTO.Add(rec); });
                    header.Detail.Endorsements?.Each(rec => { _childEndorsementsDTO.Add(rec); });

                    _childBlackTrianglesDTO.Add(detail.BlackTriangle);

                    header.Detail.LicensedUses?.Each(rec => _childLicensedIndicationsDTO.Add(rec));
                    header.Detail.UnLicensedUses?.Each(rec => _childUnlicensedIndicationsDTO.Add(rec));

                    header.Detail.LocalLicensedUses?.Each(rec => _childLocalLicensedIndicationsDTO.Add(rec));
                    header.Detail.LocalUnLicensedUses?.Each(rec => _childLocalUnlicensedIndicationsDTO.Add(rec));

                    header.Detail.ContraIndications?.Each(rec => _childContraIndicationsDTO.Add(rec));
                    header.Detail.MedusaPreparationInstructions?.Each(rec => _childMedusaPreparationInstructionsDTO.Add(rec));
                    header.Detail.NiceTas?.Each(rec => _childNiceTADTO.Add(rec));

                    header.Detail.Cautions?.Each(rec => _childCautionsDTO.Add(rec));

                    header.Detail.SideEffects?.Each(rec => _childSideEffectsDTO.Add(rec));

                    header.Detail.Diluents?.Each(rec => _childDiluentsDTO.Add(rec));

                    _childCriticalDrugFlagsDTO.Add(header.Detail.CriticalDrug == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childClinicalTrialFlagsDTO.Add(header.Detail.ClinicalTrialMedication == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childIVTOOralFlagsDTO.Add(header.Detail.IvToOral == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childEMAAdditonalFlagsDTO.Add(header.Detail.EmaAdditionalMonitoring == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childExpensiveMedicationFlagsDTO.Add(header.Detail.ExpensiveMedication == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childUnlicensedMedicationFlagsDTO.Add(header.Detail.UnlicensedMedicationCd == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childHighAlertMedicationFlagsDTO.Add(header.Detail.HighAlertMedication == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childNotForPRNFlagsDTO.Add(header.Detail.NotForPrn == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childOutpatientMedicationFlagsDTO.Add(header.Detail.OutpatientMedicationCd == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    if (header.Detail.PrescribingStatusCd.IsNotEmpty())
                        _childPrescribingstatusDTO.Add(header.Detail.PrescribingStatusCd);

                    header.Detail.TitrationTypes?.Each(rec => _childTitrationTypeDTO.Add(rec));

                    _childIgnoreDuplicateWarningsFlagsDTO.Add(header.Detail.IgnoreDuplicateWarnings == TerminologyConstants.STRINGIFIED_BOOL_TRUE);

                    _childBloodProductFlagsDTO.Add(header.Detail.IsBloodProduct);

                    _childDiluentFlagsDTO.Add(header.Detail.IsDiluent);

                    _childGastroResistantFlagsDTO.Add(header.Detail.IsGastroResistant);

                    _childMdifiedReleaseFlagsDTO.Add(header.Detail.IsModifiedRelease);

                    _childCustomComtrolledDrugFlagsDTO.Add(header.Detail.IsCustomControlledDrug);

                    _childPrescriptionPrintingRequiredFlagsDTO.Add(header.Detail.IsPrescriptionPrintingRequired);

                    _childIndicationMandatoryFlagsDTO.Add(header.Detail.IsIndicationMandatory);

                    header.Detail.ModifiedReleases?.Each(rec => _childModifiedReleaseDTO.Add(rec));
                }

                if (child.FormularyIngredient.IsCollectionValid())
                {
                    header.FormularyIngredients = _mapper.Map<List<FormularyIngredientDTO>>(child.FormularyIngredient);
                    header.FormularyIngredients.Each(rec => _childIngredientsDTO.Add(rec));
                }

                if (child.FormularyRouteDetail.IsCollectionValid())
                {
                    header.FormularyRouteDetails = _mapper.Map<List<FormularyRouteDetailDTO>>(child.FormularyRouteDetail);
                    header.FormularyRouteDetails.Each(rec => _childRouteDetailsDTO.Add(rec));
                }

                if (child.FormularyLocalRouteDetail.IsCollectionValid())
                {
                    header.FormularyLocalRouteDetails = _mapper.Map<List<FormularyLocalRouteDetailDTO>>(child.FormularyLocalRouteDetail);
                    header.FormularyLocalRouteDetails.Each(rec => _childLocalRouteDetailsDTO.Add(rec));
                }

                if (child.FormularyAdditionalCode.IsCollectionValid())
                {
                    header.FormularyAdditionalCodes = _mapper.Map<List<FormularyAdditionalCodeDTO>>(child.FormularyAdditionalCode);
                    header.FormularyAdditionalCodes.Each(rec => _childAdditionalCodesDTO.Add(rec));
                }

                _childFormulariesDTO.Add(header);
            });
        }

        private bool HasSameIngredients()
        {
            var prev = _childActiveAMPFormularies.IsCollectionValid() ? _childActiveAMPFormularies[0].FormularyIngredient: null;
            if (prev == null) return true;
            for (var ampIndex = 1; ampIndex < _childActiveAMPFormularies.Count; ampIndex++)
            {
                var current = _childActiveAMPFormularies[ampIndex].FormularyIngredient;

                var prodPropertiesComparer = new CompareLogic(_config);
                var result = prodPropertiesComparer.Compare(prev, current);
                var diffs = result.Differences;
                if (diffs.IsCollectionValid()) return false;
                prev = current;
            }

            return true;
        }

        private bool HasSameRoutes()
        {
            var prev = _childActiveAMPFormularies.IsCollectionValid() ? _childActiveAMPFormularies[0].FormularyRouteDetail : null;
            if (prev == null) return true;
            for (var ampIndex = 1; ampIndex < _childActiveAMPFormularies.Count; ampIndex++)
            {
                var current = _childActiveAMPFormularies[ampIndex].FormularyRouteDetail;

                var prodPropertiesComparer = new CompareLogic(_config);
                var result = prodPropertiesComparer.Compare(prev, current);
                var diffs = result.Differences;
                if (diffs.IsCollectionValid()) return false;
                prev = current;
            }

            return true;
        }

        private ComparisonConfig GetHeaderConfig()
        {
            ComparisonConfig config = new()
            {
                MaxDifferences = 10000000,
                IgnoreCollectionOrder = true
            };
            config.CustomComparers.Add(new BooleanAndNullComparer(RootComparerFactory.GetRootComparer()));
            config.CustomComparers.Add(new StringFalseAndNullComparer(RootComparerFactory.GetRootComparer()));
            config.CustomComparers.Add(new CustomComparer<FormularyIngredient, FormularyIngredient>(
                (st1, st2) => !(st1.IngredientCd != st2.IngredientCd || st1.IngredientName != st2.IngredientName || st1.BasisOfPharmaceuticalStrengthCd != st2.BasisOfPharmaceuticalStrengthCd || st1.StrengthValueDenominator != st2.StrengthValueDenominator || st1.StrengthValueDenominatorUnitCd != st2.StrengthValueDenominatorUnitCd || st1.StrengthValueNumerator != st2.StrengthValueNumerator || st1.StrengthValueNumeratorUnitCd != st2.StrengthValueNumeratorUnitCd)
            ));
            config.CustomComparers.Add(new CustomComparer<FormularyRouteDetail, FormularyRouteDetail>(
            (st1, st2) => !(st1.RouteCd != st2.RouteCd || st1.RouteFieldTypeCd != st2.RouteFieldTypeCd)
            ));
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Updatedtimestamp);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Updateddate);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Updatedby);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Createdby);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Createddate);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.FormularyVersion);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Createdtimestamp);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.FormularyVersionId);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.RouteDesc);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.RowId);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Source);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Tenant);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Timezonename);
            config.IgnoreProperty<FormularyRouteDetail>(x => x.Timezoneoffset);

            config.IgnoreProperty<FormularyIngredient>(x => x.Updatedtimestamp);
            config.IgnoreProperty<FormularyIngredient>(x => x.Updateddate);
            config.IgnoreProperty<FormularyIngredient>(x => x.Updatedby);
            config.IgnoreProperty<FormularyIngredient>(x => x.Createdby);
            config.IgnoreProperty<FormularyIngredient>(x => x.Createddate);
            config.IgnoreProperty<FormularyIngredient>(x => x.FormularyVersion);
            config.IgnoreProperty<FormularyIngredient>(x => x.Createdtimestamp);
            config.IgnoreProperty<FormularyIngredient>(x => x.FormularyVersionId);
            config.IgnoreProperty<FormularyIngredient>(x => x.RowId);
            config.IgnoreProperty<FormularyIngredient>(x => x.BasisOfPharmaceuticalStrengthDesc);
            config.IgnoreProperty<FormularyIngredient>(x => x.StrengthValueDenominatorUnitDesc);
            config.IgnoreProperty<FormularyIngredient>(x => x.StrengthValueNumeratorUnitDesc);
            config.IgnoreProperty<FormularyIngredient>(x => x.Tenant);
            config.IgnoreProperty<FormularyIngredient>(x => x.Timezonename);
            config.IgnoreProperty<FormularyIngredient>(x => x.Timezoneoffset);

            return config;
        }

        public override void CreateDetails()
        {
            base.CreateDetails();

            var anyChildFormularyDetailDTO = _childFormulariesDetailDTO.FirstOrDefault() ?? new FormularyDetailDTO();

            if (!_childActiveAMPFormularies.IsCollectionValid()) return;

            //Overwrite VMP properties from AMP
            var detailDTO = _formularyDTO.Detail;

            //MMC-477 - Should return null if there are multiple distinct data in child AMP's.
            //When derived from the parent VMP, should have same data. But can change if the new version of the VMP has different info.
            //All the active child AMPs should have derived from same version of parent VMP and user should make sure this in MMC interface
            var hasMultipleBasisOfPreferredName = CheckHasMultipleRecordsInChildFormularyDetail(rec => rec.BasisOfPreferredNameCd.IsNotEmpty(), rec=> rec.BasisOfPreferredNameCd);

            detailDTO.BasisOfPreferredNameCd = hasMultipleBasisOfPreferredName ? null: anyChildFormularyDetailDTO?.BasisOfPreferredNameCd;
            detailDTO.BasisOfPreferredNameDesc = hasMultipleBasisOfPreferredName ? null : anyChildFormularyDetailDTO?.BasisOfPreferredNameDesc;

            detailDTO.LicensedUses = _childLicensedIndicationsDTO?.Distinct(rec => rec.Cd).ToList();
            detailDTO.UnLicensedUses = _childUnlicensedIndicationsDTO?.Distinct(rec => rec.Cd).ToList();

            detailDTO.LocalLicensedUses = _childLocalLicensedIndicationsDTO?.Distinct(rec => rec.Cd).ToList();
            detailDTO.LocalUnLicensedUses = _childLocalUnlicensedIndicationsDTO?.Distinct(rec => rec.Cd).ToList();

            List<FormularyLookupItemDTO> tempLocalUnlicensedUses = new List<FormularyLookupItemDTO>();

            tempLocalUnlicensedUses.AddRange(detailDTO.LocalUnLicensedUses);

            for (int licUseIndex = 0; licUseIndex < detailDTO.LocalUnLicensedUses.Count(); licUseIndex++)
            {
                if (detailDTO.LocalLicensedUses.Exists(x => x.Cd == detailDTO.LocalUnLicensedUses[licUseIndex].Cd))
                {
                    tempLocalUnlicensedUses.Remove(detailDTO.LocalUnLicensedUses[licUseIndex]);
                }
            }

            detailDTO.LocalUnLicensedUses.Clear();

            detailDTO.LocalUnLicensedUses.AddRange(tempLocalUnlicensedUses);

            //MMC-477
            var hasMultipleDoseForm = CheckHasMultipleRecordsInChildFormularyDetail(rec => rec.DoseFormCd.IsNotEmpty(), rec=> rec.DoseFormCd);

            detailDTO.DoseFormCd = hasMultipleDoseForm ? null : anyChildFormularyDetailDTO?.DoseFormCd;
            detailDTO.DoseFormDesc = hasMultipleDoseForm ? null : anyChildFormularyDetailDTO?.DoseFormDesc;

            //MMC-477
            var roundingFactor = _childFormulariesDetailDTO?
                .Where(rec => rec.RoundingFactorCd.IsNotEmpty())?
                .Select(rec => rec.RoundingFactorCd)
                .Distinct()
                .ToList();
            //var roundingFactor = new HashSet<string>();
            //_childFormulariesDetailDTO?.Each(rec =>
            //{
            //    var val = rec.RoundingFactorCd.IsNotEmpty() ? rec.RoundingFactorCd : "__ignore";
            //    if (!roundingFactor.Contains(val))
            //        roundingFactor.Add(val);
            //});

            //MMC-477 - Assign if there is only one rounding factor for all its child AMPs. Otherwise, it is incorrect and no need to send that data.
            if (roundingFactor.IsCollectionValid() && roundingFactor.Count == 1)
                detailDTO.RoundingFactorCd = roundingFactor.First();
            //if (roundingFactor.Count == 1 && !roundingFactor.Contains("__ignore"))
            //    detailDTO.RoundingFactorCd = roundingFactor.First();

            //MMC-477
            var hasMultipleForm = CheckHasMultipleRecordsInChildFormularyDetail(rec => rec.FormCd.IsNotEmpty(), rec=> rec.FormCd);
            detailDTO.FormCd = hasMultipleForm ? null : anyChildFormularyDetailDTO?.FormCd;
            detailDTO.FormDesc = hasMultipleForm ? null : anyChildFormularyDetailDTO?.FormDesc;

            //MMC-477
            var hasMultipleUnitDoseFormSize = CheckHasMultipleRecordsInChildFormularyDetail(rec => rec.UnitDoseFormSize != null, rec=> rec.UnitDoseFormSize);
            detailDTO.UnitDoseFormSize = hasMultipleUnitDoseFormSize ? null : anyChildFormularyDetailDTO?.UnitDoseFormSize;
            //MMC-477
            var hasMultipleUnitDoseFormUnits = CheckHasMultipleRecordsInChildFormularyDetail(rec => rec.UnitDoseFormUnits.IsNotEmpty(), rec => rec.UnitDoseFormUnits);
            detailDTO.UnitDoseFormUnits = hasMultipleUnitDoseFormUnits ? null : anyChildFormularyDetailDTO?.UnitDoseFormUnits;
            detailDTO.UnitDoseFormUnitsDesc = hasMultipleUnitDoseFormUnits ? null : anyChildFormularyDetailDTO?.UnitDoseFormUnitsDesc;

            //MMC-477
            var hasMultipleUnitDoseUnitOfMeasureCd = CheckHasMultipleRecordsInChildFormularyDetail(rec => rec.UnitDoseUnitOfMeasureCd.IsNotEmpty(), rec => rec.UnitDoseUnitOfMeasureCd);
            detailDTO.UnitDoseUnitOfMeasureCd = hasMultipleUnitDoseUnitOfMeasureCd ? null : anyChildFormularyDetailDTO?.UnitDoseUnitOfMeasureCd;
            detailDTO.UnitDoseUnitOfMeasureDesc = hasMultipleUnitDoseUnitOfMeasureCd? null : anyChildFormularyDetailDTO?.UnitDoseUnitOfMeasureDesc;

            detailDTO.ContraIndications = _childContraIndicationsDTO?.Distinct(rec => rec.Cd).ToList();

            AssignCustomWarnings();

            AssignReminders();

            detailDTO.Endorsements = _childEndorsementsDTO?.Distinct().ToList();
            detailDTO.MedusaPreparationInstructions = _childMedusaPreparationInstructionsDTO?.Distinct().ToList();

            detailDTO.NiceTas = _childNiceTADTO?.Distinct().ToList();

            detailDTO.SideEffects = _childSideEffectsDTO?.Distinct(rec => rec.Cd).ToList();

            detailDTO.Diluents = _childDiluentsDTO?.Distinct(rec => rec.Cd).ToList();

            AssignBlackTriangle(detailDTO);

            detailDTO.Cautions = _childCautionsDTO?.Distinct(rec => rec.Cd).ToList();

            //Set Free Flags
            AssignFreeFlags(detailDTO);

            AssignClinicalTrialFlag(detailDTO);

            AssignCriticalDrug(detailDTO);

            AssignIVTOOralFlag(detailDTO);

            //Controlled drug Category
            detailDTO.ControlledDrugCategories = anyChildFormularyDetailDTO?.ControlledDrugCategories;

            //Retain what is set for the VMP -- (Rule applied while import - so no need while fetch)
            //detailDTO.WitnessingRequired = (detailDTO.ControlledDrugCategoryCd == null || detailDTO.ControlledDrugCategoryCd != "0") ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;

            AssignEMAAdditionalFlag(detailDTO);

            AssignExpensiveMednFlag(detailDTO);

            AssignUnlicensedMednFlag(detailDTO);

            AssignHighAlertMednFlag(detailDTO);

            AssignNotForPRNFlag(detailDTO);

            AssignOutPatientMedicationFlag(detailDTO);

            //MMC-477
            var hasMultiplePrescribingStatusCd = CheckHasMultipleRecordsInChildFormularyDetail(rec => rec.PrescribingStatusCd.IsNotEmpty(), rec => rec.UnitDoseUnitOfMeasureCd);

            detailDTO.PrescribingStatusCd = hasMultiplePrescribingStatusCd ? null : anyChildFormularyDetailDTO?.PrescribingStatusCd;
            detailDTO.PrescribingStatusDesc = hasMultiplePrescribingStatusCd ? null : anyChildFormularyDetailDTO?.PrescribingStatusDesc;


            AssignTitrationTypes(detailDTO);

            AssignIgnoreDuplicateWarningsFlag(detailDTO);

            AssignIsBloodProductFlag(detailDTO);

            AssignIsDiluentFlag(detailDTO);

            AssignIsModifiedReleaseFlag(detailDTO);

            AssignIsGastroResistantFlag(detailDTO);

            AssignFormularyStatusFlag(detailDTO);

            AssignIsCustomControlledDrugFlag(detailDTO);

            AssignIsPrescriptionPrintingRequiredFlag(detailDTO);

            AssignIsIndicationMandatoryFlag(detailDTO);

            detailDTO.ModifiedReleases = _childModifiedReleaseDTO.ToList();
        }

        /// <summary>
        /// Verified whether the child amps has different data
        /// </summary>
        /// <typeparam name="TK"></typeparam>
        /// <param name="condition"></param>
        /// <param name="distinct"></param>
        /// <returns></returns>
        private bool CheckHasMultipleRecordsInChildFormularyDetail<TK>(Func<FormularyDetailDTO, bool> condition, Func<FormularyDetailDTO, TK> distinct)
        {
            if (!_childFormulariesDetailDTO.IsCollectionValid() || condition == null) return false;
            return _childFormulariesDetailDTO.Where(condition)?.Distinct(distinct).Count() > 1;
        }

        public override void CreateIngredients()
        {
            //check if all active drafts has same ingredients
            var hasSameIngredients = HasSameIngredients();
            if (!hasSameIngredients)
            {
                _formularyDTO.FormularyIngredients = null;
                return;
            }
            _formularyDTO.FormularyIngredients = _childIngredientsDTO?.Distinct(new FormularyIngredientDTOComparer()).ToList();
        }

        public override async Task CreateAdditionalCodes(bool getAllAdditionalCodes = false)
        {
            _formularyDTO.FormularyAdditionalCodes = new List<FormularyAdditionalCodeDTO>();
            //_formularyDTO.AllFormularyAdditionalCodes = new List<FormularyAdditionalCodeDTO>();

            if (_childActiveAMPFormularies.IsCollectionValid())
            {
                ProjectClassificationCodesFromChildNodes(_childAdditionalCodesDTO, getAllAdditionalCodes, (codeRec) => {
                    if (string.Compare(codeRec.AdditionalCodeSystem, "bnf", true) == 0 && codeRec.AdditionalCode.IsNotEmpty())
                    {
                        codeRec.AdditionalCode = codeRec.AdditionalCode.Length > 7 ? codeRec.AdditionalCode.Substring(0, 7) : codeRec.AdditionalCode;
                    }
                }, _formularyDTO);
            }

            //MMC-451: No need to overwrite since the user has option to select the default now
            //OverrideClassificationCodesByConfigValuesForVMP();

            if (!_formularyDAO.FormularyAdditionalCode.IsCollectionValid()) return;

            //Add Additional Identity Codes
            var addlIdentityCodes = _formularyDAO.FormularyAdditionalCode.Where(rec => string.Compare(rec.CodeType, TerminologyConstants.CODE_SYSTEM_IDENTIFICATION_TYPE, true) == 0)?.ToList();

            if (!addlIdentityCodes.IsCollectionValid()) return;
            var identityCodes = this._mapper.Map<List<FormularyAdditionalCodeDTO>>(addlIdentityCodes);
            _formularyDTO.FormularyAdditionalCodes.AddRange(identityCodes);
            //_formularyDTO.AllFormularyAdditionalCodes.AddRange(identityCodes);
        }

        public override void CreateRouteDetails()
        {
            //Convert routes of current record from DAO to DTO

            //No Need to consider the routes at VMP level - and hence commented below line
            //base.CreateRouteDetails();

            //Routes associated to the child AMPs are only aggregated and considered

            if (!_childRouteDetailsDTO.IsCollectionValid()) return;

            //check if all active drafts has same routes
            var hasSameRoutes = HasSameRoutes();

            if (!hasSameRoutes)
            {
                _formularyDTO.FormularyRouteDetails = null;
                return;
            }

            var licensedRoute = _childRouteDetailsDTO.Where(x => x.RouteFieldTypeCd == "003").Distinct(rec => rec.RouteCd).ToList();

            var unlicensedRoute = _childRouteDetailsDTO.Where(x => x.RouteFieldTypeCd == "002").Distinct(rec => rec.RouteCd).ToList();

            List<FormularyRouteDetailDTO> tempUnlicensedRoute = new List<FormularyRouteDetailDTO>();

            tempUnlicensedRoute.AddRange(unlicensedRoute);

            for (int i = 0; i < unlicensedRoute.Count(); i++)
            {
                if (licensedRoute.Exists(x => x.RouteCd == unlicensedRoute[i].RouteCd))
                {
                    tempUnlicensedRoute.Remove(unlicensedRoute[i]);
                }
            }

            _childRouteDetailsDTO.Clear();

            unlicensedRoute.Clear();

            unlicensedRoute.AddRange(tempUnlicensedRoute);

            foreach (var route in licensedRoute)
            {
                _childRouteDetailsDTO.Add(route);
            }

            foreach (var uroute in unlicensedRoute)
            {
                _childRouteDetailsDTO.Add(uroute);
            }

            _formularyDTO.FormularyRouteDetails = _childRouteDetailsDTO.ToList();
        }

        public override void CreateLocalRouteDetails()
        {
            //Convert routes of current record from DAO to DTO

            //No Need to consider the routes at VMP level - and hence commented below line
            //base.CreateRouteDetails();

            //Routes associated to the child AMPs are only aggregated and considered

            if (!_childLocalRouteDetailsDTO.IsCollectionValid()) return;

            var localLicensedRoute = _childLocalRouteDetailsDTO.Where(x => x.RouteFieldTypeCd == "003").Distinct(rec => rec.RouteCd).ToList();

            var localUnlicensedRoute = _childLocalRouteDetailsDTO.Where(x => x.RouteFieldTypeCd == "002").Distinct(rec => rec.RouteCd).ToList();

            List<FormularyLocalRouteDetailDTO> tempLocalUnlicensedRoute = new List<FormularyLocalRouteDetailDTO>();

            tempLocalUnlicensedRoute.AddRange(localUnlicensedRoute);

            for (int i = 0; i < localUnlicensedRoute.Count(); i++)
            {
                if (localLicensedRoute.Exists(x => x.RouteCd == localUnlicensedRoute[i].RouteCd))
                {
                    tempLocalUnlicensedRoute.Remove(localUnlicensedRoute[i]);
                }
            }

            _childLocalRouteDetailsDTO.Clear();

            localUnlicensedRoute.Clear();

            localUnlicensedRoute.AddRange(tempLocalUnlicensedRoute);

            foreach (var route in localLicensedRoute)
            {
                _childLocalRouteDetailsDTO.Add(route);
            }

            foreach (var uroute in localUnlicensedRoute)
            {
                _childLocalRouteDetailsDTO.Add(uroute);
            }

            _formularyDTO.FormularyLocalRouteDetails = _childLocalRouteDetailsDTO.ToList();
        }

        private void AssignFormularyStatusFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Formulary_Status_Agg"] ?? "any";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.RnohFormularyStatuscd = _childFormulariesDetailDTO?.All(rec => rec.RnohFormularyStatuscd == TerminologyConstants.FORMULARYSTATUS_FORMULARY) == true ? TerminologyConstants.FORMULARYSTATUS_FORMULARY : TerminologyConstants.FORMULARYSTATUS_NONFORMULARY;
            }
            else
            {
                detailDTO.RnohFormularyStatuscd = _childFormulariesDetailDTO?.Any(rec => rec.RnohFormularyStatuscd == TerminologyConstants.FORMULARYSTATUS_FORMULARY) == true ? TerminologyConstants.FORMULARYSTATUS_FORMULARY : TerminologyConstants.FORMULARYSTATUS_NONFORMULARY;
            }
        }

        private void AssignIsGastroResistantFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Is_Gastro_Resistant_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IsGastroResistant = _childGastroResistantFlagsDTO?.All(rec => rec == true) == true;
            }
            else
            {
                detailDTO.IsGastroResistant = _childGastroResistantFlagsDTO?.Any(rec => rec == true) == true;
            }
        }

        private void AssignIsIndicationMandatoryFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Is_Indication_Mandatory_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IsIndicationMandatory = _childIndicationMandatoryFlagsDTO?.All(rec => rec == true) == true;
            }
            else
            {
                detailDTO.IsIndicationMandatory = _childIndicationMandatoryFlagsDTO?.Any(rec => rec == true) == true;
            }
        }

        private void AssignIsModifiedReleaseFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Is_Modified_Release_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IsModifiedRelease = _childMdifiedReleaseFlagsDTO?.All(rec => rec == true) == true;
            }
            else
            {
                detailDTO.IsModifiedRelease = _childMdifiedReleaseFlagsDTO?.Any(rec => rec == true) == true;
            }
        }

        private void AssignIsDiluentFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Is_Diluent_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IsDiluent = _childDiluentFlagsDTO?.All(rec => rec == true) == true;
            }
            else
            {
                detailDTO.IsDiluent = _childDiluentFlagsDTO?.Any(rec => rec == true) == true;
            }
        }

        private void AssignIsBloodProductFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Is_Blood_Product_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IsBloodProduct = _childBloodProductFlagsDTO?.All(rec => rec == true) == true;
            }
            else
            {
                detailDTO.IsBloodProduct = _childBloodProductFlagsDTO?.Any(rec => rec == true) == true;
            }
        }

        private void AssignIgnoreDuplicateWarningsFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Ignore_Dup_warnings_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IgnoreDuplicateWarnings = _childIgnoreDuplicateWarningsFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.IgnoreDuplicateWarnings = _childIgnoreDuplicateWarningsFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignTitrationTypes(FormularyDetailDTO detailDTO)
        {
            detailDTO.TitrationTypes = new List<FormularyLookupItemDTO>();

            _childTitrationTypeDTO?.Distinct(rec => rec.Cd)?.Each(rec =>
            {
                detailDTO.TitrationTypes.Add(rec);
            });
        }

        private void AssignOutPatientMedicationFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Outpatient_Medn_Agg"] ?? "any";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.OutpatientMedicationCd = _childOutpatientMedicationFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.OutpatientMedicationCd = _childOutpatientMedicationFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignNotForPRNFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Not_For_PRN_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.NotForPrn = _childNotForPRNFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.NotForPrn = _childNotForPRNFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignHighAlertMednFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_High_Alert_Med_Agg"] ?? "any";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.HighAlertMedication = _childHighAlertMedicationFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.HighAlertMedication = _childHighAlertMedicationFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignExpensiveMednFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Expensive_Med_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.ExpensiveMedication = _childExpensiveMedicationFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.ExpensiveMedication = _childExpensiveMedicationFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignUnlicensedMednFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Unlicensed_Med_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.UnlicensedMedicationCd = _childUnlicensedMedicationFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.UnlicensedMedicationCd = _childUnlicensedMedicationFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignEMAAdditionalFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_EMA_Addnl_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.EmaAdditionalMonitoring = _childEMAAdditonalFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.EmaAdditionalMonitoring = _childEMAAdditonalFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignClinicalTrialFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Clinical_Trial_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.ClinicalTrialMedication = _childClinicalTrialFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.ClinicalTrialMedication = _childClinicalTrialFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignFreeFlags(FormularyDetailDTO detailDTO)
        {
            var defaultCfc = true; var defaultgluten = true; var defaultPrservative = true; var defaultSugar = true;

            _childFormulariesDetailDTO?.AsParallel().Each(rec =>
            {
                defaultCfc = (defaultCfc && rec.CfcFree == TerminologyConstants.STRINGIFIED_BOOL_TRUE);//If any one record set to false then false
                defaultgluten = (defaultgluten && rec.GlutenFree == TerminologyConstants.STRINGIFIED_BOOL_TRUE);
                defaultPrservative = (defaultPrservative && rec.PreservativeFree == TerminologyConstants.STRINGIFIED_BOOL_TRUE);//If any one record set to false then false
                defaultSugar = (defaultSugar && rec.SugarFree == TerminologyConstants.STRINGIFIED_BOOL_TRUE);//If any one record set to false then false
            });

            detailDTO.CfcFree = defaultCfc ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            detailDTO.GlutenFree = defaultgluten ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            detailDTO.PreservativeFree = defaultPrservative ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            detailDTO.SugarFree = defaultSugar ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
        }

        private void AssignIVTOOralFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_IV_TO_Oral_Agg"] ?? "any";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IvToOral = _childIVTOOralFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.IvToOral = _childIVTOOralFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }

        private void AssignCriticalDrug(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Critical_Drug_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                //If All child VMPs are set with critical then set to true
                detailDTO.CriticalDrug = _childCriticalDrugFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : null;
            }
            else
            {
                detailDTO.CriticalDrug = _childCriticalDrugFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : null;
            }
        }

        private void AssignBlackTriangle(FormularyDetailDTO detailDTO)
        {
            var btAggRule = _configuration["Formulary_Rules:VMP_Black_Triangle_Agg"] ?? "all";

            if (string.Compare(btAggRule, "all", true) == 0)
            {
                //If All child VMPs are set with blacktriangle then set to true
                detailDTO.BlackTriangle = _childBlackTrianglesDTO?.All(rec => rec == TerminologyConstants.STRINGIFIED_BOOL_TRUE) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : null;
            }
            else
            {
                detailDTO.BlackTriangle = _childBlackTrianglesDTO?.Any(rec => rec == TerminologyConstants.STRINGIFIED_BOOL_TRUE) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : null;
            }
        }

        private void AssignCustomWarnings()
        {
            var aggRule = _configuration["Formulary_Rules:VMP_Custom_Warning_Agg"] ?? "all";

            if (string.Compare(aggRule, "all", true) == 0)
            {
                //Check if all AMPs has Custom warnings
                if (!_childActiveAMPFormularies.IsCollectionValid() || !_childFormulariesDetailDTO.IsCollectionValid()) return;

                if (_childActiveAMPFormularies.IsCollectionValid() && _childFormulariesDetailDTO.IsCollectionValid() && _childActiveAMPFormularies.Count != _childFormulariesDetailDTO.Count) return;

                //Check if all details has custom warnings
                var allVMPsHasWarnings = _childFormulariesDetailDTO.All(rec => rec.CustomWarnings.IsCollectionValid());

                if (!allVMPsHasWarnings) return;

                var detailDTO = _formularyDTO.Detail;

                detailDTO.CustomWarnings = _childCustomWarningsDTO?.GroupBy(cw => new { cw.Warning, cw.NeedResponse, cw.Source }).SelectMany(cw => cw.Skip(_childFormulariesDetailDTO.Count - 1)).ToList();
            }
            else
            {
                _formularyDTO.Detail.CustomWarnings = _childCustomWarningsDTO?.GroupBy(cw => new { cw.Warning, cw.NeedResponse, cw.Source }).Select(cw => cw.FirstOrDefault()).ToList();
            }
        }

        private void AssignReminders()
        {
            var aggRule = _configuration["Formulary_Rules:VMP_Reminder_Agg"] ?? "all";

            if (string.Compare(aggRule, "all", true) == 0)
            {
                //Check if all AMPs has Custom warnings
                if (!_childActiveAMPFormularies.IsCollectionValid() || !_childFormulariesDetailDTO.IsCollectionValid()) return;

                if (_childActiveAMPFormularies.IsCollectionValid() && _childFormulariesDetailDTO.IsCollectionValid() && _childActiveAMPFormularies.Count != _childFormulariesDetailDTO.Count) return;

                //Check if all details has custom warnings
                var allVMPsHasReminders = _childFormulariesDetailDTO.All(rec => rec.Reminders.IsCollectionValid());

                if (!allVMPsHasReminders) return;

                var detailDTO = _formularyDTO.Detail;

                detailDTO.Reminders = _childRemindersDTO?.GroupBy(rem => new { rem.Reminder, rem.Duration, rem.Active, rem.Source }).SelectMany(rem => rem.Skip(_childFormulariesDetailDTO.Count - 1)).ToList();
            }
            else
            {
                _formularyDTO.Detail.Reminders = _childRemindersDTO?.GroupBy(rem => new { rem.Reminder, rem.Duration, rem.Active, rem.Source }).Select(rem => rem.FirstOrDefault()).ToList();
            }
        }

        private void AssignIsCustomControlledDrugFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Is_Custom_Controlled"] ?? "any";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IsCustomControlledDrug = _childCustomComtrolledDrugFlagsDTO?.All(rec => rec == true) == true;
            }
            else
            {
                detailDTO.IsCustomControlledDrug = _childCustomComtrolledDrugFlagsDTO?.Any(rec => rec == true) == true;
            }
        }

        private void AssignIsPrescriptionPrintingRequiredFlag(FormularyDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_Is_Prescription_Printing_Required"] ?? "any";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IsPrescriptionPrintingRequired = _childPrescriptionPrintingRequiredFlagsDTO?.All(rec => rec == true) == true;
            }
            else
            {
                detailDTO.IsPrescriptionPrintingRequired = _childPrescriptionPrintingRequiredFlagsDTO?.Any(rec => rec == true) == true;
            }
        }

        //private void ProjectClassificationCodesFromChildNodes(bool getAllAdditionalCodes)
        //{
        //    if (!_childAdditionalCodesDTO.IsCollectionValid()) return;

        //    //Project the classification codes from AMP to VMP
        //    //Only if each classification type has only one record
        //    var codeSystemLkp = new ConcurrentDictionary<string, SortedDictionary<string, FormularyAdditionalCodeDTO>>();

        //    var classificationTypeCodes = _childAdditionalCodesDTO
        //           .Where(rec => string.Compare(rec.CodeType, TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE, true) == 0)?
        //           .Distinct()?.ToList();


        //    //Check whether each type has only one record
        //    if (classificationTypeCodes.IsCollectionValid())
        //    {
        //        classificationTypeCodes.Each(rec =>
        //        {
        //            if (codeSystemLkp.ContainsKey(rec.AdditionalCodeSystem))
        //            {
        //                if (!codeSystemLkp[rec.AdditionalCodeSystem].ContainsKey(rec.AdditionalCode))
        //                {
        //                    codeSystemLkp[rec.AdditionalCodeSystem].Add(rec.AdditionalCode, rec);
        //                }
        //            }
        //            else
        //            {
        //                codeSystemLkp[rec.AdditionalCodeSystem] = new SortedDictionary<string, FormularyAdditionalCodeDTO> { { rec.AdditionalCode, rec } };
        //            }
        //        });
        //    }

        //    FillAdditonalCodes(codeSystemLkp, getAllAdditionalCodes);
        //}

        //private void FillAdditonalCodes(ConcurrentDictionary<string, SortedDictionary<string, FormularyAdditionalCodeDTO>> codeSystemLkp, bool getAllAdditionalCodes)
        //{
        //    if (!codeSystemLkp.IsCollectionValid()) return;

        //    var defaultClassifictionCodes = GetDefaultClassificationCodesFromDB();

        //    foreach (var csItem in codeSystemLkp)
        //    {
        //        //Consider that code system only if it doesn not has same code for that classification code system
        //        if (csItem.Value != null && csItem.Value.Count > 0)
        //        {
        //            var key = $"default_{csItem.Key.ToLowerInvariant()}_classification_code";

        //            var codeData = csItem.Value.First().Value;

        //            if (defaultClassifictionCodes.ContainsKey(key))
        //            {
        //                var defAddlCode = defaultClassifictionCodes[key];
        //                //consider the primary if else take the very first one
        //                codeData = csItem.Value.ContainsKey(defAddlCode) ? csItem.Value[defAddlCode] : codeData;
        //            }

        //            if (codeData != null)
        //            {
        //                codeData.IsDefault = true;

        //                if (!getAllAdditionalCodes)
        //                {
        //                    var cloned = _mapper.Map<FormularyAdditionalCodeDTO>(codeData);

        //                    if (string.Compare(csItem.Key, "bnf", true) == 0)
        //                    {
        //                        cloned.AdditionalCode = cloned.AdditionalCode.Substring(0, 9);
        //                    }

        //                    _formularyDTO.FormularyAdditionalCodes.Add(cloned);
        //                }
        //            }
        //            if (getAllAdditionalCodes)
        //                _formularyDTO.FormularyAdditionalCodes.AddRange(csItem.Value.Values);
        //        }
        //    }
        //}

        //private void AssignRecordStatus()
        //{
        //    var childAMPNodes = new HashSet<string>();

        //    if (base.DescendentFormularies.IsCollectionValid())
        //    {
        //        childAMPNodes = base.DescendentFormularies.Where(rec => string.Compare(rec.ProductType, "AMP", true) == 0)?.Select(rec => rec.RecStatusCode)?.Distinct().ToHashSet();
        //    }
        //    else if(_childFormularies.IsCollectionValid())
        //    {
        //        childAMPNodes = _childFormularies.Where(rec => string.Compare(rec.ProductType, "AMP", true) == 0)?.Select(rec => rec.RecStatusCode)?.Distinct().ToHashSet();
        //    }


        //    if (childAMPNodes.Contains(TerminologyConstants.RECORDSTATUS_ACTIVE))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_ACTIVE;
        //    }
        //    else if (childAMPNodes.Contains(TerminologyConstants.RECORDSTATUS_APPROVED) && (!(childAMPNodes.Contains(TerminologyConstants.RECORDSTATUS_ACTIVE))
        //        || !(childAMPNodes.Contains(TerminologyConstants.RECORDSTATUS_ARCHIVED)) || !(childAMPNodes.Contains(TerminologyConstants.RECORDSTATUS_DELETED))))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_APPROVED;
        //    }
        //    else if (childAMPNodes.All(rec => rec == TerminologyConstants.RECORDSTATUS_DRAFT))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT;
        //    }
        //    else if (childAMPNodes.All(rec => rec == TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
        //    }
        //    else if (childAMPNodes.All(rec => rec == TerminologyConstants.RECORDSTATUS_DELETED))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DELETED;
        //    }
        //    else if (childAMPNodes.Contains(TerminologyConstants.RECORDSTATUS_DRAFT))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT;
        //    }
        //    else if (childAMPNodes.Contains(TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
        //    }
        //    else if (childAMPNodes.Contains(TerminologyConstants.RECORDSTATUS_DELETED))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DELETED;
        //    }
        //}
    }
}
