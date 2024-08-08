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
using Interneuron.FDBAPI.Client;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.Extensions;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Model.Search;
using LinqKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Commands
{
    public partial class FormularyCommand : IFormularyCommands
    {
        public async Task FixExistingFDBCodesByDescription(string token)
        {
            var formularyAdditionalCodeRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyAdditionalCode>)) as IFormularyRepository<FormularyAdditionalCode>;

            var locallicensesFVIds = formularyAdditionalCodeRepo.ItemsAsReadOnly.Where(rec => rec.AdditionalCodeSystem == "FDB").Select(rec => rec.FormularyVersionId).ToList();

            var batchsizeForVMPCodes = 3000;

            var batchedRequestsForVMPCodes = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < locallicensesFVIds.Count; reqIndex += batchsizeForVMPCodes)
            {
                var batches = locallicensesFVIds.Skip(reqIndex).Take(batchsizeForVMPCodes);
                batchedRequestsForVMPCodes.Add(batches.ToList());
            }

            var baseFDBUrl = _configuration.GetSection("FDB").GetValue<string>("BaseURL");
            baseFDBUrl = baseFDBUrl.EndsWith("/") ? baseFDBUrl.TrimEnd('/') : baseFDBUrl;
            var fdbClient = new FDBAPIClient(baseFDBUrl, null);
            var theraupeuticClasses = await fdbClient.GetAllTherapeuticClassifications(token);

            if (theraupeuticClasses == null || !theraupeuticClasses.Data.IsCollectionValid()) return;

            var descCodeLkp = new Dictionary<string, string>();
            theraupeuticClasses.Data.Keys.Each(k => descCodeLkp[theraupeuticClasses.Data[k]] = k);

            foreach (var batch in batchedRequestsForVMPCodes)
            {
                var addnlCodes = formularyAdditionalCodeRepo.Items.Where(rec => batch.Contains(rec.FormularyVersionId) && rec.AdditionalCodeSystem=="FDB").ToList();

                addnlCodes.AsParallel().ForEach(item =>
                {
                    if(item != null && item.AdditionalCode != null && item.AdditionalCodeDesc != null && descCodeLkp.ContainsKey(item.AdditionalCodeDesc))
                    {
                        item.AdditionalCode = descCodeLkp[item.AdditionalCodeDesc];
                    }
                });

                formularyAdditionalCodeRepo.SaveChanges();
            }
        }
        public async Task FixExistingLocalAndUnlicensedIndications()
        {
            await Task.Run(() =>
            {
                var formularyDetailRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyDetail>)) as IFormularyRepository<FormularyDetail>;

                var locallicensesFVIds = formularyDetailRepo.ItemsAsReadOnly.Where(rec => rec.LocalLicensedUse != null || rec.LocalUnlicensedUse != null).Select(rec => rec.FormularyVersionId).ToList();

                if (!locallicensesFVIds.IsCollectionValid()) return;

                var batchsizeForVMPCodes = 3000;

                var batchedRequestsForVMPCodes = new List<List<string>>();

                for (var reqIndex = 0; reqIndex < locallicensesFVIds.Count; reqIndex += batchsizeForVMPCodes)
                {
                    var batches = locallicensesFVIds.Skip(reqIndex).Take(batchsizeForVMPCodes);
                    batchedRequestsForVMPCodes.Add(batches.ToList());
                }

                foreach (var batch in batchedRequestsForVMPCodes)
                {
                    var locallicenses = formularyDetailRepo.Items.Where(rec => batch.Contains(rec.FormularyVersionId)).ToList();

                    //foreach (var item in locallicenses)
                    locallicenses.AsParallel().ForEach(item =>
                    {
                        if (item.LocalLicensedUse != null)
                        {
                            var localLicensedUses = item.LocalLicensedUse.SafeGetStringifiedCodeDescListForCode();
                            if (localLicensedUses.IsCollectionValid())
                            {
                                localLicensedUses.Each(u => u.Source = TerminologyConstants.MANUAL_DATA_SRC);
                                item.LocalLicensedUse = localLicensedUses.SafeGetFormularyLookupItemDTOListFromString();
                                formularyDetailRepo.Update(item);
                            }
                        }
                        if (item.LocalUnlicensedUse != null)
                        {
                            var localunlicensedUses = item.LocalUnlicensedUse.SafeGetStringifiedCodeDescListForCode();
                            if (localunlicensedUses.IsCollectionValid())
                            {
                                localunlicensedUses.Each(u => u.Source = TerminologyConstants.MANUAL_DATA_SRC);
                                item.LocalUnlicensedUse = localunlicensedUses.SafeGetFormularyLookupItemDTOListFromString();
                                formularyDetailRepo.Update(item);
                            }
                        }
                    });

                    formularyDetailRepo.SaveChanges();
                }
            });
        }

        public async Task AddBNFClassificationsInHierarchy() 
        {
            var bnfs = await _dMDQueries.GetLookup<DmdLookupBNFDTO>(Queries.LookupType.BNFCode);

            if (!bnfs.IsCollectionValid()) return;

            var bnfLkp = bnfs.Where(rec=> rec.Cd != null && rec.Cd != "").Distinct(rec => rec.Cd).ToDictionary(k => k.Cd, v => v.Desc);

            var formularyAddnlRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyAdditionalCode>)) as IFormularyRepository<FormularyAdditionalCode>;

            var bnfAddnlCodes = formularyAddnlRepo.ItemsAsReadOnly.Where(rec => rec.AdditionalCodeSystem == "BNF").ToList();

            var fvIdsWithAddnlCodes = bnfAddnlCodes.Select(rec=> new { FormularyVersionId = rec.FormularyVersionId, AddnlCode = rec.AdditionalCode }).ToList();

            var fvIdsWithAddnlCodesLkp = new Dictionary<string, List<string>>();
            fvIdsWithAddnlCodes.Each(rec =>
            {
                if (!fvIdsWithAddnlCodesLkp.ContainsKey(rec.FormularyVersionId))
                    fvIdsWithAddnlCodesLkp[rec.FormularyVersionId] = new List<string>();

                fvIdsWithAddnlCodesLkp[rec.FormularyVersionId].Add(rec.AddnlCode);
            });

            var batchsizeForVMPCodes = 5000;

            var batchedbnfAddnlCodes = new List<List<FormularyAdditionalCode>>();

            for (var reqIndex = 0; reqIndex < bnfAddnlCodes.Count; reqIndex += batchsizeForVMPCodes)
            {
                var batches = bnfAddnlCodes.Skip(reqIndex).Take(batchsizeForVMPCodes);
                batchedbnfAddnlCodes.Add(batches.ToList());
            }

            foreach (var batchedbnfAddnlCode in batchedbnfAddnlCodes)
            {
                foreach (var bnfAddnlCode in batchedbnfAddnlCode)
                {
                    if (bnfAddnlCode.AdditionalCode == null) continue;

                    var codesToBeAdded = new List<string>();
                    if (bnfAddnlCode.AdditionalCode.Length >= 2)
                        codesToBeAdded.Add(bnfAddnlCode.AdditionalCode.Substring(0, 2));
                    if (bnfAddnlCode.AdditionalCode.Length >= 4)
                        codesToBeAdded.Add(bnfAddnlCode.AdditionalCode.Substring(0, 4));
                    if (bnfAddnlCode.AdditionalCode.Length >= 6)
                        codesToBeAdded.Add(bnfAddnlCode.AdditionalCode.Substring(0, 6));

                    foreach (var code in codesToBeAdded)
                    {
                        if (!bnfLkp.ContainsKey(bnfAddnlCode.AdditionalCode)) continue;

                        if (fvIdsWithAddnlCodesLkp[bnfAddnlCode.FormularyVersionId].Any(rec => rec == code)) 
                            continue;//same code already there

                        var addnl = new FormularyAdditionalCode()
                        {
                            CodeType = TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE,
                            Source = "DMD",
                            AdditionalCode = code,
                            AdditionalCodeDesc = bnfLkp[code],
                            AdditionalCodeSystem = "BNF",
                            FormularyVersionId = bnfAddnlCode.FormularyVersionId
                        };

                        formularyAddnlRepo.Add(addnl);
                    }
                }
                formularyAddnlRepo.SaveChanges();
            }
        }

        public async Task UpdateBNFsDescription()
        {
            var bnfs = await _dMDQueries.GetLookup<DmdLookupBNFDTO>(Queries.LookupType.BNFCode);

            if (!bnfs.IsCollectionValid()) return;

            var bnfLkp = bnfs.Where(rec=> rec.Cd != null && rec.Cd != "").Distinct(rec => rec.Cd).ToDictionary(k => k.Cd, v => v.Desc);

            var formularyAddnlRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyAdditionalCode>)) as IFormularyRepository<FormularyAdditionalCode>;

            var bnfAddnlCodes = formularyAddnlRepo.Items.Where(rec => rec.AdditionalCodeSystem == "BNF").ToList();

            var batchsizeForVMPCodes = 1000;

            var batchedRequestsForVMPCodes = new List<List<FormularyAdditionalCode>>();

            for (var reqIndex = 0; reqIndex < bnfAddnlCodes.Count; reqIndex += batchsizeForVMPCodes)
            {
                var batches = bnfAddnlCodes.Skip(reqIndex).Take(batchsizeForVMPCodes);
                batchedRequestsForVMPCodes.Add(batches.ToList());
            }

            foreach (var bnfAddnlCode in bnfAddnlCodes)
            {
                if (bnfAddnlCode.AdditionalCode == null) continue;

                if (bnfAddnlCode.AdditionalCode.Length > 7)
                    bnfAddnlCode.AdditionalCode = bnfAddnlCode.AdditionalCode.Substring(0, 7);

                bnfAddnlCode.AdditionalCodeDesc = bnfLkp.ContainsKey(bnfAddnlCode.AdditionalCode) ? bnfLkp[bnfAddnlCode.AdditionalCode] : bnfAddnlCode.AdditionalCodeDesc;

                formularyAddnlRepo.Update(bnfAddnlCode);
            }

            formularyAddnlRepo.SaveChanges();
        }

        public async Task UpdateDefaultBNFs()
        {
            UpdateDefaultBNFsForAMPs();

            await UpdateDefaultBNFsFORVMPsAndVTMS("VMP");
            await UpdateDefaultBNFsFORVMPsAndVTMS("VTM");


            //var formularyBasicResultsRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

            //var ancestors = await formularyBasicResultsRepo.GetFormularyAncestorForCodes(uniqueCodes.ToArray());

            //var descendents = await formularyBasicResultsRepo.GetFormularyDescendentForCodes(uniqueCodes.ToArray());
        }

        private async Task UpdateDefaultBNFsFORVMPsAndVTMS(string productType)
        {
            //Get All VMPs
            var formularyHeaderRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            //first fill the defaults for metainfos that are null and the for the ones that are exisiting
            var allVMPFVIdsWithCodes = formularyHeaderRepo.ItemsAsReadOnly.Where(rec => rec.ProductType == productType && rec.IsLatest == true).Select(rec => new { FormularyVersionId = rec.FormularyVersionId, Code = rec.Code }).ToList();

            if (!allVMPFVIdsWithCodes.IsCollectionValid()) return;

            var batchsizeForVMPCodes = 3000;

            var allVMPCodes = allVMPFVIdsWithCodes.Select(rec => rec.Code).Distinct().ToList();

            var batchedRequestsForVMPCodes = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < allVMPCodes.Count; reqIndex += batchsizeForVMPCodes)
            {
                var batches = allVMPCodes.Skip(reqIndex).Take(batchsizeForVMPCodes);
                batchedRequestsForVMPCodes.Add(batches.ToList());
            }

            var vmpFVIdWithAMPFVIds = new Dictionary<string, List<string>>();

            foreach (var batch in batchedRequestsForVMPCodes)
            {
                var formularyBasicResultsRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

                var alldescendentsForCodes = await formularyBasicResultsRepo.GetFormularyDescendentForCodes(batch.ToArray());

                if (!alldescendentsForCodes.IsCollectionValid()) continue;

                //var allAMPFVIdsWithParentCodes = formularyHeaderRepo.ItemsAsReadOnly.Where(rec => rec.ProductType == "AMP" && batch.Contains(rec.ParentCode) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE).Select(rec => new { FormularyVersionId = rec.FormularyVersionId, ParentCode = rec.ParentCode }).ToList();
                var allAMPFVIdsWithParentCodes = new List<Tuple<string, string>>()
                                                .Select(t => new { FormularyVersionId = t.Item1, ParentCode = t.Item2 }).ToList();

                if (productType == "VMP")
                {
                    allAMPFVIdsWithParentCodes = alldescendentsForCodes.Where(rec => rec.ProductType == "AMP" && batch.Contains(rec.ParentCode) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE).Select(rec => new { FormularyVersionId = rec.FormularyVersionId, ParentCode = rec.ParentCode }).ToList();
                }
                else if (productType == "VTM")
                {
                    //prepare vmp and vtm code mapping
                    var vmpVTMCodeMappings = alldescendentsForCodes.Where(rec => rec.ProductType == "VMP" && batch.Contains(rec.ParentCode))?.Distinct(rec=> rec.Code).ToDictionary(k=> k.Code, v=> v.ParentCode);

                    if (!vmpVTMCodeMappings.IsCollectionValid()) continue;

                    var vmpCodes = vmpVTMCodeMappings.Keys;

                    allAMPFVIdsWithParentCodes = alldescendentsForCodes.Where(rec => rec.ProductType == "AMP" && vmpCodes.Contains(rec.ParentCode) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE).Select(rec => new { 
                        FormularyVersionId = rec.FormularyVersionId, 
                        ParentCode = vmpVTMCodeMappings.ContainsKey(rec.ParentCode) ? vmpVTMCodeMappings[rec.ParentCode]: null }).ToList();
                }

                if (!allAMPFVIdsWithParentCodes.IsCollectionValid()) continue;

                //for parent fvid store all child amp fvids

                foreach (var cd in batch)
                {
                    var vmpFVIds = allVMPFVIdsWithCodes.Where(rec=> rec.Code == cd)?.Distinct(rec=> rec.FormularyVersionId).Select(rec => rec.FormularyVersionId).ToList();
                    var ampFVIds = allAMPFVIdsWithParentCodes.Where(rec => rec.ParentCode == cd)?.Distinct(rec => rec.FormularyVersionId).Select(rec=> rec.FormularyVersionId).ToList();

                    vmpFVIds.Each(rec =>
                    {
                        //if (!vmpFVIdWithAMPFVIds.ContainsKey(rec))
                        //    vmpFVIdWithAMPFVIds[rec] = new List<string>();

                        vmpFVIdWithAMPFVIds[rec] = ampFVIds;
                    });
                }
            }

            if (!vmpFVIdWithAMPFVIds.IsCollectionValid()) return;

            var batchedRequestsForVMPs = new List<Dictionary<string, List<string>>>();

            for (var reqIndex = 0; reqIndex < vmpFVIdWithAMPFVIds.Count; reqIndex += batchsizeForVMPCodes)
            {
                var batches = vmpFVIdWithAMPFVIds.Skip(reqIndex).Take(batchsizeForVMPCodes);
                batchedRequestsForVMPs.Add(batches.ToDictionary(k => k.Key, v => v.Value));
            }

            foreach (var batchVMPFVIds in batchedRequestsForVMPs)
            {
                var allAMPFVIdsInBatch = new List<string>();
                batchVMPFVIds.Each(rec => allAMPFVIdsInBatch.AddRange(rec.Value)).ToList();

                var formularyAddnlRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyAdditionalCode>)) as IFormularyRepository<FormularyAdditionalCode>;

                var formularyHeaderToUpdateRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

                var addnlsToUpdate = formularyAddnlRepo.ItemsAsReadOnly.Where(rec => allAMPFVIdsInBatch.Contains(rec.FormularyVersionId) && rec.AdditionalCodeSystem == "BNF" && rec.AdditionalCode != null && rec.AdditionalCode.Length >= 7).ToList();

                var headersToUpdate = formularyHeaderToUpdateRepo.Items.Where(rec => batchVMPFVIds.Keys.Contains(rec.FormularyVersionId)).ToList();

                if (!addnlsToUpdate.IsCollectionValid() || !headersToUpdate.IsCollectionValid()) continue;

                foreach (var headerOfVMP in headersToUpdate)
                {
                    if (!batchVMPFVIds.ContainsKey(headerOfVMP.FormularyVersionId) || !batchVMPFVIds[headerOfVMP.FormularyVersionId].IsCollectionValid()) continue;

                    var ampFVIds = batchVMPFVIds[headerOfVMP.FormularyVersionId];

                    var bnfs = addnlsToUpdate.Where(rec=> ampFVIds.Contains(rec.FormularyVersionId))?.Distinct(rec=> rec.AdditionalCode).OrderBy(rec=> rec.AdditionalCode).ToList();

                    var defaultBNF = !bnfs.IsCollectionValid() ? null : ((bnfs.Count == 1) ? bnfs.FirstOrDefault() : bnfs[1]);//select the 2nd one

                    if (defaultBNF == null) continue;

                    SetDefaultClassificationCodesFromDB(headerOfVMP, defaultBNF);

                    formularyHeaderToUpdateRepo.Update(headerOfVMP);
                }

                formularyHeaderRepo.SaveChanges();

            }
        }

        private void UpdateDefaultBNFsForAMPs()
        {
            //Get All AMPs
            var formularyHeaderRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            //first fill the defaults for metainfos that are null and the for the ones that are exisiting
            var allAMPFVIds = formularyHeaderRepo.ItemsAsReadOnly.Where(rec => rec.ProductType == "AMP" && rec.IsLatest == true).Select(rec => rec.FormularyVersionId).ToList();

            var batchsizeForDesc = 3000;

            var batchedRequestsForDesc = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < allAMPFVIds.Count; reqIndex += batchsizeForDesc)
            {
                var batches = allAMPFVIds.Skip(reqIndex).Take(batchsizeForDesc);
                batchedRequestsForDesc.Add(batches.ToList());
            }

            foreach (var batch in batchedRequestsForDesc)
            {
                var formularyAddnlRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyAdditionalCode>)) as IFormularyRepository<FormularyAdditionalCode>;

                var formularyHeaderToUpdateRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

                var addnlsToUpdate = formularyAddnlRepo.ItemsAsReadOnly.Where(rec => batch.Contains(rec.FormularyVersionId) && rec.AdditionalCodeSystem == "BNF" && rec.AdditionalCode != null && rec.AdditionalCode.Length >= 7).ToList();

                var headerToUpdate = formularyHeaderToUpdateRepo.Items.Where(rec => batch.Contains(rec.FormularyVersionId)).ToList();

                if (!addnlsToUpdate.IsCollectionValid()) continue;

                var uniqueFVIds = addnlsToUpdate.Select(rec => rec.FormularyVersionId).Distinct().ToList();

                foreach (var uniqueFVId in uniqueFVIds)
                {
                    var bnfs = addnlsToUpdate.Where(rec => rec.FormularyVersionId == uniqueFVId)?.Distinct(rec=> rec.AdditionalCode).OrderBy(rec => rec.AdditionalCode).ToList();
                    var header = headerToUpdate.Where(rec => rec.FormularyVersionId == uniqueFVId).FirstOrDefault();

                    var defaultBNF = !bnfs.IsCollectionValid() ? null : ((bnfs.Count == 1) ? bnfs.FirstOrDefault() : bnfs[1]);//select the 2nd one

                    if (defaultBNF == null || header == null) continue;

                    SetDefaultClassificationCodesFromDB(header, defaultBNF);

                    formularyHeaderToUpdateRepo.Update(header);
                }

                formularyHeaderRepo.SaveChanges();
            }
        }

        private void SetDefaultClassificationCodesFromDB(FormularyHeader header, FormularyAdditionalCode defaultBNFCodeToSet)
        {
            if (defaultBNFCodeToSet == null || string.IsNullOrEmpty(defaultBNFCodeToSet.AdditionalCode)) return;

            var rootObj = string.IsNullOrEmpty(header.MetaInfoJson) ? new JObject() : JObject.Parse(header.MetaInfoJson);

            rootObj[TerminologyConstants.DEF_CLASS_CODES] = rootObj[TerminologyConstants.DEF_CLASS_CODES] ?? new JObject();

            var defClassificationCode = rootObj[TerminologyConstants.DEF_CLASS_CODES];
            var bnfCode = defClassificationCode[TerminologyConstants.DEF_BNF_CLASS_CODE];

            if (bnfCode != null && !string.IsNullOrEmpty(bnfCode.ToString()))
                return;//exists already

            var key = TerminologyConstants.DEF_TEMPLATE_CLASS_CODES.Replace("{template}", defaultBNFCodeToSet.AdditionalCodeSystem.ToLowerInvariant());
            defClassificationCode[key] = defaultBNFCodeToSet.AdditionalCode.Length > 7 ? defaultBNFCodeToSet.AdditionalCode.Substring(0, 7): defaultBNFCodeToSet.AdditionalCode;

            rootObj[TerminologyConstants.DEF_CLASS_CODES] = defClassificationCode;

            header.MetaInfoJson = rootObj.ToString();
        }


        //    //var defaultClassifictionCodes = new Dictionary<string, string>();
        //    List<FormularyAdditionalCodeDTO> addnlCodes = new List<FormularyAdditionalCodeDTO>();

        //    if (!string.IsNullOrEmpty(header.MetaInfoJson))
        //    {
        //        var meta = JObject.Parse(header.MetaInfoJson);
        //        if (meta != null)
        //        {
        //            var defClassificationCode = meta[TerminologyConstants.DEF_CLASS_CODES];
        //            if (defClassificationCode != null)
        //            {
        //                var bnfCode = defClassificationCode[TerminologyConstants.DEF_BNF_CLASS_CODE];
        //                var atcCode = defClassificationCode[TerminologyConstants.DEF_ATC_CLASS_CODES];
        //                var fdbCode = defClassificationCode[TerminologyConstants.DEF_FDB_CLASS_CODES];

        //                if (bnfCode != null && !string.IsNullOrEmpty(bnfCode.ToString()))
        //                    defaultClassifictionCodes[TerminologyConstants.DEF_BNF_CLASS_CODE] = bnfCode.ToString();
        //                if (atcCode != null && !string.IsNullOrEmpty(atcCode.ToString()))
        //                    defaultClassifictionCodes[TerminologyConstants.DEF_ATC_CLASS_CODES] = atcCode.ToString();
        //                if (fdbCode != null && !string.IsNullOrEmpty(fdbCode.ToString()))
        //                    defaultClassifictionCodes[TerminologyConstants.DEF_FDB_CLASS_CODES] = fdbCode.ToString();
        //            }
        //        }
        //    }

        //    return defaultClassifictionCodes;
        //}


        //public string PrepareDefaultAdditionalCodesAsMeta(List<FormularyAdditionalCodeDTO> addnlCodes)
        //{
        //    if (!addnlCodes.IsCollectionValid()) return string.Empty;

        //    var classObj = new JObject();

        //    foreach (var addnlCode in addnlCodes)
        //    {
        //        if (addnlCode.IsDefault)
        //        {
        //            var key = TerminologyConstants.DEF_TEMPLATE_CLASS_CODES.Replace("{template}", addnlCode.AdditionalCodeSystem.ToLowerInvariant());
        //            classObj[key] = addnlCode.AdditionalCode;
        //        }
        //    }
        //    var rootObj = new JObject();
        //    rootObj[TerminologyConstants.DEF_CLASS_CODES] = classObj;

        //    return rootObj.ToString();
        //}
    }
}
