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
﻿using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Queries
{
    public interface IFormularyQueries
    {
        Task<List<T>> GetLookup<T>(LookupType lookupType) where T : ILookupItemDTO;

        Task<Dictionary<T1, T2>> GetLookup<T, T1, T2>(LookupType lookupType, Func<T, T1> keySelector, Func<T, T2> valueSelector) where T : ILookupItemDTO;

        Task<FormularySearchResultsWithHierarchyDTO> GetFormularyHierarchyForSearchRequest(FormularySearchFilterRequest filterCriteria);

        Task<List<FormularySearchResultDTO>> GetFormularyAsFlatList(FormularySearchFilterRequest filterCriteriaRequest);
        Task<FormularyDTO> GetFormularyDetail(string id);

        List<FormularyDTO> GetLatestFormulariesBriefInfo();

        //Task<FormularySearchResultsWithHierarchyDTO> GetDMDAndFormularyHierarchy(FormularySearchFilterRequest filterCriteria);

        Task<FormularyDTO> GetFormularyDetailRuleBound(string id, bool getAllAdditionalCodes = false);

        Task<FormularyDTO> GetActiveFormularyDetailRuleBoundByCode(string code, bool fromCache = true, bool includeInvalid = false);

        //Not being used - Uses different logic to get this data
        //Task<FormularyDTO[]> GetActiveFormularyDetailRuleBoundByCodeArray(string[] code);

        List<FormularyDTO> GetLatestFormulariesBriefInfoByNameOrCode(string nameOrCode, string productType = null, bool isExactMatch = false);

        DeriveProductNamesDTO DeriveProductNames(List<FormularyIngredientDTO> ingredients, string unitDoseFormSize, string formulationName = null, string supplierName = null, string productType = "amp");

        CheckIfProductExistsDTO CheckIfProductExists(List<FormularyIngredientDTO> ingredients, string unitDoseFormSize, string formulationName = null, string supplierName = null, string productType = "amp", bool isExactMatch = false);

        Task<List<FormularySearchResultDTO>> GetLatestTopLevelFormulariesBasicInfo();

        Task<List<FormularySearchResultDTO>> GetFormularyDescendentForCodes(List<string> codes, bool onlyNonDeleted = true);

        Task<List<FormularySearchResultDTO>> GetFormularyImmediateDescendentForCodes(List<string> codes, bool onlyNonDeleted = true);

        //Task<List<FormularySearchResultDTO>> GetFormularyImmediateDescendentForFormularyIds(List<string> formularyIds, bool onlyNonDeleted = true);
        Task<List<FormularySearchResultDTO>> GetFormularyImmediateDescendentForFormularyVersionIds(List<string> formularyVersionIds, bool onlyNonDeleted = true);

        Task<List<FormularySearchResultDTO>> GetFormulariesAsDiluents();

        Task<List<FormularyLocalRouteDetailDTO>> GetFormulariesRoutes();

        Task<FormularyHistoryDTO> GetHistoryOfFormularies(HistoryOfFormulariesRequest request);

        Task<List<FormularyLocalLicensedUseDTO>> GetLocalLicensedUse(List<string> formularyVersionIds);

        Task<List<FormularyLocalUnlicensedUseDTO>> GetLocalUnlicensedUse(List<string> formularyVersionIds);

        Task<List<FormularyLocalLicensedRouteDTO>> GetLocalLicensedRoute(List<string> formularyVersionIds);

        Task<List<FormularyLocalUnlicensedRouteDTO>> GetLocalUnlicensedRoute(List<string> formularyVersionIds);

        Task<List<CustomWarningDTO>> GetCustomWarning(List<string> formularyVersionIds);

        Task<List<ReminderDTO>> GetReminder(List<string> formularyVersionIds);

        Task<List<EndorsementDTO>> GetEndorsement(List<string> formularyVersionIds);

        Task<List<MedusaPreparationInstructionDTO>> GetMedusaPreparationInstruction(List<string> formularyVersionIds);

        Task<List<TitrationTypeDTO>> GetTitrationType(List<string> formularyVersionIds);

        Task<List<RoundingFactorDTO>> GetRoundingFactor(List<string> formularyVersionIds);

        Task<List<CompatibleDiluentDTO>> GetCompatibleDiluent(List<string> formularyVersionIds);

        Task<List<ClinicalTrialMedicationDTO>> GetClinicalTrialMedication(List<string> formularyVersionIds);

        Task<List<GastroResistantDTO>> GetGastroResistant(List<string> formularyVersionIds);

        Task<List<CriticalDrugDTO>> GetCriticalDrug(List<string> formularyVersionIds);

        Task<List<ModifiedReleaseDTO>> GetModifiedRelease(List<string> formularyVersionIds);

        Task<List<ExpensiveMedicationDTO>> GetExpensiveMedication(List<string> formularyVersionIds);

        Task<List<HighAlertMedicationDTO>> GetHighAlertMedication(List<string> formularyVersionIds);

        Task<List<IVToOralDTO>> GetIVToOral(List<string> formularyVersionIds);

        Task<List<NotForPRNDTO>> GetNotForPRN(List<string> formularyVersionIds);

        Task<List<BloodProductDTO>> GetBloodProduct(List<string> formularyVersionIds);

        Task<List<DiluentDTO>> GetDiluent(List<string> formularyVersionIds);

        Task<List<PrescribableDTO>> GetPrescribable(List<string> formularyVersionIds);

        Task<List<OutpatientMedicationDTO>> GetOutpatientMedication(List<string> formularyVersionIds);

        Task<List<IgnoreDuplicateWarningDTO>> GetIgnoreDuplicateWarning(List<string> formularyVersionIds);

        Task<List<ControlledDrugDTO>> GetControlledDrug(List<string> formularyVersionIds);

        Task<List<PrescriptionPrintingRequiredDTO>> GetPrescriptionPrintingRequired(List<string> formularyVersionIds);

        Task<List<IndicationMandatoryDTO>> GetIndicationMandatory(List<string> formularyVersionIds);

        Task<List<WitnessingRequiredDTO>> GetWitnessingRequired(List<string> formularyVersionIds);

        Task<List<FormularyStatusDTO>> GetFormularyStatus(List<string> formularyVersionIds);

        //Not being used - Uses different logic to get this data
        //Task<List<FormularyDetailResultDTO>> GetFormularyDetailByCodes(string[] codes);

        //Task CacheActiveFormularyDetailRuleBound(string code);

        Task<List<FormularyDTO>> GetActiveFormularyDetailRuleBoundByCodes(List<string> codes, bool fromCache = true, bool includeInvalid = false);

        List<string> GetActiveFormularyCodes(List<string> codes = null);

        Task<List<ActiveFormularyBasicDTO>> GetActiveFormularyBasicDetailRuleBoundByCodes(List<string> list, bool fromCache, bool includeInvalid);

        Task<List<FormularyUsageStatDTO>> GetFormulariesUsageStatByPrefix(string prefixText, Dictionary<string, object> filterMeta);
        Task<List<FormularyUsageStatDTO>> GetFormulariesUsageStatByCodes(List<string> codes, Dictionary<string, object> filterMeta);
        Task<List<FormularyChangeLogDTO>> GetFormularyChangeLogForCodes(List<string> codes);

        Task<List<FormularyDTO>> GetFormularyDetailRuleBoundForFVIds(List<string> fvIds, bool getAllAdditionalCodes = false);

        Task<ValidateAMPStatusChangeDTO> ValidateAMPStatusChange(ValidateFormularyStatusChangeRequest request);
        Dictionary<string, long> GetFormularyIdOrderInfoLookup(List<string> formularyIds);
        Task<List<FormularyDTO>> GetFormularyHeaderOnlyForFVIds(List<string> fvIds);

        bool HasAnyUpdateInProgress();

        List<FormularyLocalRouteDetailDTO> GetLocalRoutes(GetRoutesRequest request);
        List<FormularyRouteDetailDTO> GetRoutes(GetRoutesRequest request);
    }
}
