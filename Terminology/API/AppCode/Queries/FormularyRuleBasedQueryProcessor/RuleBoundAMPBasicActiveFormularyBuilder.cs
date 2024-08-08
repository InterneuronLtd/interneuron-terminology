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
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Queries
{
    public class RuleBoundAMPBasicActiveFormularyBuilder : RuleBoundBaseFormularyBuilder
    {
        private FormularyHeader _parentFormulary;
        private FormularyDTO _parentFormularyDTO;

        public RuleBoundAMPBasicActiveFormularyBuilder(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }


        public override async Task CreateAdditionalCodes(bool getAllAddnlCodes = false)
        {
            //Need to bring from the siblings if not present for this AMP
            //if (!_formularyDAO.FormularyAdditionalCode.IsCollectionValid()) return;

            _activeFormularyBasicDTO.FormularyAdditionalCodes = new List<FormularyAdditionalCodeDTO>();

            var additionalCodes = new List<FormularyAdditionalCodeDTO>();
            
            if(_formularyDAO.FormularyAdditionalCode.IsCollectionValid())
                additionalCodes = this._mapper.Map<List<FormularyAdditionalCodeDTO>>(_formularyDAO.FormularyAdditionalCode);

            //Check if this AMP code has any Classification Code system missing
            //If missing, take it from the siblings (by aggregating codes of its sibling)
            var missingAddnlCodes = await GetMissingClassificationCodeSystems(additionalCodes);

            if (missingAddnlCodes.IsCollectionValid())
                additionalCodes.AddRange(missingAddnlCodes);

            //The below fn will set the default classification code - but these are not additionalcodes of child nodes but on
            ProjectClassificationCodesFromChildNodes(additionalCodes, getAllAddnlCodes, (codeRec) =>
            {
                if (string.Compare(codeRec.AdditionalCodeSystem, "bnf", true) == 0 && codeRec.AdditionalCode.IsNotEmpty())
                {
                    codeRec.AdditionalCode = codeRec.AdditionalCode.Length > 7 ? codeRec.AdditionalCode.Substring(0, 7): codeRec.AdditionalCode;
                }
            }, _activeFormularyBasicDTO);

            //MMC-451: No need to overwrite since the user has option to select the default now
            //OverrideClassificationCodesByConfigValuesForAMP();

            //can have multiple for the same classification type
            //_activeFormularyBasicDTO.FormularyAdditionalCodes.Each(rec => rec.IsDefault = true);


            //Add Additional Identity Codes
            if (!_formularyDAO.FormularyAdditionalCode.IsCollectionValid()) return;

            var addlIdentityCodes = _formularyDAO.FormularyAdditionalCode.Where(rec => string.Compare(rec.CodeType, TerminologyConstants.CODE_SYSTEM_IDENTIFICATION_TYPE, true) == 0)?.ToList();

            if (!addlIdentityCodes.IsCollectionValid()) return;
            var identityCodes = this._mapper.Map<List<FormularyAdditionalCodeDTO>>(addlIdentityCodes);
            _activeFormularyBasicDTO.FormularyAdditionalCodes.AddRange(identityCodes);

        }

        protected override void OverrideClassificationCodesByConfigValuesForAMP()
        {
            var classCodes = _configuration.GetSection("Formulary_Rules:OverridableClassificationCodes").Get<List<FormularyAdditionalCodeDTO>>();

            if (!classCodes.IsCollectionValid()) return;

            var classCodesForCurrentCode = classCodes.Where(rec => rec.DmdCode == _activeFormularyBasicDTO.ParentCode);//check for parent code

            if (!classCodesForCurrentCode.IsCollectionValid()) return;

            foreach (var item in _activeFormularyBasicDTO.FormularyAdditionalCodes)
            {
                //get from config if exists and overwrite it from config
                var classCodeForSystem = classCodesForCurrentCode.Where(rec => rec.AdditionalCodeSystem == item.AdditionalCodeSystem).FirstOrDefault();

                if (classCodeForSystem != null)
                {
                    item.AdditionalCodeDesc = classCodeForSystem.AdditionalCodeDesc;
                    item.AdditionalCode = classCodeForSystem.AdditionalCode;
                }
            }
        }
    }
}
