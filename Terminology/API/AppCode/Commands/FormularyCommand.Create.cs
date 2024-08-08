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
using Interneuron.Terminology.API.AppCode.Commands.FormularyCreateHandlers;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Commands
{
    public partial class FormularyCommand : IFormularyCommands
    {
        private Dictionary<string, string> _supplierCodeNames;
        private Dictionary<string, string> _formCodeNames;

        public async Task<CreateEditFormularyDTO> CreateFormulary(CreateEditFormularyRequest request, Action<List<string>> onCreateComplete = null)
        {
            var response = new CreateEditFormularyDTO
            {
                Status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS, StatusMessage = "", ErrorMessages = new List<string>() },
                Data = new List<FormularyDTO>()
            };

            await PrefillDMDLookup();

            var existingAMPs = CheckIfHasExistingAMPDrugs(request.RequestsData);

            if (existingAMPs.IsCollectionValid())
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                response.Status.ErrorMessages.Add($"Records with the same name already exists in the system. Existing Ids: {string.Join(",", existingAMPs)}");
                return response;
            }

            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;


            var formulariesToBeSaved = CreateFormularies(request, formularyRepo);

            if (formulariesToBeSaved.IsCollectionValid())
            {
                formulariesToBeSaved.Each(saveFormulary =>
                {
                    formularyRepo.Add(saveFormulary);
                });

                formularyRepo.SaveChanges();

                if(onCreateComplete != null)
                {
                    var codes = formulariesToBeSaved?.Select(rec => rec.Code)?.Distinct()?.ToList();

                    var completeHierarchy = await PostUpdate(codes);

                    if (completeHierarchy.IsCollectionValid())
                    {
                        onCreateComplete?.Invoke(completeHierarchy);
                    }
                }

                formulariesToBeSaved.Each(saveFormulary =>
                {
                    RePopulateDTOPostSave(saveFormulary, response);
                });
            }

            return response;
        }

        private List<FormularyHeader> CreateFormularies(CreateEditFormularyRequest request, IFormularyRepository<FormularyHeader> formularyRepo)
        {
            var formulariesToBeSaved = new List<FormularyHeader>();

            request.RequestsData.Each(rec =>
            {
                var vtm = GetOrCreateFormulary(rec, formularyRepo, formulariesToBeSaved, new VTMCreateHandler(_mapper, rec), "vtm", true);
                var vmp = GetOrCreateFormulary(rec, formularyRepo, formulariesToBeSaved, new VMPCreateHandler(_mapper, rec), "vmp", true, vtm.FormularyId);
                var amp = GetOrCreateFormulary(rec, formularyRepo, formulariesToBeSaved, new AMPCreateHandler(_mapper, rec), "amp", true, vmp.FormularyId);

                var prodNames = DeriveProductName(rec, "all");
                vtm.Name = prodNames.ProductNameByType["vtm"];

                vmp.ParentCode = vtm.Code;
                vmp.VtmId = vtm.FormularyVersionId;
                vmp.Name = prodNames.ProductNameByType["vmp"];
                vmp.ParentFormularyId = vtm.FormularyId;

                amp.ParentCode = vmp.Code;
                amp.VtmId = vtm.FormularyVersionId;
                amp.VmpId = vmp.FormularyVersionId;
                amp.Name = prodNames.ProductNameByType["amp"];
                amp.ParentFormularyId = vmp.FormularyId;

            });
            UpdateDMDLookupDescription(formulariesToBeSaved);

            return formulariesToBeSaved;
        }

        private void UpdateDMDLookupDescription(List<FormularyHeader> formulariesToBeSaved)
        {
            if (!formulariesToBeSaved.IsCollectionValid()) return;

            foreach (var rec in formulariesToBeSaved)
            {
                rec.FormularyDetail?.Each(formularyDetail=> UpdateFormularyDetailDMDLookup(formularyDetail));
                rec.FormularyIngredient?.Each(ing => UpdateFormularyIngredientsDMDLookup(ing));
                rec.FormularyRouteDetail?.Each(route => UpdateFormularyRoutesDMDLookup(route));
                rec.FormularyLocalRouteDetail?.Each(route => UpdateFormularyLocalRoutesDMDLookup(route));
                rec.FormularyExcipient?.Each(excipient => UpdateFormularyExcipientDMDLookup(excipient));
            }
        }

        private DeriveProductNamesDTO DeriveProductName(FormularyDTO formulary, string productType)
        {
            var ingredients = formulary.FormularyIngredients;
            var unitDoseFormSize = formulary.Detail.UnitDoseFormSize;
            var formulationName = formulary.Detail.FormCd.IsNotEmpty() ? _formCodeNames[formulary.Detail.FormCd] : null;
            var supplierName = formulary.Detail.SupplierCd.IsNotEmpty() ? (formulary.Detail.SupplierName.IsNotEmpty() ? formulary.Detail.SupplierName : (_supplierCodeNames.ContainsKey(formulary.Detail.SupplierCd) ? _supplierCodeNames[formulary.Detail.SupplierCd] : null)) : null;

            return _formularyQueries.DeriveProductNames(ingredients, unitDoseFormSize.ToString(), formulationName, supplierName, productType);
        }

        private FormularyHeader GetOrCreateFormulary(FormularyDTO rec, IFormularyRepository<FormularyHeader> formularyRepo, List<FormularyHeader> formulariesToBeSaved, BaseCreateHandler createHandler, string productType, bool isExactMatch = false, string parentFormularyId = null)
        {
            var hasExistingVTMRes = CheckIfProductExistsForFormulary(rec, productType, isExactMatch);

            var hasExistingVTM = (hasExistingVTMRes != null && hasExistingVTMRes.DoesExist);

            if (hasExistingVTM && hasExistingVTMRes.ExistingFormularyVersionId.IsNotEmpty())
            {
                var existingFormularyForVTM = formularyRepo.ItemsAsReadOnly.FirstOrDefault(rec => rec.FormularyVersionId == hasExistingVTMRes.ExistingFormularyVersionId);
                if (existingFormularyForVTM != null && existingFormularyForVTM.Code.IsNotEmpty())
                {
                    //take the most recent version
                    var existingFormulariesForCode = formularyRepo.ItemsAsReadOnly.Where(rec => rec.Code == existingFormularyForVTM.Code && rec.VersionId == 1)
                        ?.OrderByDescending(rec=> rec.Createdtimestamp).ToList();
                    //return existinFormularyForVTM;
                    if(existingFormulariesForCode.IsCollectionValid())
                    {
                        if(parentFormularyId.IsEmpty())
                            return existingFormulariesForCode[0];
                        if(existingFormulariesForCode[0].ParentFormularyId == parentFormularyId)
                            return existingFormulariesForCode[0];
                        var newOverriddenFormulary = createHandler.CreateFormulary();
                        newOverriddenFormulary.Code = existingFormulariesForCode[0].Code;//should have the same code
                        formulariesToBeSaved.Add(newOverriddenFormulary);
                        return newOverriddenFormulary;
                    }
                }
            }

            //Create New formulary and return that formulary
            var newFormulary = createHandler.CreateFormulary();
            formulariesToBeSaved.Add(newFormulary);
            return newFormulary;
        }


        private List<string> CheckIfHasExistingAMPDrugs(List<FormularyDTO> requestsData)
        {
            var existingAMPs = new List<string>();

            requestsData.Each(rec =>
            {
                var checkAMPRes = CheckIfProductExistsForFormulary(rec, "amp");

                if (checkAMPRes != null && checkAMPRes.DoesExist)
                {
                    existingAMPs.Add(checkAMPRes.ExistingFormularyVersionId);
                }
            });

            return existingAMPs;
        }

        private CheckIfProductExistsDTO CheckIfProductExistsForFormulary(FormularyDTO formulary, string productType, bool isExactMatch = false)
        {
            var ingredients = formulary.FormularyIngredients;
            var unitDoseFormSize = formulary.Detail.UnitDoseFormSize;
            var formulationName = formulary.Detail.FormCd.IsNotEmpty() ? _formCodeNames[formulary.Detail.FormCd] : null;
            var supplierName = formulary.Detail.SupplierCd.IsNotEmpty() ? (formulary.Detail.SupplierName.IsNotEmpty() ? formulary.Detail.SupplierName : (_supplierCodeNames.ContainsKey(formulary.Detail.SupplierCd) ? _supplierCodeNames[formulary.Detail.SupplierCd] : null)) : null;

            return _formularyQueries.CheckIfProductExists(ingredients, unitDoseFormSize.ToString(), formulationName, supplierName, productType, isExactMatch);
        }

        private void RePopulateDTOPostSave(FormularyHeader formularyHeader, CreateEditFormularyDTO createFormularyDTO)
        {
            var headerDTO = _mapper.Map<FormularyDTO>(formularyHeader);

            if (formularyHeader.FormularyDetail.IsCollectionValid())
                headerDTO.Detail = _mapper.Map<FormularyDetailDTO>(formularyHeader.FormularyDetail.First());

            if (formularyHeader.FormularyAdditionalCode.IsCollectionValid())
            {
                headerDTO.FormularyAdditionalCodes = _mapper.Map<List<FormularyAdditionalCodeDTO>>(formularyHeader.FormularyAdditionalCode.ToList());
            }

            if (formularyHeader.FormularyIngredient.IsCollectionValid())
            {
                headerDTO.FormularyIngredients = _mapper.Map<List<FormularyIngredientDTO>>(formularyHeader.FormularyIngredient.ToList());
            }

            if (formularyHeader.FormularyExcipient.IsCollectionValid())
            {
                headerDTO.FormularyExcipients = _mapper.Map<List<FormularyExcipientDTO>>(formularyHeader.FormularyExcipient.ToList());
            }

            if (formularyHeader.FormularyRouteDetail.IsCollectionValid())
            {
                headerDTO.FormularyRouteDetails = _mapper.Map<List<FormularyRouteDetailDTO>>(formularyHeader.FormularyRouteDetail.ToList());
            }

            if (formularyHeader.FormularyLocalRouteDetail.IsCollectionValid())
            {
                headerDTO.FormularyLocalRouteDetails = _mapper.Map<List<FormularyLocalRouteDetailDTO>>(formularyHeader.FormularyLocalRouteDetail.ToList());
            }

            createFormularyDTO.Data.Add(headerDTO);
        }
    }
}
