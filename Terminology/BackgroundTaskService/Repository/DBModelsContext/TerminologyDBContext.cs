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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;

namespace Interneuron.Terminology.BackgroundTaskService.Repository.DBModelsContext
{
    public partial class TerminologyDBContext : DbContext
    {
        public virtual DbSet<FormularyAdditionalCode> FormularyAdditionalCode { get; set; } = null!;
        public virtual DbSet<FormularyChangeLog> FormularyChangeLog { get; set; } = null!;
        public virtual DbSet<FormularyDetail> FormularyDetail { get; set; } = null!;
        public virtual DbSet<FormularyExcipient> FormularyExcipient { get; set; } = null!;
        public virtual DbSet<FormularyHeader> FormularyHeader { get; set; } = null!;
        public virtual DbSet<FormularyIndication> FormularyIndication { get; set; } = null!;
        public virtual DbSet<FormularyIngredient> FormularyIngredient { get; set; } = null!;
        public virtual DbSet<FormularyLocalRouteDetail> FormularyLocalRouteDetail { get; set; } = null!;
        public virtual DbSet<FormularyOntologyForm> FormularyOntologyForm { get; set; } = null!;
        public virtual DbSet<FormularyRouteDetail> FormularyRouteDetail { get; set; } = null!;
        public virtual DbSet<FormularyRuleConfig> FormularyRuleConfig { get; set; } = null!;
        public virtual DbSet<FormularyUsageStats> FormularyUsageStats { get; set; } = null!;
        public virtual DbSet<LookupCommon> LookupCommon { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("local_formulary", "pg_trgm")
                .HasPostgresExtension("uuid-ossp");

            modelBuilder.Entity<FormularyAdditionalCode>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_additional_code_pk");

                entity.ToTable("formulary_additional_code", "local_formulary");

                entity.HasIndex(e => e.FormularyVersionId, "formulary_additional_code_formulary_version_id_idx");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.AdditionalCode)
                    .HasMaxLength(500)
                    .HasColumnName("additional_code");

                entity.Property(e => e.AdditionalCodeDesc).HasColumnName("additional_code_desc");

                entity.Property(e => e.AdditionalCodeSystem)
                    .HasMaxLength(500)
                    .HasColumnName("additional_code_system");

                entity.Property(e => e.Attr1).HasColumnName("attr1");

                entity.Property(e => e.CodeType)
                    .HasMaxLength(1000)
                    .HasColumnName("code_type")
                    .HasDefaultValueSql("'Classification'::character varying");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.MetaJson).HasColumnName("meta_json");

