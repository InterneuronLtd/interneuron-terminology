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
﻿using System;
using System.Collections.Generic;

namespace Interneuron.Terminology.Model.DomainModels
{
    public partial class FormularyDetail : Interneuron.Terminology.Infrastructure.Domain.EntityBase, Interneuron.Terminology.Infrastructure.Domain.IAuditable
    {
        public string RowId { get; set; }
        public DateTime? Createdtimestamp { get; set; }
        public DateTime? Createddate { get; set; }
        public string Createdby { get; set; }
        public string Timezonename { get; set; }
        public int? Timezoneoffset { get; set; }
        public string Tenant { get; set; }
        public string FormularyVersionId { get; set; }
        public string MedicationTypeCode { get; set; }
        public string RnohFormularyStatuscd { get; set; }
        public string InpatientMedicationCd { get; set; }
        public string OutpatientMedicationCd { get; set; }
        public string PrescribingStatusCd { get; set; }
        public string RulesCd { get; set; }
        public string UnlicensedMedicationCd { get; set; }
        public string DefinedDailyDose { get; set; }
        public string NotForPrn { get; set; }
        public string HighAlertMedication { get; set; }
        public string IgnoreDuplicateWarnings { get; set; }
        public string MedusaPreparationInstructions { get; set; }
        public string CriticalDrug { get; set; }
        public string ControlledDrugCategoryCd { get; set; }
        public string Cytotoxic { get; set; }
        public string ClinicalTrialMedication { get; set; }
        public string Fluid { get; set; }
        public string Antibiotic { get; set; }
        public string Anticoagulant { get; set; }
        public string Antipsychotic { get; set; }
        public string Antimicrobial { get; set; }
        public bool? AddReviewReminder { get; set; }
        public string IvToOral { get; set; }
        public string TitrationTypeCd { get; set; }
        public string RoundingFactorCd { get; set; }
        public decimal? MaxDoseNumerator { get; set; }
        public string MaximumDoseUnitCd { get; set; }
        public string WitnessingRequired { get; set; }
        public string NiceTa { get; set; }
        public string MarkedModifierCd { get; set; }
        public string Insulins { get; set; }
        public string MentalHealthDrug { get; set; }
        public string BasisOfPreferredNameCd { get; set; }
        public string SugarFree { get; set; }
        public string GlutenFree { get; set; }
        public string PreservativeFree { get; set; }
        public string CfcFree { get; set; }
        public string DoseFormCd { get; set; }
        public decimal? UnitDoseFormSize { get; set; }
        public string UnitDoseFormUnits { get; set; }
        public string UnitDoseUnitOfMeasureCd { get; set; }
        public string FormCd { get; set; }
        public string TradeFamilyCd { get; set; }
        public string ModifiedReleaseCd { get; set; }
        public string BlackTriangle { get; set; }
        public string SupplierCd { get; set; }
        public string CurrentLicensingAuthorityCd { get; set; }
        public string EmaAdditionalMonitoring { get; set; }
        public string ParallelImport { get; set; }
        public string RestrictionsOnAvailabilityCd { get; set; }
        public DateTime? Updatedtimestamp { get; set; }
        public DateTime? Updateddate { get; set; }
        public string Updatedby { get; set; }
        public string DrugClass { get; set; }
        public string RestrictionNote { get; set; }
        public string RestrictedPrescribing { get; set; }
        public string SideEffect { get; set; }
        public string Caution { get; set; }
        public string ContraIndication { get; set; }
        public string SafetyMessage { get; set; }
        public string CustomWarning { get; set; }
        public string Endorsement { get; set; }
        public string LicensedUse { get; set; }
        public string UnlicensedUse { get; set; }
        public string OrderableFormtypeCd { get; set; }
        public string TradeFamilyName { get; set; }
        public string ExpensiveMedication { get; set; }
        public bool? IsBloodProduct { get; set; }
        public bool? IsDiluent { get; set; }
        public bool? IsModifiedRelease { get; set; }
        public bool? IsGastroResistant { get; set; }
        public bool? Prescribable { get; set; }
        public string ControlledDrugCategorySource { get; set; }
        public string BlackTriangleSource { get; set; }
        public string HighAlertMedicationSource { get; set; }
        public string PrescribableSource { get; set; }
        public bool? IsCustomControlledDrug { get; set; }
        public string Diluent { get; set; }
        public string SupplierName { get; set; }
        public bool? IsIndicationMandatory { get; set; }
        public string Reminder { get; set; }
        public string LocalLicensedUse { get; set; }
        public string LocalUnlicensedUse { get; set; }
        public bool? IsPrescriptionPrintingRequired { get; set; }
        public string RestrictionsOnAvailabilityDesc { get; set; }
        public string BasisOfPreferredNameDesc { get; set; }
        public string ControlledDrugCategoryDesc { get; set; }
        public string DoseFormDesc { get; set; }
        public string FormDesc { get; set; }
        public string CurrentLicensingAuthorityDesc { get; set; }
        public string PrescribingStatusDesc { get; set; }
        public string UnitDoseUnitOfMeasureDesc { get; set; }
        public string UnitDoseFormUnitsDesc { get; set; }

        public virtual FormularyHeader FormularyVersion { get; set; }
    }
}
