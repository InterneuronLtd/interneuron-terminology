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
using Interneuron.FDBAPI.Client.DataModels;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Model.Search;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Extensions
{
    public static class FormularyExtension
    {
        public static FormularyHeader CloneFormulary(this FormularyHeader existingRecord, IMapper mapper, string rootEntityIdentifier = null)
        {
            var newFormularyHeader = mapper.Map<FormularyHeader>(existingRecord);

            newFormularyHeader.RowId = Guid.NewGuid().ToString();

            if (!rootEntityIdentifier.IsEmpty())
                newFormularyHeader.FormularyVersionId = rootEntityIdentifier;

            if (existingRecord.FormularyDetail.IsCollectionValid())
            {
                newFormularyHeader.FormularyDetail = mapper.Map<ICollection<FormularyDetail>>(existingRecord.FormularyDetail);
                newFormularyHeader.FormularyDetail.First().FormularyVersionId = newFormularyHeader.FormularyVersionId;
                newFormularyHeader.FormularyDetail.First().RowId = Guid.NewGuid().ToString();
            }

            if (existingRecord.FormularyAdditionalCode.IsCollectionValid())
            {
                newFormularyHeader.FormularyAdditionalCode = mapper.Map<ICollection<FormularyAdditionalCode>>(existingRecord.FormularyAdditionalCode);
                newFormularyHeader.FormularyAdditionalCode.Each(ac =>
                {
                    ac.FormularyVersionId = newFormularyHeader.FormularyVersionId;
                    ac.RowId = Guid.NewGuid().ToString();
                });
            }

            //if (existingRecord.FormularyIndication.IsCollectionValid())
            //{
            //    newFormularyHeader.FormularyIndication = mapper.Map<ICollection<FormularyIndication>>(existingRecord.FormularyIndication);
            //    newFormularyHeader.FormularyIndication.Each(rec =>
            //    {
            //        rec.FormularyVersionId = newFormularyHeader.FormularyVersionId;
            //        rec.RowId = Guid.NewGuid().ToString();
            //    });
            //}

            if (existingRecord.FormularyIngredient.IsCollectionValid())
            {
                newFormularyHeader.FormularyIngredient = mapper.Map<ICollection<FormularyIngredient>>(existingRecord.FormularyIngredient);
                newFormularyHeader.FormularyIngredient.Each(rec =>
                {
                    rec.FormularyVersionId = newFormularyHeader.FormularyVersionId;
                    rec.RowId = Guid.NewGuid().ToString();
                });
            }

            if (existingRecord.FormularyRouteDetail.IsCollectionValid())
            {
                newFormularyHeader.FormularyRouteDetail = mapper.Map<ICollection<FormularyRouteDetail>>(existingRecord.FormularyRouteDetail);
                newFormularyHeader.FormularyRouteDetail.Each(rec =>
                {
                    rec.FormularyVersionId = newFormularyHeader.FormularyVersionId;
                    rec.RowId = Guid.NewGuid().ToString();
                });
            }

            if (existingRecord.FormularyLocalRouteDetail.IsCollectionValid())
            {
                newFormularyHeader.FormularyLocalRouteDetail = mapper.Map<ICollection<FormularyLocalRouteDetail>>(existingRecord.FormularyLocalRouteDetail);
                newFormularyHeader.FormularyLocalRouteDetail.Each(rec =>
                {
                    rec.FormularyVersionId = newFormularyHeader.FormularyVersionId;
                    rec.RowId = Guid.NewGuid().ToString();
                });
            }

            if (existingRecord.FormularyExcipient.IsCollectionValid())
            {
                newFormularyHeader.FormularyExcipient = mapper.Map<ICollection<FormularyExcipient>>(existingRecord.FormularyExcipient);
                newFormularyHeader.FormularyExcipient.Each(rec =>
                {
                    rec.FormularyVersionId = newFormularyHeader.FormularyVersionId;
                    rec.RowId = Guid.NewGuid().ToString();
                });
            }

            //if (existingRecord.FormularyOntologyForm.IsCollectionValid())
            //{
            //    newFormularyHeader.FormularyOntologyForm = mapper.Map<ICollection<FormularyOntologyForm>>(existingRecord.FormularyOntologyForm);
            //    newFormularyHeader.FormularyOntologyForm.Each(rec =>
            //    {
            //        rec.FormularyVersionId = newFormularyHeader.FormularyVersionId;
            //        rec.RowId = Guid.NewGuid().ToString();
            //    });
            //}

            return newFormularyHeader;
        }

        public static string SafeGetStringifiedCodeDescListForCode(this string code, Dictionary<string, List<string>> listData, string source = null)
        {
            if (code.IsEmpty() || !listData.IsCollectionValid()) return null;

            if (!listData.ContainsKey(code)) return null;

            var data = listData[code];

            if (data == null) return null;

            var list = data.Select(rec => new FormularyLookupItemDTO { Cd = rec, Desc = rec, Source = source }).ToList();

            if (!list.IsCollectionValid()) return null;

            return JsonConvert.SerializeObject(list);
        }

        public static string SafeGetStringifiedCodeDescListForCode(this string code, Dictionary<string, List<FDBIdText>> listData, string source = null)
        {
            if (code.IsEmpty() || !listData.IsCollectionValid()) return null;

            if (!listData.ContainsKey(code)) return null;

            var data = listData[code];

            if (data == null) return null;

            var list = data.Select(rec => new FormularyLookupItemDTO { Cd = rec.Id, Desc = rec.Text, Source = source }).ToList();

            if (!list.IsCollectionValid()) return null;

            return JsonConvert.SerializeObject(list);
        }

        public static List<FormularyLookupItemDTO> SafeGetStringifiedCodeDescListForCode(this string dataAsString)
        {
            if (dataAsString.IsEmpty()) return null;

            List<FormularyLookupItemDTO> dataAsList = null;

            try
            {
                dataAsList = JsonConvert.DeserializeObject<List<FormularyLookupItemDTO>>(dataAsString);//id and text
            }
            catch { dataAsList = null; }

            if (dataAsList == null) return null;

            return dataAsList;
        }

        public static string SafeGetFormularyLookupItemDTOListFromString(this List<FormularyLookupItemDTO> codeDescList)
        {
            if (!codeDescList.IsCollectionValid()) return null;

            string sringified = null;
            try
            {
                sringified = JsonConvert.SerializeObject(codeDescList);
            }
            catch { sringified = null; }
            return sringified;
        }


        public static FormularyHeader CloneFormularyV2(this FormularyHeader existingFormulary, IMapper mapper)
        {
            var newFormulary = mapper.Map<FormularyHeader>(existingFormulary);
            var newFormularyVersionId = Guid.NewGuid().ToString();

            if (existingFormulary.FormularyAdditionalCode.IsCollectionValid())
            {
                newFormulary.FormularyAdditionalCode = mapper.Map<List<FormularyAdditionalCode>>(existingFormulary.FormularyAdditionalCode);
                newFormulary.FormularyAdditionalCode?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyDetail.IsCollectionValid())
            {
                newFormulary.FormularyDetail = mapper.Map<List<FormularyDetail>>(existingFormulary.FormularyDetail);
                newFormulary.FormularyDetail?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyRouteDetail.IsCollectionValid())
            {
                newFormulary.FormularyRouteDetail = mapper.Map<List<FormularyRouteDetail>>(existingFormulary.FormularyRouteDetail);
                newFormulary.FormularyRouteDetail?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyLocalRouteDetail.IsCollectionValid())
            {
                newFormulary.FormularyLocalRouteDetail = mapper.Map<List<FormularyLocalRouteDetail>>(existingFormulary.FormularyLocalRouteDetail);
                newFormulary.FormularyLocalRouteDetail?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyIngredient.IsCollectionValid())
            {
                newFormulary.FormularyIngredient = mapper.Map<List<FormularyIngredient>>(existingFormulary.FormularyIngredient);
                newFormulary.FormularyIngredient?.Each(rec => { rec.FormularyVersionId = newFormularyVersionId; rec.RowId = null; });
            }
            if (existingFormulary.FormularyExcipient.IsCollectionValid())
            {
                newFormulary.FormularyExcipient = mapper.Map<List<FormularyExcipient>>(existingFormulary.FormularyExcipient);
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

        /// <summary>
        /// This method returns the FormularyId of the 'code' from the 'Active' tree.
        /// If no 'active' record exists then it will return null.
        /// </summary>
        /// <param name="codes"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static async Task<Dictionary<string, string>> GetActiveFormularyIdForCode(this List<string> codes, IServiceProvider serviceProvider)
        {
            #region E.g.
            /// VTM01 1
            ///-VMP01 VTM01 1 -11
            ///--AMP01 VMP01 -11 -111 - Draft

            ///VTM01 2
            ///-VMP01 VTM01 2 -12
            ///--AMP01 VMP01 -12 -112 - Active

            ///vtm01 - 1, 2
            ///-should be 2 selected
            ///vmp01 - 11, 12
            ///-should be 12 selected
            #endregion
            if (!codes.IsCollectionValid()) return null;

            var repo = serviceProvider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var formularyBasicResultsRepo = serviceProvider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

            var descendents = (await formularyBasicResultsRepo.GetFormularyDescendentForCodes(codes.ToArray()))?.ToList();

            if (!descendents.IsCollectionValid()) return null;

            var codeFIds = repo.ItemsAsReadOnly.Where(rec => codes.Contains(rec.Code) && rec.IsLatest == true)
                ?.Select(rec => new { rec.Code, rec.FormularyId })
                .ToList();

            //there can be only one active amp which is latest
            var activeAMPs = descendents
                .Where(rec => rec.ProductType == "AMP" && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE && rec.IsLatest == true)
                ?.Select(rec => new { rec.Code, rec.FormularyId })
                .Distinct(rec => rec.Code)
                .ToList();

            if (!activeAMPs.IsCollectionValid()) return null;

            var activeAMPFormularyIds = activeAMPs.Select(rec => rec.FormularyId).ToList();


            //go to the root for active amps
            var ancestors = repo.GetFormularyAncestorsForFormularyIdsAsLookup(activeAMPFormularyIds);

            if (!ancestors.IsCollectionValid()) return null;

            var lkp = new Dictionary<string, string>();

            var ancestorFIds = ancestors.Keys.Distinct().ToHashSet();

            codeFIds.Each(rec => {
                if (ancestorFIds.Contains(rec.FormularyId))
                {
                    lkp[rec.Code] = rec.FormularyId;
                }
            });

            return lkp;
        }
    }
}