                entity.Property(e => e.Source)
                    .HasMaxLength(500)
                    .HasColumnName("source")
                    .HasDefaultValueSql("'M'::character varying");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.HasOne(d => d.FormularyVersion)
                    .WithMany(p => p.FormularyAdditionalCode)
                    .HasForeignKey(d => d.FormularyVersionId)
                    .HasConstraintName("formulary_additional_code_fk");
            });

            modelBuilder.Entity<FormularyChangeLog>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_change_log_pk");

                entity.ToTable("formulary_change_log", "local_formulary");

                entity.HasIndex(e => e.Code, "formulary_change_log_code_idx");

                entity.HasIndex(e => e.HasProductDeletedChanged, "formulary_change_log_has_product_deleted_changed_idx");

                entity.HasIndex(e => e.HasProductDetailChanged, "formulary_change_log_has_product_detail_changed_idx");

                entity.HasIndex(e => e.HasProductFlagsChanged, "formulary_change_log_has_product_flags_idx");

                entity.HasIndex(e => e.HasProductGuidanceChanged, "formulary_change_log_has_product_guidance_idx");

                entity.HasIndex(e => e.HasProductInvalidFlagChanged, "formulary_change_log_has_product_invalid_flag_changed_idx");

                entity.HasIndex(e => e.HasProductPosologyChanged, "formulary_change_log_has_product_posology_idx");

                entity.HasIndex(e => e.Name, "formulary_change_log_name_idx");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Code)
                    .HasMaxLength(255)
                    .HasColumnName("code");

                entity.Property(e => e.FormularyId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_id");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.EntitiesCompared).HasColumnName("entities_compared");

                entity.Property(e => e.HasProductDeletedChanged).HasColumnName("has_product_deleted_changed");

                entity.Property(e => e.HasProductDetailChanged).HasColumnName("has_product_detail_changed");

                entity.Property(e => e.HasProductFlagsChanged).HasColumnName("has_product_flags_changed");

                entity.Property(e => e.HasProductGuidanceChanged).HasColumnName("has_product_guidance_changed");

                entity.Property(e => e.HasProductInvalidFlagChanged).HasColumnName("has_product_invalid_flag_changed");

                entity.Property(e => e.HasProductPosologyChanged).HasColumnName("has_product_posology_changed");

                entity.Property(e => e.Name).HasColumnName("name");

                entity.Property(e => e.ParentCode)
                    .HasMaxLength(255)
                    .HasColumnName("parent_code");

                entity.Property(e => e.ProductDeletedChanges).HasColumnName("product_deleted_changes");

                entity.Property(e => e.ProductDetailChanges).HasColumnName("product_detail_changes");

                entity.Property(e => e.ProductFlagsChanges).HasColumnName("product_flags_changes");

                entity.Property(e => e.ProductGuidanceChanges).HasColumnName("product_guidance_changes");

                entity.Property(e => e.ProductInvalidChanges).HasColumnName("product_invalid_changes");

                entity.Property(e => e.ProductPosologyChanges).HasColumnName("product_posology_changes");

                entity.Property(e => e.DeltaDetail).HasColumnName("delta_detail");

                entity.Property(e => e.ProductType)
                    .HasMaxLength(100)
                    .HasColumnName("product_type");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");
            });

            modelBuilder.Entity<FormularyDetail>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_detail_pk");

                entity.ToTable("formulary_detail", "local_formulary");

                entity.HasIndex(e => e.FormularyVersionId, "formulary_detail_formulary_version_id_idx");

                entity.HasIndex(e => e.IsDiluent, "formulary_detail_is_diluent_idx");

                entity.HasIndex(e => e.RnohFormularyStatuscd, "formulary_detail_rnoh_formulary_statuscd_idx");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.AddReviewReminder).HasColumnName("add_review_reminder");

                entity.Property(e => e.Antibiotic)
                    .HasMaxLength(10)
                    .HasColumnName("antibiotic");

                entity.Property(e => e.Anticoagulant)
                    .HasMaxLength(10)
                    .HasColumnName("anticoagulant");

                entity.Property(e => e.Antimicrobial)
                    .HasMaxLength(10)
                    .HasColumnName("antimicrobial");

                entity.Property(e => e.Antipsychotic)
                    .HasMaxLength(10)
                    .HasColumnName("antipsychotic");

                entity.Property(e => e.BasisOfPreferredNameCd)
                    .HasMaxLength(50)
                    .HasColumnName("basis_of_preferred_name_cd");

                entity.Property(e => e.BasisOfPreferredNameDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("basis_of_preferred_name_desc");

                entity.Property(e => e.BlackTriangle)
                    .HasMaxLength(10)
                    .HasColumnName("black_triangle");

                entity.Property(e => e.BlackTriangleSource)
                    .HasMaxLength(100)
                    .HasColumnName("black_triangle_source");

                entity.Property(e => e.Caution).HasColumnName("caution");

                entity.Property(e => e.CfcFree)
                    .HasMaxLength(10)
                    .HasColumnName("cfc_free");

                entity.Property(e => e.ClinicalTrialMedication)
                    .HasMaxLength(10)
                    .HasColumnName("clinical_trial_medication");

                entity.Property(e => e.ContraIndication).HasColumnName("contra_indication");

                entity.Property(e => e.ControlledDrugCategoryCd)
                    .HasMaxLength(50)
                    .HasColumnName("controlled_drug_category_cd");

                entity.Property(e => e.ControlledDrugCategoryDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("controlled_drug_category_desc");

                entity.Property(e => e.ControlledDrugCategorySource)
                    .HasMaxLength(100)
                    .HasColumnName("controlled_drug_category_source");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.CriticalDrug)
                    .HasMaxLength(10)
                    .HasColumnName("critical_drug");

                entity.Property(e => e.CurrentLicensingAuthorityCd)
                    .HasMaxLength(50)
                    .HasColumnName("current_licensing_authority_cd");

                entity.Property(e => e.CurrentLicensingAuthorityDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("current_licensing_authority_desc");

                entity.Property(e => e.CustomWarning).HasColumnName("custom_warning");

                entity.Property(e => e.Cytotoxic)
                    .HasMaxLength(10)
                    .HasColumnName("cytotoxic");

                entity.Property(e => e.DefinedDailyDose)
                    .HasMaxLength(255)
                    .HasColumnName("defined_daily_dose");

                entity.Property(e => e.Diluent).HasColumnName("diluent");

                entity.Property(e => e.DoseFormCd)
                    .HasMaxLength(50)
                    .HasColumnName("dose_form_cd");

                entity.Property(e => e.DoseFormDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("dose_form_desc");

                entity.Property(e => e.DrugClass)
                    .HasMaxLength(255)
                    .HasColumnName("drug_class");

                entity.Property(e => e.EmaAdditionalMonitoring)
                    .HasMaxLength(10)
                    .HasColumnName("ema_additional_monitoring");

                entity.Property(e => e.Endorsement).HasColumnName("endorsement");

                entity.Property(e => e.ExpensiveMedication)
                    .HasMaxLength(50)
                    .HasColumnName("expensive_medication");

                entity.Property(e => e.Fluid)
                    .HasMaxLength(10)
                    .HasColumnName("fluid");

                entity.Property(e => e.FormCd)
                    .HasMaxLength(255)
                    .HasColumnName("form_cd");

                entity.Property(e => e.FormDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("form_desc");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.GlutenFree)
                    .HasMaxLength(10)
                    .HasColumnName("gluten_free");

                entity.Property(e => e.HighAlertMedication)
                    .HasMaxLength(10)
                    .HasColumnName("high_alert_medication");

                entity.Property(e => e.HighAlertMedicationSource)
                    .HasMaxLength(100)
                    .HasColumnName("high_alert_medication_source");

                entity.Property(e => e.IgnoreDuplicateWarnings)
                    .HasMaxLength(10)
                    .HasColumnName("ignore_duplicate_warnings");

                entity.Property(e => e.InpatientMedicationCd)
                    .HasMaxLength(50)
                    .HasColumnName("inpatient_medication_cd");

                entity.Property(e => e.Insulins)
                    .HasMaxLength(10)
                    .HasColumnName("insulins");

                entity.Property(e => e.IsBloodProduct).HasColumnName("is_blood_product");

                entity.Property(e => e.IsCustomControlledDrug).HasColumnName("is_custom_controlled_drug");

                entity.Property(e => e.IsDiluent).HasColumnName("is_diluent");

                entity.Property(e => e.IsGastroResistant).HasColumnName("is_gastro_resistant");

                entity.Property(e => e.IsIndicationMandatory).HasColumnName("is_indication_mandatory");

                entity.Property(e => e.IsModifiedRelease).HasColumnName("is_modified_release");

                entity.Property(e => e.IsPrescriptionPrintingRequired).HasColumnName("is_prescription_printing_required");

                entity.Property(e => e.IvToOral)
                    .HasMaxLength(10)
                    .HasColumnName("iv_to_oral");

                entity.Property(e => e.LicensedUse).HasColumnName("licensed_use");

                entity.Property(e => e.LocalLicensedUse).HasColumnName("local_licensed_use");

                entity.Property(e => e.LocalUnlicensedUse).HasColumnName("local_unlicensed_use");

                entity.Property(e => e.MarkedModifierCd)
                    .HasMaxLength(50)
                    .HasColumnName("marked_modifier_cd");

                entity.Property(e => e.MaxDoseNumerator)
                    .HasPrecision(100, 4)
                    .HasColumnName("max_dose_numerator");

                entity.Property(e => e.MaximumDoseUnitCd)
                    .HasMaxLength(50)
                    .HasColumnName("maximum_dose_unit_cd");

                entity.Property(e => e.MedicationTypeCode)
                    .HasMaxLength(50)
                    .HasColumnName("medication_type_code");

                entity.Property(e => e.MedusaPreparationInstructions).HasColumnName("medusa_preparation_instructions");

                entity.Property(e => e.MentalHealthDrug)
                    .HasMaxLength(10)
                    .HasColumnName("mental_health_drug");

                entity.Property(e => e.ModifiedReleaseCd)
                    .HasMaxLength(50)
                    .HasColumnName("modified_release_cd");

                entity.Property(e => e.NiceTa)
                    .HasMaxLength(255)
                    .HasColumnName("nice_ta");

                entity.Property(e => e.NotForPrn)
                    .HasMaxLength(10)
                    .HasColumnName("not_for_prn");

                entity.Property(e => e.OrderableFormtypeCd)
                    .HasMaxLength(50)
                    .HasColumnName("orderable_formtype_cd");

                entity.Property(e => e.OutpatientMedicationCd)
                    .HasMaxLength(50)
                    .HasColumnName("outpatient_medication_cd");

                entity.Property(e => e.ParallelImport)
                    .HasMaxLength(10)
                    .HasColumnName("parallel_import");

                entity.Property(e => e.Prescribable)
                    .HasColumnName("prescribable")
                    .HasDefaultValueSql("true");

                entity.Property(e => e.PrescribableSource)
                    .HasMaxLength(100)
                    .HasColumnName("prescribable_source");

                entity.Property(e => e.PrescribingStatusCd)
                    .HasMaxLength(50)
                    .HasColumnName("prescribing_status_cd");

                entity.Property(e => e.PrescribingStatusDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("prescribing_status_desc");

                entity.Property(e => e.PreservativeFree)
                    .HasMaxLength(10)
                    .HasColumnName("preservative_free");

                entity.Property(e => e.Reminder).HasColumnName("reminder");

                entity.Property(e => e.RestrictedPrescribing)
                    .HasMaxLength(10)
                    .HasColumnName("restricted_prescribing");

                entity.Property(e => e.RestrictionNote).HasColumnName("restriction_note");

                entity.Property(e => e.RestrictionsOnAvailabilityCd)
                    .HasMaxLength(50)
                    .HasColumnName("restrictions_on_availability_cd");

                entity.Property(e => e.RestrictionsOnAvailabilityDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("restrictions_on_availability_desc");

                entity.Property(e => e.RnohFormularyStatuscd)
                    .HasMaxLength(50)
                    .HasColumnName("rnoh_formulary_statuscd");

                entity.Property(e => e.RoundingFactorCd)
                    .HasMaxLength(50)
                    .HasColumnName("rounding_factor_cd");

                entity.Property(e => e.RulesCd)
                    .HasMaxLength(50)
                    .HasColumnName("rules_cd");

                entity.Property(e => e.SafetyMessage).HasColumnName("safety_message");

                entity.Property(e => e.SideEffect).HasColumnName("side_effect");

                entity.Property(e => e.SugarFree)
                    .HasMaxLength(10)
                    .HasColumnName("sugar_free");

                entity.Property(e => e.SupplierCd)
                    .HasMaxLength(50)
                    .HasColumnName("supplier_cd");

                entity.Property(e => e.SupplierName)
                    .HasMaxLength(1000)
                    .HasColumnName("supplier_name");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.TitrationTypeCd)
                    .HasMaxLength(50)
                    .HasColumnName("titration_type_cd");

                entity.Property(e => e.TradeFamilyCd)
                    .HasMaxLength(18)
                    .HasColumnName("trade_family_cd");

                entity.Property(e => e.TradeFamilyName)
                    .HasMaxLength(500)
                    .HasColumnName("trade_family_name");

                entity.Property(e => e.UnitDoseFormSize)
                    .HasPrecision(20, 4)
                    .HasColumnName("unit_dose_form_size");

                entity.Property(e => e.UnitDoseFormUnits)
                    .HasMaxLength(18)
                    .HasColumnName("unit_dose_form_units");

                entity.Property(e => e.UnitDoseFormUnitsDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("unit_dose_form_units_desc");

                entity.Property(e => e.UnitDoseUnitOfMeasureCd)
                    .HasMaxLength(50)
                    .HasColumnName("unit_dose_unit_of_measure_cd");

                entity.Property(e => e.UnitDoseUnitOfMeasureDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("unit_dose_unit_of_measure_desc");

                entity.Property(e => e.UnlicensedMedicationCd)
                    .HasMaxLength(50)
                    .HasColumnName("unlicensed_medication_cd");

                entity.Property(e => e.UnlicensedUse).HasColumnName("unlicensed_use");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.WitnessingRequired)
                    .HasMaxLength(10)
                    .HasColumnName("witnessing_required");

                entity.HasOne(d => d.FormularyVersion)
                    .WithMany(p => p.FormularyDetail)
                    .HasForeignKey(d => d.FormularyVersionId)
                    .HasConstraintName("formulary_detail_fk");
            });

            modelBuilder.Entity<FormularyExcipient>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_excipient_pk");

                entity.ToTable("formulary_excipient", "local_formulary");

                entity.HasIndex(e => e.FormularyVersionId, "formulary_excipient_formulary_version_id_idx");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.IngredientCd)
                    .HasMaxLength(1000)
                    .HasColumnName("ingredient_cd");

                entity.Property(e => e.IngredientName)
                    .HasMaxLength(1000)
                    .HasColumnName("ingredient_name");

                entity.Property(e => e.Strength)
                    .HasMaxLength(20)
                    .HasColumnName("strength");

                entity.Property(e => e.StrengthUnitCd)
                    .HasMaxLength(18)
                    .HasColumnName("strength_unit_cd");

                entity.Property(e => e.StrengthUnitDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("strength_unit_desc");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.HasOne(d => d.FormularyVersion)
                    .WithMany(p => p.FormularyExcipient)
                    .HasForeignKey(d => d.FormularyVersionId)
                    .HasConstraintName("formulary_excipient_fk");
            });

            modelBuilder.Entity<FormularyHeader>(entity =>
            {
                entity.HasKey(e => e.FormularyVersionId)
                    .HasName("formulary_header_pk");

                entity.ToTable("formulary_header", "local_formulary");

                entity.HasIndex(e => e.Code, "formulary_header_code_idx");

                entity.HasIndex(e => e.FormularyVersionId, "formulary_header_formulary_version_id_idx");

                entity.HasIndex(e => e.IsLatest, "formulary_header_is_latest_idx");

                entity.HasIndex(e => e.Name, "formulary_header_name_idx");

                entity.HasIndex(e => e.NameTokens, "formulary_header_name_tokens_idx")
                    .HasMethod("gin");

                entity.HasIndex(e => e.ParentCode, "formulary_header_parent_code_idx");

                entity.HasIndex(e => e.ProductType, "formulary_header_product_type_idx");

                entity.HasIndex(e => e.RecStatusCode, "formulary_header_rec_status_code_idx");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.Code)
                    .HasMaxLength(255)
                    .HasColumnName("code");

                entity.Property(e => e.CodeSystem)
                    .HasMaxLength(500)
                    .HasColumnName("code_system")
                    .HasDefaultValueSql("'DMD'::character varying");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.DuplicateOfFormularyId)
                    .HasMaxLength(255)
                    .HasColumnName("duplicate_of_formulary_id");

                entity.Property(e => e.FormularyId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_id");

                entity.Property(e => e.IsDmdDeleted).HasColumnName("is_dmd_deleted");

                entity.Property(e => e.IsDmdInvalid).HasColumnName("is_dmd_invalid");

                entity.Property(e => e.IsDuplicate).HasColumnName("is_duplicate");

                entity.Property(e => e.IsLatest).HasColumnName("is_latest");

                entity.Property(e => e.IsLockedForSave).HasColumnName("is_locked_for_save");

                entity.Property(e => e.MetaInfoJson).HasColumnName("meta_info_json");

                entity.Property(e => e.Name).HasColumnName("name");

                entity.Property(e => e.NameTokens).HasColumnName("name_tokens");

                entity.Property(e => e.ParentCode)
                    .HasMaxLength(255)
                    .HasColumnName("parent_code");

                entity.Property(e => e.ParentFormularyId)
                    .HasMaxLength(255)
                    .HasColumnName("parent_formulary_id");

                entity.Property(e => e.ParentName).HasColumnName("parent_name");

                entity.Property(e => e.ParentNameTokens).HasColumnName("parent_name_tokens");

                entity.Property(e => e.ParentProductType)
                    .HasMaxLength(100)
                    .HasColumnName("parent_product_type");

                entity.Property(e => e.Prevcode)
                    .HasMaxLength(255)
                    .HasColumnName("prevcode");

                entity.Property(e => e.ProductType)
                    .HasMaxLength(100)
                    .HasColumnName("product_type");

                entity.Property(e => e.RecSource)
                    .HasMaxLength(50)
                    .HasColumnName("rec_source");

                entity.Property(e => e.RecStatusCode)
                    .HasMaxLength(8)
                    .HasColumnName("rec_status_code");

                entity.Property(e => e.RecStatuschangeDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("rec_statuschange_date");

                entity.Property(e => e.RecStatuschangeMsg).HasColumnName("rec_statuschange_msg");

                entity.Property(e => e.RecStatuschangeTs).HasColumnName("rec_statuschange_ts");

                entity.Property(e => e.RecStatuschangeTzname)
                    .HasMaxLength(255)
                    .HasColumnName("rec_statuschange_tzname");

                entity.Property(e => e.RecStatuschangeTzoffset).HasColumnName("rec_statuschange_tzoffset");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.VersionId).HasColumnName("version_id");

                entity.Property(e => e.VmpId)
                    .HasMaxLength(100)
                    .HasColumnName("vmp_id");

                entity.Property(e => e.VtmId)
                    .HasMaxLength(100)
                    .HasColumnName("vtm_id");
            });

            modelBuilder.Entity<FormularyIndication>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_indication_pk");

                entity.ToTable("formulary_indication", "local_formulary");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.IndicationCd)
                    .HasMaxLength(50)
                    .HasColumnName("indication_cd");

                entity.Property(e => e.IndicationName)
                    .HasMaxLength(500)
                    .HasColumnName("indication_name");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.HasOne(d => d.FormularyVersion)
                    .WithMany(p => p.FormularyIndication)
                    .HasForeignKey(d => d.FormularyVersionId)
                    .HasConstraintName("formulary_indication_fk");
            });

            modelBuilder.Entity<FormularyIngredient>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_ingredient_pk");

                entity.ToTable("formulary_ingredient", "local_formulary");

                entity.HasIndex(e => e.FormularyVersionId, "formulary_ingredient_formulary_version_id_idx");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.BasisOfPharmaceuticalStrengthCd)
                    .HasMaxLength(50)
                    .HasColumnName("basis_of_pharmaceutical_strength_cd");

                entity.Property(e => e.BasisOfPharmaceuticalStrengthDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("basis_of_pharmaceutical_strength_desc");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.IngredientCd)
                    .HasMaxLength(1000)
                    .HasColumnName("ingredient_cd");

                entity.Property(e => e.IngredientName)
                    .HasMaxLength(1000)
                    .HasColumnName("ingredient_name");

                entity.Property(e => e.StrengthValueDenominator)
                    .HasMaxLength(20)
                    .HasColumnName("strength_value_denominator");

                entity.Property(e => e.StrengthValueDenominatorUnitCd)
                    .HasMaxLength(20)
                    .HasColumnName("strength_value_denominator_unit_cd");

                entity.Property(e => e.StrengthValueDenominatorUnitDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("strength_value_denominator_unit_desc");

                entity.Property(e => e.StrengthValueNumerator)
                    .HasMaxLength(20)
                    .HasColumnName("strength_value_numerator");

                entity.Property(e => e.StrengthValueNumeratorUnitCd)
                    .HasMaxLength(18)
                    .HasColumnName("strength_value_numerator_unit_cd");

                entity.Property(e => e.StrengthValueNumeratorUnitDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("strength_value_numerator_unit_desc");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.HasOne(d => d.FormularyVersion)
                    .WithMany(p => p.FormularyIngredient)
                    .HasForeignKey(d => d.FormularyVersionId)
                    .HasConstraintName("formulary_ingredient_fk");
            });

            modelBuilder.Entity<FormularyLocalRouteDetail>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_local_route_detail_pk");

                entity.ToTable("formulary_local_route_detail", "local_formulary");

                entity.HasIndex(e => e.FormularyVersionId, "formulary_local_route_detail_formulary_version_id_idx");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.RouteCd)
                    .HasMaxLength(50)
                    .HasColumnName("route_cd");

                entity.Property(e => e.RouteDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("route_desc");

                entity.Property(e => e.RouteFieldTypeCd)
                    .HasMaxLength(50)
                    .HasColumnName("route_field_type_cd");

                entity.Property(e => e.Source)
                    .HasMaxLength(100)
                    .HasColumnName("source");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.HasOne(d => d.FormularyVersion)
                    .WithMany(p => p.FormularyLocalRouteDetail)
                    .HasForeignKey(d => d.FormularyVersionId)
                    .HasConstraintName("formulary_local_route_detail_fk");
            });

            modelBuilder.Entity<FormularyOntologyForm>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_ontology_form_pk");

                entity.ToTable("formulary_ontology_form", "local_formulary");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.FormCd)
                    .HasMaxLength(50)
                    .HasColumnName("form_cd");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.HasOne(d => d.FormularyVersion)
                    .WithMany(p => p.FormularyOntologyForm)
                    .HasForeignKey(d => d.FormularyVersionId)
                    .HasConstraintName("formulary_ontology_form_fk");
            });

            modelBuilder.Entity<FormularyRouteDetail>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_route_detail_pk");

                entity.ToTable("formulary_route_detail", "local_formulary");

                entity.HasIndex(e => e.FormularyVersionId, "formulary_route_detail_formulary_version_id_idx");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.FormularyVersionId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_version_id");

                entity.Property(e => e.RouteCd)
                    .HasMaxLength(50)
                    .HasColumnName("route_cd");

                entity.Property(e => e.RouteDesc)
                    .HasMaxLength(1000)
                    .HasColumnName("route_desc");

                entity.Property(e => e.RouteFieldTypeCd)
                    .HasMaxLength(50)
                    .HasColumnName("route_field_type_cd");

                entity.Property(e => e.Source)
                    .HasMaxLength(100)
                    .HasColumnName("source");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.HasOne(d => d.FormularyVersion)
                    .WithMany(p => p.FormularyRouteDetail)
                    .HasForeignKey(d => d.FormularyVersionId)
                    .HasConstraintName("formulary_route_detail_fk");
            });

            modelBuilder.Entity<FormularyRuleConfig>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_rule_config_pkey");

                entity.ToTable("formulary_rule_config", "local_formulary");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.ConfigJson).HasColumnName("config_json");

                entity.Property(e => e.Contextkey)
                    .HasMaxLength(255)
                    .HasColumnName("_contextkey");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdsource)
                    .HasMaxLength(255)
                    .HasColumnName("_createdsource");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .HasColumnName("name");

                entity.Property(e => e.Recordstatus)
                    .HasColumnName("_recordstatus")
                    .HasDefaultValueSql("1");

                entity.Property(e => e.Sequenceid)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("_sequenceid");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");
            });

            modelBuilder.Entity<FormularyUsageStats>(entity =>
            {
                entity.HasKey(e => e.RowId)
                    .HasName("formulary_usage_stats_pk");

                entity.ToTable("formulary_usage_stats", "local_formulary");

                entity.HasIndex(e => e.Code, "formulary_usage_stats_code_idx");

                entity.HasIndex(e => e.Name, "formulary_usage_stats_name_gin_idx")
                    .HasMethod("gin")
                    .HasOperators(new[] { "gin_trgm_ops" });

                entity.HasIndex(e => e.NameTokens, "formulary_usage_stats_name_tokens_idx_tsv_idx")
                    .HasMethod("gin");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Code)
                    .HasMaxLength(255)
                    .HasColumnName("code");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.FormularyId)
                    .HasMaxLength(255)
                    .HasColumnName("formulary_id");

                entity.Property(e => e.FullName).HasColumnName("full_name");

                entity.Property(e => e.FullNameTokens).HasColumnName("full_name_tokens");

                entity.Property(e => e.Name).HasColumnName("name");

                entity.Property(e => e.NameTokens).HasColumnName("name_tokens");

                entity.Property(e => e.Source)
                    .HasMaxLength(255)
                    .HasColumnName("source");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.UsageCount)
                    .HasColumnName("usage_count")
                    .HasDefaultValueSql("1");
            });

            modelBuilder.Entity<LookupCommon>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("lookup_common", "local_formulary");

                entity.HasIndex(e => e.Cd, "lookup_common_cd_idx");

                entity.HasIndex(e => e.Desc, "lookup_common_desc_idx");

                entity.HasIndex(e => e.Type, "lookup_common_type_idx");

                entity.Property(e => e.Additionalproperties).HasColumnName("additionalproperties");

                entity.Property(e => e.Cd)
                    .HasMaxLength(50)
                    .HasColumnName("cd");

                entity.Property(e => e.Contextkey)
                    .HasMaxLength(255)
                    .HasColumnName("_contextkey");

                entity.Property(e => e.Createdby)
                    .HasMaxLength(255)
                    .HasColumnName("_createdby");

                entity.Property(e => e.Createddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_createddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Createdmessageid)
                    .HasMaxLength(255)
                    .HasColumnName("_createdmessageid");

                entity.Property(e => e.Createdsource)
                    .HasMaxLength(255)
                    .HasColumnName("_createdsource");

                entity.Property(e => e.Createdtimestamp)
                    .HasColumnName("_createdtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");

                entity.Property(e => e.Desc)
                    .HasMaxLength(1000)
                    .HasColumnName("desc");

                entity.Property(e => e.Isdefault).HasColumnName("isdefault");

                entity.Property(e => e.Recordstatus)
                    .HasColumnName("_recordstatus")
                    .HasDefaultValueSql("1");

                entity.Property(e => e.RowId)
                    .HasMaxLength(255)
                    .HasColumnName("_row_id")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Sequenceid)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("_sequenceid");

                entity.Property(e => e.Tenant)
                    .HasMaxLength(255)
                    .HasColumnName("_tenant");

                entity.Property(e => e.Timezonename)
                    .HasMaxLength(255)
                    .HasColumnName("_timezonename");

                entity.Property(e => e.Timezoneoffset).HasColumnName("_timezoneoffset");

                entity.Property(e => e.Type)
                    .HasMaxLength(100)
                    .HasColumnName("type");

                entity.Property(e => e.Updatedby)
                    .HasMaxLength(255)
                    .HasColumnName("_updatedby");

                entity.Property(e => e.Updateddate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("_updateddate")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Updatedtimestamp)
                    .HasColumnName("_updatedtimestamp")
                    .HasDefaultValueSql("timezone('UTC'::text, now())");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
