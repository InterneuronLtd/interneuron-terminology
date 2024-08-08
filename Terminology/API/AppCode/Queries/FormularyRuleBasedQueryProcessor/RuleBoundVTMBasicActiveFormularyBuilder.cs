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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Queries
{
    public class RuleBoundVTMBasicActiveFormularyBuilder : RuleBoundBaseFormularyBuilder
    {

        private List<FormularyHeader> _childActiveAMPFormularies = new List<FormularyHeader>();
        private List<FormularyBasicSearchResultModel> _childFormularies = new List<FormularyBasicSearchResultModel>();
        private ConcurrentBag<ActiveFormularyBasicDTO> _childFormulariesDTO = new ConcurrentBag<ActiveFormularyBasicDTO>();
        private ConcurrentBag<FormularyCustomWarningDTO> _childCustomWarningsDTO = new ConcurrentBag<FormularyCustomWarningDTO>();
        private ConcurrentBag<FormularyReminderDTO> _childRemindersDTO = new ConcurrentBag<FormularyReminderDTO>();
        private ConcurrentBag<string> _childEndorsementsDTO = new ConcurrentBag<string>();

        private ConcurrentBag<FormularyAdditionalCodeDTO> _childAdditionalCodesDTO = new ConcurrentBag<FormularyAdditionalCodeDTO>();

        private ConcurrentBag<ActiveFormularyBasicDetailDTO> _childFormulariesDetailDTO = new ConcurrentBag<ActiveFormularyBasicDetailDTO>();

        private ConcurrentBag<FormularyLookupItemDTO> _childLicensedIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<FormularyLookupItemDTO> _childUnlicensedIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<FormularyLookupItemDTO> _childLocalLicensedIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();
        private ConcurrentBag<FormularyLookupItemDTO> _childLocalUnlicensedIndicationsDTO = new ConcurrentBag<FormularyLookupItemDTO>();

        
        private ConcurrentBag<string> _childMedusaPreparationInstructionsDTO = new ConcurrentBag<string>();

        private ConcurrentBag<FormularyLookupItemDTO> _childTitrationTypeDTO = new ConcurrentBag<FormularyLookupItemDTO>();

        private ConcurrentBag<bool> _childIgnoreDuplicateWarningsFlagsDTO = new ConcurrentBag<bool>();

        public RuleBoundVTMBasicActiveFormularyBuilder(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override async Task CreateBasicActiveFormularyBase(FormularyHeader formularyDAO)
        {
            await base.CreateBasicActiveFormularyBase(formularyDAO);

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
            var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

            var nodes = await repo.GetFormularyDescendentForCodes(new string[] { _formularyDAO.Code });

            if (!nodes.IsCollectionValid()) return;

            _childFormularies = nodes.ToList();

            var childIds = nodes.Where(rec => string.Compare(rec.ProductType, "AMP", true) == 0 && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)?.Select(rec => rec.FormularyVersionId)?.ToList();

            if (!childIds.IsCollectionValid()) return;

            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            _childActiveAMPFormularies = formularyRepo.GetFormularyListForIds(childIds)?.ToList();
            */
        }

        private async Task FillAMPProperties()
        {
            await FetchChildAMPFormularies();

            if (!_childActiveAMPFormularies.IsCollectionValid()) return;

            _childActiveAMPFormularies.AsParallel().Each(child =>
            {
                var header = _mapper.Map<ActiveFormularyBasicDTO>(child);

                var detail = child.FormularyDetail.FirstOrDefault();

                if (detail != null)
                {
                    header.Detail = _mapper.Map<ActiveFormularyBasicDetailDTO>(detail);

                    _childFormulariesDetailDTO.Add(header.Detail);

                    header.Detail.CustomWarnings?.Each(rec => { _childCustomWarningsDTO.Add(rec); });
                    header.Detail.Reminders?.Each(rec => { _childRemindersDTO.Add(rec); });
                    header.Detail.Endorsements?.Each(rec => { _childEndorsementsDTO.Add(rec); });

                    header.Detail.LicensedUses?.Each(rec => _childLicensedIndicationsDTO.Add(rec));
                    header.Detail.UnLicensedUses?.Each(rec => _childUnlicensedIndicationsDTO.Add(rec));

                    header.Detail.LocalLicensedUses?.Each(rec => _childLocalLicensedIndicationsDTO.Add(rec));
                    header.Detail.LocalUnLicensedUses?.Each(rec => _childLocalUnlicensedIndicationsDTO.Add(rec));

                    
                    header.Detail.MedusaPreparationInstructions?.Each(rec => _childMedusaPreparationInstructionsDTO.Add(rec));
                    
                    header.Detail.TitrationTypes?.Each(rec => _childTitrationTypeDTO.Add(rec));

                    _childIgnoreDuplicateWarningsFlagsDTO.Add(header.Detail.IgnoreDuplicateWarnings == TerminologyConstants.STRINGIFIED_BOOL_TRUE);
                }

                
                if (child.FormularyAdditionalCode.IsCollectionValid())
                {
                    header.FormularyAdditionalCodes = _mapper.Map<List<FormularyAdditionalCodeDTO>>(child.FormularyAdditionalCode);
                    header.FormularyAdditionalCodes.Each(rec => _childAdditionalCodesDTO.Add(rec));
                }

                _childFormulariesDTO.Add(header);
            });
        }


        public override async Task CreateAdditionalCodes(bool getAllAddnlCodes = false)
        {
            _activeFormularyBasicDTO.FormularyAdditionalCodes = new List<FormularyAdditionalCodeDTO>();

            if (_childActiveAMPFormularies.IsCollectionValid())
            {
                ProjectClassificationCodesFromChildNodes(_childAdditionalCodesDTO, getAllAddnlCodes, (codeRec) => {
                    if (string.Compare(codeRec.AdditionalCodeSystem, "bnf", true) == 0 && codeRec.AdditionalCode.IsNotEmpty())
                    {
                        codeRec.AdditionalCode = codeRec.AdditionalCode.Length > 7 ? codeRec.AdditionalCode.Substring(0, 7) : codeRec.AdditionalCode;
                    }
                }, _activeFormularyBasicDTO);
            }

            //Add Additional Identity Codes
            if (!_formularyDAO.FormularyAdditionalCode.IsCollectionValid()) return;

            var addlIdentityCodes = _formularyDAO.FormularyAdditionalCode.Where(rec => string.Compare(rec.CodeType, TerminologyConstants.CODE_SYSTEM_IDENTIFICATION_TYPE, true) == 0)?.ToList();

            if (!addlIdentityCodes.IsCollectionValid()) return;
            _activeFormularyBasicDTO.FormularyAdditionalCodes.AddRange(this._mapper.Map<List<FormularyAdditionalCodeDTO>>(addlIdentityCodes));
        }


        public override void CreateBasicActiveFormularyDetails()
        {
            base.CreateBasicActiveFormularyDetails();

            var anyChildFormularyDetailDTO = _childFormulariesDetailDTO.FirstOrDefault() ?? new ActiveFormularyBasicDetailDTO();

            if (!_childActiveAMPFormularies.IsCollectionValid()) return;

            //Overwrite VMP properties from AMP
            var detailDTO = _activeFormularyBasicDTO.Detail;
            detailDTO.LicensedUses = _childLicensedIndicationsDTO?.Distinct(rec => rec.Cd).ToList();
            detailDTO.UnLicensedUses = _childUnlicensedIndicationsDTO?.Distinct(rec => rec.Cd).ToList();

            detailDTO.LocalLicensedUses = _childLocalLicensedIndicationsDTO?.Distinct(rec => rec.Cd).ToList();
            detailDTO.LocalUnLicensedUses = _childLocalUnlicensedIndicationsDTO?.Distinct(rec => rec.Cd).ToList();

            List<FormularyLookupItemDTO> tempLocalUnlicensedUses = new List<FormularyLookupItemDTO>();

            tempLocalUnlicensedUses.AddRange(detailDTO.LocalUnLicensedUses);

            for (var localIndex = 0; localIndex < detailDTO.LocalUnLicensedUses.Count(); localIndex++)
            {
                if (detailDTO.LocalLicensedUses.Exists(x => x.Cd == detailDTO.LocalUnLicensedUses[localIndex].Cd))
                {
                    tempLocalUnlicensedUses.Remove(detailDTO.LocalUnLicensedUses[localIndex]);
                }
            }

            detailDTO.LocalUnLicensedUses.Clear();

            detailDTO.LocalUnLicensedUses.AddRange(tempLocalUnlicensedUses);



            AssignCustomWarnings();

            AssignReminders();

            detailDTO.Endorsements = _childEndorsementsDTO?.Distinct().ToList();

            detailDTO.MedusaPreparationInstructions = _childMedusaPreparationInstructionsDTO?.Distinct().ToList();


            AssignTitrationTypes(detailDTO);

            AssignFormularyStatusFlag(detailDTO);

            AssignIgnoreDuplicateWarningsFlag(detailDTO);
        }

        private void AssignIgnoreDuplicateWarningsFlag(ActiveFormularyBasicDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VTM_Ignore_Dup_warnings_Agg"] ?? "all";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.IgnoreDuplicateWarnings = _childIgnoreDuplicateWarningsFlagsDTO?.All(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else
            {
                detailDTO.IgnoreDuplicateWarnings = _childIgnoreDuplicateWarningsFlagsDTO?.Any(rec => rec) == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
        }
        private void AssignFormularyStatusFlag(ActiveFormularyBasicDetailDTO detailDTO)
        {
            var cdAggRule = _configuration["Formulary_Rules:VTM_Formulary_Status_Agg"] ?? "any";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                detailDTO.RnohFormularyStatuscd = _childFormulariesDetailDTO?.All(rec => rec.RnohFormularyStatuscd == TerminologyConstants.FORMULARYSTATUS_FORMULARY) == true ? TerminologyConstants.FORMULARYSTATUS_FORMULARY : TerminologyConstants.FORMULARYSTATUS_NONFORMULARY;
            }
            else
            {
                detailDTO.RnohFormularyStatuscd = _childFormulariesDetailDTO?.Any(rec => rec.RnohFormularyStatuscd == TerminologyConstants.FORMULARYSTATUS_FORMULARY) == true ? TerminologyConstants.FORMULARYSTATUS_FORMULARY : TerminologyConstants.FORMULARYSTATUS_NONFORMULARY;
            }
        }
      


        private void AssignTitrationTypes(ActiveFormularyBasicDetailDTO detailDTO)
        {
            detailDTO.TitrationTypes = new List<FormularyLookupItemDTO>();

            _childTitrationTypeDTO?.Distinct(rec => rec.Cd)?.Each(rec =>
            {
                detailDTO.TitrationTypes.Add(rec);
            });
        }


        private bool GetIVTOOralFlagForVMPs(List<bool> flagVals)
        {
            var cdAggRule = _configuration["Formulary_Rules:VMP_IV_TO_Oral_Agg"] ?? "any";

            if (string.Compare(cdAggRule, "all", true) == 0)
            {
                return flagVals?.All(rec => rec) == true;
            }
            else
            {
                return flagVals?.Any(rec => rec) == true;
            }
        }


        private void AssignCustomWarnings()
        {
            var aggRule = _configuration["Formulary_Rules:VTM_Custom_Warning_Agg"] ?? "all";

            if (string.Compare(aggRule, "all", true) == 0)
            {
                //Check if all AMPs has Custom warnings
                if (!_childActiveAMPFormularies.IsCollectionValid() || !_childFormulariesDetailDTO.IsCollectionValid()) return;

                if (_childActiveAMPFormularies.IsCollectionValid() && _childFormulariesDetailDTO.IsCollectionValid() && _childActiveAMPFormularies.Count != _childFormulariesDetailDTO.Count) return;

                //Check if all details has custom warnings
                var allVMPsHasWarnings = _childFormulariesDetailDTO.All(rec => rec.CustomWarnings.IsCollectionValid());

                if (!allVMPsHasWarnings) return;

                var detailDTO = _activeFormularyBasicDTO.Detail;

                detailDTO.CustomWarnings = _childCustomWarningsDTO?.GroupBy(cw => new { cw.Warning, cw.NeedResponse, cw.Source }).SelectMany(cw => cw.Skip(_childFormulariesDetailDTO.Count - 1)).ToList();
            }
            else
            {
                _activeFormularyBasicDTO.Detail.CustomWarnings = _childCustomWarningsDTO?.GroupBy(cw => new { cw.Warning, cw.NeedResponse, cw.Source }).Select(cw => cw.FirstOrDefault()).ToList();
            }
        }

        private void AssignReminders()
        {
            var aggRule = _configuration["Formulary_Rules:VTM_Reminder_Agg"] ?? "all";

            if (string.Compare(aggRule, "all", true) == 0)
            {
                //Check if all AMPs has Reminders
                if (!_childActiveAMPFormularies.IsCollectionValid() || !_childFormulariesDetailDTO.IsCollectionValid()) return;

                if (_childActiveAMPFormularies.IsCollectionValid() && _childFormulariesDetailDTO.IsCollectionValid() && _childActiveAMPFormularies.Count != _childFormulariesDetailDTO.Count) return;

                //Check if all details has reminders
                var allVMPsHasReminders = _childFormulariesDetailDTO.All(rec => rec.Reminders.IsCollectionValid());

                if (!allVMPsHasReminders) return;

                var detailDTO = _activeFormularyBasicDTO.Detail;

                detailDTO.Reminders = _childRemindersDTO?.GroupBy(rem => new { rem.Reminder, rem.Duration, rem.Active, rem.Source }).SelectMany(rem => rem.Skip(_childFormulariesDetailDTO.Count - 1)).ToList();
            }
            else
            {
                _activeFormularyBasicDTO.Detail.Reminders = _childRemindersDTO?.GroupBy(rem => new { rem.Reminder, rem.Duration, rem.Active, rem.Source }).Select(rem => rem.FirstOrDefault()).ToList();
            }
        }


       

        //private void AssignRecordStatus()
        //{
        //    var childVMPNodes = new HashSet<string>();

        //    if (base.DescendentFormularies.IsCollectionValid())
        //    {
        //        childVMPNodes = base.DescendentFormularies.Where(rec => string.Compare(rec.ProductType, "VMP", true) == 0)?.Select(rec => rec.RecStatusCode)?.Distinct().ToHashSet();
        //    }
        //    else if (_childFormularies.IsCollectionValid())
        //    {
        //        childVMPNodes = _childFormularies.Where(rec => string.Compare(rec.ProductType, "VMP", true) == 0)?.Select(rec => rec.RecStatusCode)?.Distinct().ToHashSet();
        //    }

        //    if (childVMPNodes.Contains(TerminologyConstants.RECORDSTATUS_ACTIVE))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_ACTIVE;
        //    }
        //    else if (childVMPNodes.Contains(TerminologyConstants.RECORDSTATUS_APPROVED) && (!(childVMPNodes.Contains(TerminologyConstants.RECORDSTATUS_ACTIVE))
        //        || !(childVMPNodes.Contains(TerminologyConstants.RECORDSTATUS_ARCHIVED)) || !(childVMPNodes.Contains(TerminologyConstants.RECORDSTATUS_DELETED))))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_APPROVED;
        //    }
        //    else if (childVMPNodes.All(rec => rec == TerminologyConstants.RECORDSTATUS_DRAFT))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT;
        //    }
        //    else if (childVMPNodes.All(rec => rec == TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
        //    }
        //    else if (childVMPNodes.All(rec => rec == TerminologyConstants.RECORDSTATUS_DELETED))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DELETED;
        //    }
        //    else if (childVMPNodes.Contains(TerminologyConstants.RECORDSTATUS_DRAFT))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT;
        //    }
        //    else if (childVMPNodes.Contains(TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
        //    }
        //    else if (childVMPNodes.Contains(TerminologyConstants.RECORDSTATUS_DELETED))
        //    {
        //        _formularyDTO.RecStatusCode = TerminologyConstants.RECORDSTATUS_DELETED;
        //    }
        //}
    }


}
