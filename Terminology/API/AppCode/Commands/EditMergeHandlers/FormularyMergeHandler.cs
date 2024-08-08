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
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.Extensions;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Interneuron.Terminology.API.AppCode.Commands.EditMergeHandlers
{
    public abstract class FormularyMergeHandler
    {
        private IMapper _mapper;

        public FormularyMergeHandler(IMapper mapper)
        {
            _mapper = mapper;
        }
        public abstract FormularyHeader Merge(FormularyHeader existingFormulary, FormularyDTO dto);

        protected virtual FormularyHeader CloneFormulary(FormularyHeader existingFormulary)
        {
            return existingFormulary.CloneFormularyV2(_mapper);
        }

        protected List<FormularyLookupItemDTO> HandleFormularyLookupItems(List<FormularyLookupItemDTO> sourceLookupItems, List<FormularyLookupItemDTO> destinationLookupItems)
        {
            destinationLookupItems = destinationLookupItems.IsCollectionValid() ? destinationLookupItems : new List<FormularyLookupItemDTO>();
            var customManualSourceItems = destinationLookupItems.Where(rec => rec.Source == TerminologyConstants.MANUAL_DATA_SRC || rec.Source.IsEmpty()).ToList();

            //Delete indications from custom source - manually entered
            if (customManualSourceItems.IsCollectionValid())
                customManualSourceItems.Each(rec => destinationLookupItems.Remove(rec));

            if (sourceLookupItems.IsCollectionValid())
            {
                var destinationLookupItemsCodes = destinationLookupItems.Select(rec => rec.Cd).ToList();

                //Get custom added indications
                var manualSourceLookupItems = sourceLookupItems.Where(rec => !destinationLookupItemsCodes.Contains(rec.Cd) && (rec.Source == TerminologyConstants.MANUAL_DATA_SRC || rec.Source.IsEmpty())).ToList();

                manualSourceLookupItems?.Each(rec =>
                {
                    rec.Source = TerminologyConstants.MANUAL_DATA_SRC;
                    destinationLookupItems.Add(rec);
                });
            }

            return destinationLookupItems;
        }

        public string GetDefaultAdditionalCodesAsMeta(List<FormularyAdditionalCodeDTO> addnlCodes)
        {
            if (!addnlCodes.IsCollectionValid()) return string.Empty;

            var classObj = new JObject();

            foreach (var addnlCode in addnlCodes)
            {
                if (addnlCode.IsDefault)
                {
                    var key = TerminologyConstants.DEF_TEMPLATE_CLASS_CODES.Replace("{template}", addnlCode.AdditionalCodeSystem.ToLowerInvariant());
                    classObj[key] = addnlCode.AdditionalCode;
                }
            }
            var rootObj = new JObject();
            rootObj[TerminologyConstants.DEF_CLASS_CODES] = classObj;
            
            return rootObj.ToString();
        }
    }

    
}