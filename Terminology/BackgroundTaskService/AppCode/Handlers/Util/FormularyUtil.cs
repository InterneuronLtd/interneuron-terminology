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
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers.Util
{
    public class FormularyUtil
    {
        private readonly IMapper _mapper;

        public FormularyUtil(IMapper mapper)
        {
            _mapper = mapper;
        }
        public FormularyHeader CloneFormulary(FormularyHeader existingFormulary)
        {
            var newFormulary = _mapper.Map<FormularyHeader>(existingFormulary);
            newFormulary.RowId = null;
            var newFormularyVersionId = Guid.NewGuid().ToString();

            if (existingFormulary.FormularyAdditionalCode.IsCollectionValid())
            {
                newFormulary.FormularyAdditionalCode = _mapper.Map<List<FormularyAdditionalCode>>(existingFormulary.FormularyAdditionalCode);
                newFormulary.FormularyAdditionalCode?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyDetail.IsCollectionValid())
            {
                newFormulary.FormularyDetail = _mapper.Map<List<FormularyDetail>>(existingFormulary.FormularyDetail);
                newFormulary.FormularyDetail?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyRouteDetail.IsCollectionValid())
            {
                newFormulary.FormularyRouteDetail = _mapper.Map<List<FormularyRouteDetail>>(existingFormulary.FormularyRouteDetail);
                newFormulary.FormularyRouteDetail?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyLocalRouteDetail.IsCollectionValid())
            {
                newFormulary.FormularyLocalRouteDetail = _mapper.Map<List<FormularyLocalRouteDetail>>(existingFormulary.FormularyLocalRouteDetail);
                newFormulary.FormularyLocalRouteDetail?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyIngredient.IsCollectionValid())
            {
                newFormulary.FormularyIngredient = _mapper.Map<List<FormularyIngredient>>(existingFormulary.FormularyIngredient);
                newFormulary.FormularyIngredient?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyExcipient.IsCollectionValid())
            {
                newFormulary.FormularyExcipient = _mapper.Map<List<FormularyExcipient>>(existingFormulary.FormularyExcipient);
                newFormulary.FormularyExcipient?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }

            newFormulary.FormularyId = existingFormulary.FormularyId;
            newFormulary.VersionId = existingFormulary.VersionId + 1;
            newFormulary.FormularyVersionId = newFormularyVersionId;
            newFormulary.IsLatest = true;
            newFormulary.IsDuplicate = existingFormulary.IsDuplicate;// false;//Need to check
            newFormulary.DuplicateOfFormularyId = existingFormulary.DuplicateOfFormularyId;
            newFormulary.IsLockedForSave = false;

            return newFormulary;
        }
    }


}
