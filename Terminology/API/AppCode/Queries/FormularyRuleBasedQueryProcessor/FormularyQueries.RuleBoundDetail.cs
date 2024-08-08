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
﻿using Interneuron.Caching;
using Interneuron.Common.Extensions;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.Extensions;
using Interneuron.Terminology.API.AppCode.Infrastructure.Caching;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Model.Search;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Queries
{
    public partial class FormularyQueries : IFormularyQueries
    {
        /// <summary>
        /// The codes passed are mapped to the same tree of the FVId
        /// and the 'AMPs' of the same tree (irrespective of the state) is used to bind the rules for aggregation.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="getAllAdditionalCodes"></param>
        /// <returns></returns>
        public async Task<FormularyDTO> GetFormularyDetailRuleBound(string id, bool getAllAdditionalCodes = false)
        {
            #region steps
            //get Current object by id

            //Create the complete DTO object for it

            //Verify the product type of the current object

            //Get Ancestors or descendents or both based on the product type

            //1.
            #endregion steps

            var results = await GetFormularyDetailRuleBoundForFVIds(new List<string> { id }, getAllAdditionalCodes);

            if (results == null) return null;
            
            return results[0];

            #region Old code - ref only
            /*
            id = id.Trim();
            var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var resultsFromDB = repo.GetFormularyDetail(id);

            var resultObj = resultsFromDB.FirstOrDefault();

            if (resultObj == null) return null;

            var builderType = GetFormBuilderType(resultObj);

            var orchestrator = new RuleBoundFormularyBuilderOrchestrator(builderType);

            await orchestrator.BuildFormulary(resultObj, getAllAddnlCodes: getAllAdditionalCodes);

            var formularyDTO = builderType.FormularyDTO;

            formularyDTO.Detail.LicensedUses.Sort((a, b) => a.Desc.CompareTo(b.Desc));

            formularyDTO.Detail.ContraIndications.Sort((a, b) => a.Desc.CompareTo(b.Desc));

            formularyDTO.Detail.SideEffects.Sort((a, b) => a.Desc.CompareTo(b.Desc));

            formularyDTO.Detail.Cautions.Sort((a, b) => a.Desc.CompareTo(b.Desc));

            return formularyDTO;
            */
            #endregion Old code - ref only
        }

        #region old code - ref only
        //private List<FormularyHeader> GetActiveDescendentsWithAllDetailsForFormularyIds(List<string> formularyIds)
        //{
        //    if (!formularyIds.IsCollectionValid()) return null;

        //    var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

        //    var descendentsLkp = repo.GetFormularyDescendentsForFormularyIdsAsLookup(formularyIds, onlyActiveRecs: true);

        //    var descendentFIds = new List<string>();

        //    if (descendentsLkp.IsCollectionValid())
        //        descendentsLkp.Values.Each(rec => descendentFIds.AddRange(rec));

        //    var fvIds = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && descendentFIds.Contains(rec.FormularyId))?.Select(rec => rec.FormularyVersionId).Distinct().ToList();

        //    var descendents = repo.GetFormularyListForIds(fvIds)?.ToList();

        //    return descendents;
        //}
        #endregion old code - ref only

        /// <summary>
        /// The codes passed are mapped to the same tree of the FVIds
        /// and the 'AMPs' of the same tree (irrespective of the state) is used to bind the rules for aggregation.
        /// </summary>
        /// <param name="fvIds"></param>
        /// <param name="getAllAdditionalCodes"></param>
        /// <returns></returns>
        public async Task<List<FormularyDTO>> GetFormularyDetailRuleBoundForFVIds(List<string> fvIds, bool getAllAdditionalCodes = false)
        {
            fvIds = fvIds.Select(id=>id.Trim()).ToList();
            
            var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var resultsFromDB = repo.GetFormularyListForIds(fvIds).ToList();

            if (!resultsFromDB.IsCollectionValid()) return null;

            var formularyIdWithProdType = repo.ItemsAsReadOnly.Where(rec => fvIds.Contains(rec.FormularyVersionId))?.Select(rec => new { rec.FormularyId,  rec.ProductType }).ToList();

            var formularyIds = formularyIdWithProdType.Select(rec => rec.FormularyId).ToList();

            var uniqueProdTypes = formularyIdWithProdType.Select(rec => rec.ProductType).Distinct().ToHashSet();

            var descendentListForFormularyId = new ConcurrentDictionary<string, List<FormularyHeader>>();

            if (uniqueProdTypes.Any(rec => string.Compare(rec, "AMP", true) != 0))
                descendentListForFormularyId = await GetDescendentsForFormularyIdsAsLkp(formularyIds);

            #region old code - ref only
            /*
            var codesWithProdType = repo.ItemsAsReadOnly.Where(rec => fvIds.Contains(rec.FormularyVersionId))?.Select(rec=> new { Code = rec.Code, ProductType = rec.ProductType }).ToList();

            var codes = codesWithProdType.Select(rec=> rec.Code).ToList();
            var uniqueProdTypes = codesWithProdType.Select(rec=> rec.ProductType).Distinct().ToHashSet();

            var descendentListForCode = new ConcurrentDictionary<string, List<FormularyHeader>>();

            if (uniqueProdTypes.Any(rec => string.Compare(rec, "AMP", true) != 0))
                descendentListForCode = await GetDescendentsForCodesAsLkp(codes);
            */
            #endregion old code - ref only

            var resultsList = new List<FormularyDTO>();

            foreach (var rec in resultsFromDB)
            {
                //var descendents = descendentListForCode.ContainsKey(rec.Code) ? descendentListForCode[rec.Code] : null;
                var descendents = descendentListForFormularyId.ContainsKey(rec.FormularyId) ? descendentListForFormularyId[rec.FormularyId] : null;
                var builderType = GetFormBuilderType(rec);

                var orchestrator = new RuleBoundFormularyBuilderOrchestrator(builderType);

                await orchestrator.BuildFormulary(rec, null, descendents, getAllAdditionalCodes);

                var formularyDTO = builderType.FormularyDTO;

                formularyDTO.Detail.LicensedUses.Sort((a, b) => a.Desc.CompareTo(b.Desc));

                formularyDTO.Detail.ContraIndications.Sort((a, b) => a.Desc.CompareTo(b.Desc));

                formularyDTO.Detail.SideEffects.Sort((a, b) => a.Desc.CompareTo(b.Desc));

                formularyDTO.Detail.Cautions.Sort((a, b) => a.Desc.CompareTo(b.Desc));

                resultsList.Add(formularyDTO);
            }

            #region old code - ref only (before mmc-477)
            //var resultsList = await BuildFormularyDetailByRule(resultsFromDB, descendentListForCode, getAllAdditionalCodes, ignoreRecStsCd: true);

            //if(!resultsList.IsCollectionValid() ) return null;

            //resultsList.Each(formularyDTO =>
            //{
            //    formularyDTO.Detail.LicensedUses.Sort((a, b) => a.Desc.CompareTo(b.Desc));

            //    formularyDTO.Detail.ContraIndications.Sort((a, b) => a.Desc.CompareTo(b.Desc));

            //    formularyDTO.Detail.SideEffects.Sort((a, b) => a.Desc.CompareTo(b.Desc));

            //    formularyDTO.Detail.Cautions.Sort((a, b) => a.Desc.CompareTo(b.Desc));
            //});
            #endregion old code - ref only (before mmc-477)

            return resultsList;
        }

        public async Task<FormularyDTO> GetActiveFormularyDetailRuleBoundByCode(string code, bool fromCache = true, bool includeInvalid = false)
        {
            #region steps
            //get Current object by code

            //Create the complete DTO object for it

            //Verify the product type of the current object

            //Get Ancestors or descendents or both based on the product type

            //1.
            #endregion steps

            code = code.Trim();

            //Log.Logger.Error($"Info: Value of From Cache {fromCache}");


            if (fromCache)
            {
                var timer = new Stopwatch();
                timer.Start();

                //check if can ping server - can use this but multiple cache calls
                //bool canPing = await CacheService.ServerPingAsync();

                //Log.Logger.Error("Info: Trying to fetch from cache");

                FormularyDTO record = null;

                //unable to get it from cache - fallback on db
                try
                {
                    record = await CacheService.GetAsync<FormularyDTO>($"{CacheKeys.ACTIVE_FORMULARY}{code}");
                }
                catch { }

                if (record != null)
                {
                    timer.Stop();
                    TimeSpan timeTaken = timer.Elapsed;
                    var msg = $"Info: Completed fetching from api. Time taken: {timeTaken.ToString(@"hh\:mm\:ss\.fff")}";
                    //Log.Logger.Error(msg);

                    return record;
                }

                timer.Stop();
            }
            var results = await GetActiveFormularyDetailRuleBoundByCodes(new List<string> { code }, fromCache, includeInvalid);
            
            if (results != null) return results[0];

            return null;

            //Log.Logger.Error("Info: Bypassed cache..fetching from DB.");

            //var timerDb = new Stopwatch();
            //timerDb.Start();

            #region Old Code - ref only
            /*
            var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var currentActiveRecsForCode = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && rec.Code == code).ToList();

            if (!currentActiveRecsForCode.IsCollectionValid()) return null;

            currentActiveRecsForCode = currentActiveRecsForCode.Where(rec =>
            {
                //EPMA-2702 - Recordstatus will be there for VTM and VMPs also and that need to be checked
                //if (string.Compare(rec.ProductType, "amp", true) != 0) return true;

                return rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE;
            }).ToList();

            if (!currentActiveRecsForCode.IsCollectionValid()) return null;

            var idForCurrentActiveRecsForCode = currentActiveRecsForCode.Select(rec => rec.FormularyVersionId).First();

            var resultsFromDB = repo.GetFormularyDetail(idForCurrentActiveRecsForCode);

            var resultObj = resultsFromDB.FirstOrDefault();

            if (resultObj == null) return null;

            var builderType = GetFormBuilderType(resultObj);

            var orchestrator = new RuleBoundFormularyBuilderOrchestrator(builderType);

            await orchestrator.BuildFormulary(resultObj);

            if (builderType.FormularyDTO.FormularyRouteDetails != null && builderType.FormularyDTO.FormularyRouteDetails.Count > 0)
            {
                builderType.FormularyDTO.FormularyRouteDetails.Clear();
            }

            if (builderType.FormularyDTO.Detail.LicensedUses != null && builderType.FormularyDTO.Detail.LicensedUses.Count > 0)
            {
                builderType.FormularyDTO.Detail.LicensedUses.Clear();
            }

            if (builderType.FormularyDTO.Detail.UnLicensedUses != null && builderType.FormularyDTO.Detail.UnLicensedUses.Count > 0)
            {
                builderType.FormularyDTO.Detail.UnLicensedUses.Clear();
            }

            if (builderType.FormularyDTO.FormularyLocalRouteDetails.IsCollectionValid())
            {
                builderType.FormularyDTO.FormularyLocalRouteDetails.Where(x => x.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL).Each(rec => {
                    FormularyRouteDetailDTO formularyRouteDetailDTO = new FormularyRouteDetailDTO();

                    formularyRouteDetailDTO.Createdby = rec.Createdby;
                    formularyRouteDetailDTO.Createddate = rec.Createddate;
                    formularyRouteDetailDTO.FormularyVersionId = rec.FormularyVersionId;
                    formularyRouteDetailDTO.RouteCd = rec.RouteCd;
                    formularyRouteDetailDTO.RouteDesc = rec.RouteDesc;
                    formularyRouteDetailDTO.RouteFieldTypeCd = rec.RouteFieldTypeCd;
                    formularyRouteDetailDTO.RouteFieldTypeDesc = rec.RouteFieldTypeDesc;
                    formularyRouteDetailDTO.RowId = rec.RowId;
                    formularyRouteDetailDTO.Source = rec.Source;
                    formularyRouteDetailDTO.Updatedby = rec.Updatedby;
                    formularyRouteDetailDTO.Updateddate = rec.Updateddate;

                    if (builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
                    {
                        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
                    }
                    else
                    {
                        builderType.FormularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

                        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
                    }

                });

                builderType.FormularyDTO.FormularyLocalRouteDetails.Where(x => x.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED).Each(rec => {
                    FormularyRouteDetailDTO formularyRouteDetailDTO = new FormularyRouteDetailDTO();

                    formularyRouteDetailDTO.Createdby = rec.Createdby;
                    formularyRouteDetailDTO.Createddate = rec.Createddate;
                    formularyRouteDetailDTO.FormularyVersionId = rec.FormularyVersionId;
                    formularyRouteDetailDTO.RouteCd = rec.RouteCd;
                    formularyRouteDetailDTO.RouteDesc = rec.RouteDesc;
                    formularyRouteDetailDTO.RouteFieldTypeCd = rec.RouteFieldTypeCd;
                    formularyRouteDetailDTO.RouteFieldTypeDesc = rec.RouteFieldTypeDesc;
                    formularyRouteDetailDTO.RowId = rec.RowId;
                    formularyRouteDetailDTO.Source = rec.Source;
                    formularyRouteDetailDTO.Updatedby = rec.Updatedby;
                    formularyRouteDetailDTO.Updateddate = rec.Updateddate;

                    if (builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
                    {
                        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
                    }
                    else
                    {
                        builderType.FormularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

                        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
                    }

                });
            }

            builderType.FormularyDTO.Detail.LocalLicensedUses.Each(rec => {
                FormularyLookupItemDTO formularyLookupItemDTO = new FormularyLookupItemDTO();

                formularyLookupItemDTO.AdditionalProperties = rec.AdditionalProperties;
                formularyLookupItemDTO.Cd = rec.Cd;
                formularyLookupItemDTO.Desc = rec.Desc;
                formularyLookupItemDTO.IsDefault = rec.IsDefault;
                formularyLookupItemDTO.Recordstatus = rec.Recordstatus;
                formularyLookupItemDTO.Source = rec.Source;
                formularyLookupItemDTO.Type = rec.Type;

                if (builderType.FormularyDTO.Detail.LicensedUses.IsCollectionValid())
                {
                    builderType.FormularyDTO.Detail.LicensedUses.Add(formularyLookupItemDTO);
                }
                else
                {
                    builderType.FormularyDTO.Detail.LicensedUses = new List<FormularyLookupItemDTO>();

                    builderType.FormularyDTO.Detail.LicensedUses.Add(formularyLookupItemDTO);
                }
            });

            builderType.FormularyDTO.Detail.LocalUnLicensedUses.Each(rec => {
                FormularyLookupItemDTO formularyLookupItemDTO = new FormularyLookupItemDTO();

                formularyLookupItemDTO.AdditionalProperties = rec.AdditionalProperties;
                formularyLookupItemDTO.Cd = rec.Cd;
                formularyLookupItemDTO.Desc = rec.Desc;
                formularyLookupItemDTO.IsDefault = rec.IsDefault;
                formularyLookupItemDTO.Recordstatus = rec.Recordstatus;
                formularyLookupItemDTO.Source = rec.Source;
                formularyLookupItemDTO.Type = rec.Type;

                if (builderType.FormularyDTO.Detail.UnLicensedUses.IsCollectionValid())
                {
                    builderType.FormularyDTO.Detail.UnLicensedUses.Add(formularyLookupItemDTO);
                }
                else
                {
                    builderType.FormularyDTO.Detail.UnLicensedUses = new List<FormularyLookupItemDTO>();

                    builderType.FormularyDTO.Detail.UnLicensedUses.Add(formularyLookupItemDTO);
                }

            });

            if (builderType.FormularyDTO.FormularyLocalRouteDetails != null && builderType.FormularyDTO.FormularyLocalRouteDetails.Count > 0)
            {
                builderType.FormularyDTO.FormularyLocalRouteDetails.Clear();
            }

            if (builderType.FormularyDTO.Detail.LocalLicensedUses != null && builderType.FormularyDTO.Detail.LocalLicensedUses.Count > 0)
            {
                builderType.FormularyDTO.Detail.LocalLicensedUses.Clear();
            }

            if (builderType.FormularyDTO.Detail.LocalUnLicensedUses != null && builderType.FormularyDTO.Detail.LocalUnLicensedUses.Count > 0)
            {
                builderType.FormularyDTO.Detail.LocalUnLicensedUses.Clear();
            }

            if (builderType.FormularyDTO.RecStatusCode != TerminologyConstants.RECORDSTATUS_ACTIVE) return null;

            //timerDb.Stop();
            //TimeSpan timeTakenDbCall = timerDb.Elapsed;
            //var msgDb = $"Info: Completed fetching from api from db. Time taken: {timeTakenDbCall.ToString(@"hh\:mm\:ss\.fff")}";
            //Log.Logger.Error(msgDb);

            return builderType.FormularyDTO;
            */
            #endregion Old Code - ref only

        }

        public Dictionary<string, long> GetFormularyIdOrderInfoLookup(List<string> formularyIds)
        {
            var inputFIds = formularyIds.Distinct().ToHashSet();
            var results = new Dictionary<string, long>();

            if (!formularyIds.IsCollectionValid()) return results;

            formularyIds = formularyIds.Distinct().ToList();

            var repo = _provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var codesForFIDs = repo.ItemsAsReadOnly.Where(rec => formularyIds.Contains(rec.FormularyId) && rec.VersionId == 1).Select(rec=> rec.Code).ToList();

            var details = repo.ItemsAsReadOnly.Where(rec => codesForFIDs.Contains(rec.Code) && rec.VersionId == 1)
                ?.Select(rec => new { rec.Code, rec.PrevCode, rec.FormularyId, rec.VersionId, rec.Createdtimestamp })
                .OrderBy(rec=> rec.Createdtimestamp)
                .ToList();

            if (!details.IsCollectionValid()) return results;

            var codesForPrevs = new Dictionary<string, string>();
            var prevCodes = new List<string>();

            var uniqueCodeWithRecordsLkp = new Dictionary<string, List<(string code, string prevCode, string formularyId, DateTime? createdtimestamp)>>();

            foreach(var rec in details)
            {
                if (!uniqueCodeWithRecordsLkp.ContainsKey(rec.Code))
                    uniqueCodeWithRecordsLkp[rec.Code] = new List<(string code, string prevCode, string formularyId, DateTime? createdtimestamp)>();

                uniqueCodeWithRecordsLkp[rec.Code].Add((rec.Code, rec.PrevCode, rec.FormularyId, rec.Createdtimestamp));

                if (rec.PrevCode.IsNotEmpty())
                {
                    codesForPrevs[rec.PrevCode] = rec.Code;
                    prevCodes.Add(rec.PrevCode);
                }
            }

            //going only two levels - but can go further
            if (prevCodes.IsCollectionValid())
            {
                var prevDetails = repo.ItemsAsReadOnly.Where(rec => prevCodes.Contains(rec.Code) && rec.VersionId == 1)
                    ?.Select(rec => new { rec.Code, rec.PrevCode, rec.FormularyId, rec.VersionId, rec.Createdtimestamp })
                    .OrderBy(rec => rec.Createdtimestamp)
                    .ToList();

                if (prevDetails.IsCollectionValid())
                {
                    foreach(var rec in prevDetails)
                    {
                        if (codesForPrevs.ContainsKey(rec.Code))
                        {
                            var code = codesForPrevs[rec.Code];
                            if (!uniqueCodeWithRecordsLkp.ContainsKey(code))
                                uniqueCodeWithRecordsLkp[code] = new List<(string code, string prevCode, string formularyId, DateTime? createdtimestamp)>();

                            uniqueCodeWithRecordsLkp[code].Add((rec.Code, rec.PrevCode, rec.FormularyId, rec.Createdtimestamp));
                        }
                    }
                }
            }

            Parallel.ForEach(uniqueCodeWithRecordsLkp.Keys, recKey =>
            {
                var val = uniqueCodeWithRecordsLkp[recKey];
                if (val.IsCollectionValid())
                {
                    var ordered = val.OrderBy(r => r.createdtimestamp).ToList();//.Where(r => inputFIds.Contains(r.formularyId))?.ToList();
                    uniqueCodeWithRecordsLkp[recKey] = ordered ?? new List<(string code, string prevCode, string formularyId, DateTime? createdtimestamp)>();
                }
            });

            var codeCountLkp = new Dictionary<string, int>();
            var uniqueCodes = details.Select(rec => rec.Code).Distinct().ToList();//to ensure ordering

            foreach(var uniqueCode in uniqueCodes)
            {
                if (uniqueCodeWithRecordsLkp[uniqueCode].IsCollectionValid())
                {
                    var cnt = 1;
                    foreach (var rec in uniqueCodeWithRecordsLkp[uniqueCode])
                    {
                        if (cnt == 1 && rec.prevCode.IsNotEmpty() && codeCountLkp.ContainsKey(rec.prevCode))
                            cnt = codeCountLkp[rec.prevCode] + 1;
                        results[rec.formularyId] = cnt;
                        codeCountLkp[rec.code] = cnt;
                        cnt++;
                    }
                }
            }
            /*

            var codeCountLkp = new Dictionary<string, int>();

            foreach (var uniqueCode in uniqueCodes)
            {
                var cnt = 1;
                details.Where(rec => rec.Code == uniqueCode)?.OrderBy(rec => rec.Createdtimestamp)
                    .Each(rec =>
                    {
                        if (cnt == 1 && rec.PrevCode.IsNotEmpty() && codeCountLkp.ContainsKey(rec.PrevCode))
                            cnt = codeCountLkp[rec.PrevCode] + 1;
                        results[rec.FormularyId] = cnt;
                        codeCountLkp[rec.Code] = cnt;
                        cnt++;
                    });
            }
            */

            return results;
        }

        #region - Not being used - uses differen logic to get this data
        //public async Task<FormularyDTO[]> GetActiveFormularyDetailRuleBoundByCodeArray(string[] code)
        //{
        //    //get Current object by codes

        //    //Create the complete DTO object for it

        //    //Verify the product type of the current object

        //    //Get Ancestors or descendents or both based on the product type

        //    //1.

        //    //code = code.Trim();

        //    var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

        //    var currentActiveRecsForCode = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && code.Contains(rec.Code)).ToList();

        //    if (!currentActiveRecsForCode.IsCollectionValid()) return null;

        //    currentActiveRecsForCode = currentActiveRecsForCode.Where(rec =>
        //    {
        //        if (string.Compare(rec.ProductType, "amp", true) != 0) return true;

        //        return rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE;
        //    }).ToList();

        //    var FormularyDTOList = new List<FormularyDTO>();

        //    foreach (var activeRecsForCode in currentActiveRecsForCode)
        //    {
        //        var idForCurrentActiveRecsForCode = activeRecsForCode.FormularyVersionId;

        //        var resultsFromDB = repo.GetFormularyDetail(idForCurrentActiveRecsForCode);

        //        var resultObj = resultsFromDB.FirstOrDefault();

        //        if (resultObj != null)
        //        {
        //            var builderType = GetFormBuilderType(resultObj);

        //            var orchestrator = new RuleBoundFormularyBuilderOrchestrator(builderType);

        //            await orchestrator.BuildFormulary(resultObj);

        //            if (builderType.FormularyDTO.FormularyRouteDetails != null && builderType.FormularyDTO.FormularyRouteDetails.Count > 0)
        //            {
        //                builderType.FormularyDTO.FormularyRouteDetails.Clear();
        //            }

        //            if (builderType.FormularyDTO.Detail.LicensedUses != null && builderType.FormularyDTO.Detail.LicensedUses.Count > 0)
        //            {
        //                builderType.FormularyDTO.Detail.LicensedUses.Clear();
        //            }

        //            if (builderType.FormularyDTO.Detail.UnLicensedUses != null && builderType.FormularyDTO.Detail.UnLicensedUses.Count > 0)
        //            {
        //                builderType.FormularyDTO.Detail.UnLicensedUses.Clear();
        //            }

        //            if (builderType.FormularyDTO.FormularyLocalRouteDetails.IsCollectionValid())
        //            {
        //                builderType.FormularyDTO.FormularyLocalRouteDetails.Where(x => x.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL).Each(rec => {
        //                    FormularyRouteDetailDTO formularyRouteDetailDTO = new FormularyRouteDetailDTO();

        //                    formularyRouteDetailDTO.Createdby = rec.Createdby;
        //                    formularyRouteDetailDTO.Createddate = rec.Createddate;
        //                    formularyRouteDetailDTO.FormularyVersionId = rec.FormularyVersionId;
        //                    formularyRouteDetailDTO.RouteCd = rec.RouteCd;
        //                    formularyRouteDetailDTO.RouteDesc = rec.RouteDesc;
        //                    formularyRouteDetailDTO.RouteFieldTypeCd = rec.RouteFieldTypeCd;
        //                    formularyRouteDetailDTO.RouteFieldTypeDesc = rec.RouteFieldTypeDesc;
        //                    formularyRouteDetailDTO.RowId = rec.RowId;
        //                    formularyRouteDetailDTO.Source = rec.Source;
        //                    formularyRouteDetailDTO.Updatedby = rec.Updatedby;
        //                    formularyRouteDetailDTO.Updateddate = rec.Updateddate;

        //                    if (builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
        //                    {
        //                        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
        //                    }
        //                    else
        //                    {
        //                        builderType.FormularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

        //                        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
        //                    }

        //                });

        //                builderType.FormularyDTO.FormularyLocalRouteDetails.Where(x => x.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED).Each(rec => {
        //                    FormularyRouteDetailDTO formularyRouteDetailDTO = new FormularyRouteDetailDTO();

        //                    formularyRouteDetailDTO.Createdby = rec.Createdby;
        //                    formularyRouteDetailDTO.Createddate = rec.Createddate;
        //                    formularyRouteDetailDTO.FormularyVersionId = rec.FormularyVersionId;
        //                    formularyRouteDetailDTO.RouteCd = rec.RouteCd;
        //                    formularyRouteDetailDTO.RouteDesc = rec.RouteDesc;
        //                    formularyRouteDetailDTO.RouteFieldTypeCd = rec.RouteFieldTypeCd;
        //                    formularyRouteDetailDTO.RouteFieldTypeDesc = rec.RouteFieldTypeDesc;
        //                    formularyRouteDetailDTO.RowId = rec.RowId;
        //                    formularyRouteDetailDTO.Source = rec.Source;
        //                    formularyRouteDetailDTO.Updatedby = rec.Updatedby;
        //                    formularyRouteDetailDTO.Updateddate = rec.Updateddate;

        //                    if (builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
        //                    {
        //                        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
        //                    }
        //                    else
        //                    {
        //                        builderType.FormularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

        //                        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
        //                    }

        //                });
        //            }

        //            builderType.FormularyDTO.Detail.LocalLicensedUses.Each(rec => {
        //                FormularyLookupItemDTO formularyLookupItemDTO = new FormularyLookupItemDTO();

        //                formularyLookupItemDTO.AdditionalProperties = rec.AdditionalProperties;
        //                formularyLookupItemDTO.Cd = rec.Cd;
        //                formularyLookupItemDTO.Desc = rec.Desc;
        //                formularyLookupItemDTO.IsDefault = rec.IsDefault;
        //                formularyLookupItemDTO.Recordstatus = rec.Recordstatus;
        //                formularyLookupItemDTO.Source = rec.Source;
        //                formularyLookupItemDTO.Type = rec.Type;

        //                if (builderType.FormularyDTO.Detail.LicensedUses.IsCollectionValid())
        //                {
        //                    builderType.FormularyDTO.Detail.LicensedUses.Add(formularyLookupItemDTO);
        //                }
        //                else
        //                {
        //                    builderType.FormularyDTO.Detail.LicensedUses = new List<FormularyLookupItemDTO>();

        //                    builderType.FormularyDTO.Detail.LicensedUses.Add(formularyLookupItemDTO);
        //                }
        //            });

        //            builderType.FormularyDTO.Detail.LocalUnLicensedUses.Each(rec => {
        //                FormularyLookupItemDTO formularyLookupItemDTO = new FormularyLookupItemDTO();

        //                formularyLookupItemDTO.AdditionalProperties = rec.AdditionalProperties;
        //                formularyLookupItemDTO.Cd = rec.Cd;
        //                formularyLookupItemDTO.Desc = rec.Desc;
        //                formularyLookupItemDTO.IsDefault = rec.IsDefault;
        //                formularyLookupItemDTO.Recordstatus = rec.Recordstatus;
        //                formularyLookupItemDTO.Source = rec.Source;
        //                formularyLookupItemDTO.Type = rec.Type;

        //                if (builderType.FormularyDTO.Detail.UnLicensedUses.IsCollectionValid())
        //                {
        //                    builderType.FormularyDTO.Detail.UnLicensedUses.Add(formularyLookupItemDTO);
        //                }
        //                else
        //                {
        //                    builderType.FormularyDTO.Detail.UnLicensedUses = new List<FormularyLookupItemDTO>();

        //                    builderType.FormularyDTO.Detail.UnLicensedUses.Add(formularyLookupItemDTO);
        //                }

        //            });

        //            if (builderType.FormularyDTO.FormularyLocalRouteDetails != null && builderType.FormularyDTO.FormularyLocalRouteDetails.Count > 0)
        //            {
        //                builderType.FormularyDTO.FormularyLocalRouteDetails.Clear();
        //            }

        //            if (builderType.FormularyDTO.Detail.LocalLicensedUses != null && builderType.FormularyDTO.Detail.LocalLicensedUses.Count > 0)
        //            {
        //                builderType.FormularyDTO.Detail.LocalLicensedUses.Clear();
        //            }

        //            if (builderType.FormularyDTO.Detail.LocalUnLicensedUses != null && builderType.FormularyDTO.Detail.LocalUnLicensedUses.Count > 0)
        //            {
        //                builderType.FormularyDTO.Detail.LocalUnLicensedUses.Clear();
        //            }

        //            FormularyDTOList.Add(builderType.FormularyDTO);
        //        }                
        //    }

        //    return FormularyDTOList.ToArray();
        //}
        #endregion

        //private async Task<ConcurrentDictionary<string, List<FormularyHeader>>> GetDescendentsForCodesAsLkp(List<string> codes, bool onlyBasicDetail = false)
        private async Task<ConcurrentDictionary<string, List<FormularyHeader>>> GetDescendentsForFormularyIdsAsLkp(List<string> formularyIds, bool onlyBasicDetail = false, bool onlyActiveRecs = false)
        {
            return await Task.Run<ConcurrentDictionary<string, List<FormularyHeader>>>(() =>
            {
                if (!formularyIds.IsCollectionValid()) return null;

                var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

                var basicSearchRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

                //var descendents = await basicSearchRepo.GetFormularyDescendentForCodes(codes.ToArray(), true);
                //var uniqueIds = descendents.Select(rec => rec.FormularyVersionId)?.Distinct()?.ToList();

                var descendentsLkp = repo.GetDescendentFormularyIdsForFormularyIdsAsLookup(formularyIds, onlyActiveRecs: onlyActiveRecs);

                var descendentFIds = new List<string>();
                descendentFIds.AddRange(formularyIds);//the current level is included by default
                descendentFIds = descendentFIds.Distinct().ToList();

                if (descendentsLkp.IsCollectionValid())
                    descendentsLkp.Values.Each(rec => descendentFIds.AddRange(rec));

                var uniqueIds = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && descendentFIds.Contains(rec.FormularyId))?.Select(rec => rec.FormularyVersionId).Distinct().ToList();

                List<FormularyHeader> descendentsDetails = null;

                if (onlyBasicDetail)
                {
                    descendentsDetails = repo.GetFormularyBasicDetailListForIds(uniqueIds).ToList();
                }
                else
                {
                    //Get full details for these descendents
                    descendentsDetails = repo.GetFormularyListForIds(uniqueIds).ToList();
                }

                if (!descendentsDetails.IsCollectionValid()) return null;

                var descendentListForCode = new ConcurrentDictionary<string, List<FormularyHeader>>();

                var descendentsParentLookup = GetDescendentsLookp(descendentsDetails);

                formularyIds?.Distinct()?.AsParallel()?.Each(rec =>
                {
                    var childList = new ConcurrentBag<FormularyHeader>();
                    FillNestedDescendentsToList(rec, childList, descendentsParentLookup);
                    descendentListForCode[rec] = childList.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)?.ToList();
                });

                return descendentListForCode;
            });
        }

        private ConcurrentDictionary<string, List<FormularyHeader>> GetDescendentsLookp(List<FormularyHeader> descendentsDetails)
        {
            var descendentsParentLookup = new ConcurrentDictionary<string, List<FormularyHeader>>();

            #region old logic - ref only
            //descendentsDetails.AsParallel().Each(rec =>
            //{
            //    if (rec.ParentCode.IsNotEmpty())
            //    {
            //        if (descendentsParentLookup.ContainsKey(rec.ParentCode))
            //        {
            //            descendentsParentLookup[rec.ParentCode].Add(rec);
            //        }
            //        else
            //        {
            //            descendentsParentLookup[rec.ParentCode] = new List<FormularyHeader> { rec };
            //        }
            //    }
            //});
            #endregion old logic - ref only

            descendentsDetails.AsParallel().Each(rec =>
            {
                if (rec.ParentFormularyId.IsNotEmpty())
                {
                    if (descendentsParentLookup.ContainsKey(rec.ParentFormularyId))
                    {
                        descendentsParentLookup[rec.ParentFormularyId].Add(rec);
                    }
                    else
                    {
                        descendentsParentLookup[rec.ParentFormularyId] = new List<FormularyHeader> { rec };
                    }
                }
            });

            return descendentsParentLookup;
        }

        private void FillNestedDescendentsToList(string parentFormularyId, ConcurrentBag<FormularyHeader> childFormularies, ConcurrentDictionary<string, List<FormularyHeader>> descendentsParentLookup)
        {
            var children = descendentsParentLookup.ContainsKey(parentFormularyId) ? descendentsParentLookup[parentFormularyId] : null;

            if (!children.IsCollectionValid()) return;

            children.Each(rec => childFormularies.Add(rec));

            children.AsParallel().Each(rec =>
            {
                if (rec.FormularyId.IsNotEmpty())
                    FillNestedDescendentsToList(rec.FormularyId, childFormularies, descendentsParentLookup);
            });
        }

        /// <summary>
        /// The codes passed are mapped to the 'Active' tree based on the 'Acive' AMPs
        /// and the 'Active' tree is used to bind the rules for aggregation.
        /// </summary>
        /// <param name="codes"></param>
        /// <param name="fromCache"></param>
        /// <returns></returns>
        public async Task<List<ActiveFormularyBasicDTO>> GetActiveFormularyBasicDetailRuleBoundByCodes(List<string> codes, bool fromCache = true, bool includeInvalid = false)
        {
            //var timer = new Stopwatch();
            //timer.Start();
            //Log.Logger.Error("Info: Fetching from formulary detail from DB");

            if (!codes.IsCollectionValid()) return null;

            //Codes are used to get the 'Active' tree only
            var activeFormularyIdsForCodesAsLookup = await codes.GetActiveFormularyIdForCode(_provider);

            if (!activeFormularyIdsForCodesAsLookup.IsCollectionValid()) return null;

            var activeFormularyIds = activeFormularyIdsForCodesAsLookup.Values.Where(rec => rec.IsNotEmpty())?.ToList();

            if (!activeFormularyIds.IsCollectionValid()) return null;

            var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var currentActiveRecsForFormularyIdsQry = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && activeFormularyIds.Contains(rec.FormularyId));

            if (includeInvalid == false)
                currentActiveRecsForFormularyIdsQry = currentActiveRecsForFormularyIdsQry.Where(rec => (rec.IsDmdInvalid == null || rec.IsDmdInvalid == false));

            var currentActiveRecsForFormularyIds = currentActiveRecsForFormularyIdsQry.ToList();

            if (!currentActiveRecsForFormularyIds.IsCollectionValid()) return null;

            currentActiveRecsForFormularyIds = currentActiveRecsForFormularyIds.Where(rec =>
            {
                //EPMA-2702 - Recordstatus will be there for VTM and VMPs also and that need to be checked
                //if (string.Compare(rec.ProductType, "amp", true) != 0) return true;

                return rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE;
            }).ToList();

            if (!currentActiveRecsForFormularyIds.IsCollectionValid()) return null;

            var idForCurrentActiveRecsForForFormularyIds = currentActiveRecsForFormularyIds.Select(rec => rec.FormularyVersionId).ToList();

            var resultsFromDB = repo.GetFormularyListForIds(idForCurrentActiveRecsForForFormularyIds).ToList();

            if (resultsFromDB == null) return null;

            var activeRecFormularyIds = currentActiveRecsForFormularyIds.Select(rec => rec.FormularyId)?.ToList();

            var descendentListForFormularyIds = await GetDescendentsForFormularyIdsAsLkp(activeRecFormularyIds, true);

            var resultsList = await BuildFormularyBasicDetailByRule(resultsFromDB, descendentListForFormularyIds);

            //timer.Stop();
            //var timeTaken = timer.Elapsed;
            //var msg = $"Info: Completed fetching from db. Time taken: {timeTaken.ToString(@"hh\:mm\:ss\.fff")}";

            return resultsList.ToList();

            #region Old -For Reference
            /*
            var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var currentActiveRecsForCodes = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && codes.Contains(rec.Code)).ToList();

            if (!currentActiveRecsForCodes.IsCollectionValid()) return null;

            currentActiveRecsForCodes = currentActiveRecsForCodes.Where(rec =>
            {
                //EPMA-2702 - Recordstatus will be there for VTM and VMPs also and that need to be checked
                //if (string.Compare(rec.ProductType, "amp", true) != 0) return true;

                return rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE;
            }).ToList();

            if (!currentActiveRecsForCodes.IsCollectionValid()) return null;

            var idForCurrentActiveRecsForCodes = currentActiveRecsForCodes.Select(rec => rec.FormularyVersionId).ToList();

            var resultsFromDB = repo.GetFormularyListForIds(idForCurrentActiveRecsForCodes).ToList();

            if (resultsFromDB == null) return null;

            var activeRecCodes = currentActiveRecsForCodes.Select(rec => rec.Code)?.ToList();

            var descendentListForCode = await GetDescendentsForCodesAsLkp(activeRecCodes, true);

            var resultsList = await BuildFormularyBasicDetailByRule(resultsFromDB, descendentListForCode);
            
            //timer.Stop();
            //var timeTaken = timer.Elapsed;
            //var msg = $"Info: Completed fetching from db. Time taken: {timeTaken.ToString(@"hh\:mm\:ss\.fff")}";

            return resultsList.ToList();
            */
            #endregion Old -For Reference
        }

        /// <summary>
        /// The codes passed are mapped to the 'Active' tree based on the 'Acive' AMPs
        /// and the 'Active' tree is used to bind the rules for aggregation.
        /// </summary>
        /// <param name="codes"></param>
        /// <param name="fromCache"></param>
        /// <returns></returns>
        public async Task<List<FormularyDTO>> GetActiveFormularyDetailRuleBoundByCodes(List<string> codes, bool fromCache = true, bool includeInvalid = false)
        {
            if (!codes.IsCollectionValid()) return null;
            // Log.Logger.Error("Info: Stated fetching from formulary detail");
            // Log.Logger.Error($"Info: Value of from Cache: {fromCache}");

            if (fromCache)
            {
                //check if can ping server
                bool canPing = true;// await CacheService.ServerPingAsync();To be uncommented

                if(canPing)
                {
                    var timer = new Stopwatch();
                    timer.Start();

                    //Log.Logger.Error("Info: Stated fetching from cache");
                    //ConcurrentBag<FormularyDTO> formularyDTOs = new ConcurrentBag<FormularyDTO>();
                    var formularyDTOs = new List<FormularyDTO>();

                    //foreach (var code in codes)
                    //Parallel.ForEach(codes, code =>
                    //{
                    //    var dto = CacheService.Get<FormularyDTO>($"{CacheKeys.ACTIVE_FORMULARY}{code}");

                    //    if (dto != null)
                    //        formularyDTOs.Add(dto);
                    //});

                    var cacheKeys = codes.Select(rec => $"{CacheKeys.ACTIVE_FORMULARY}{rec}").ToList();

                    formularyDTOs = await CacheService.GetAsync<FormularyDTO>(cacheKeys);

                    timer.Stop();
                    TimeSpan timeTaken = timer.Elapsed;
                    var msg = $"Info: Completed fetching from cache. Time taken: {timeTaken.ToString(@"hh\:mm\:ss\.fff")}";
                    Log.Logger.Error(msg);

                    if (formularyDTOs.IsCollectionValid())
                        return formularyDTOs.ToList();
                }
            }

            //Log.Logger.Error("Info: Fetching from formulary detail from DB");

            //Codes are used to get the 'Active' tree only
            var activeFormularyIdsForCodesAsLookup = await codes.GetActiveFormularyIdForCode(_provider);

            if (!activeFormularyIdsForCodesAsLookup.IsCollectionValid()) return null;

            var activeFormularyIds = activeFormularyIdsForCodesAsLookup.Values.Where(rec => rec.IsNotEmpty())?.ToList();

            if (!activeFormularyIds.IsCollectionValid()) return null;

            var repo = _provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var currentActiveRecsForFormularyIdsQry = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && activeFormularyIds.Contains(rec.FormularyId));

            if (includeInvalid == false)
                currentActiveRecsForFormularyIdsQry = currentActiveRecsForFormularyIdsQry.Where(rec => (rec.IsDmdInvalid == null || rec.IsDmdInvalid == false));

            var currentActiveRecsForFormularyIds = currentActiveRecsForFormularyIdsQry.ToList();

            if (!currentActiveRecsForFormularyIds.IsCollectionValid()) return null;

            currentActiveRecsForFormularyIds = currentActiveRecsForFormularyIds.Where(rec =>
            {
                //EPMA-2702 - Recordstatus will be there for VTM and VMPs also and that need to be checked
                //if (string.Compare(rec.ProductType, "amp", true) != 0) return true;

                return rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE;
            }).ToList();

            if (!currentActiveRecsForFormularyIds.IsCollectionValid()) return null;

            var idForCurrentActiveRecsForForFormularyIds = currentActiveRecsForFormularyIds.Select(rec => rec.FormularyVersionId).ToList();

            var resultsFromDB = repo.GetFormularyListForIds(idForCurrentActiveRecsForForFormularyIds).ToList();

            if (resultsFromDB == null) return null;

            var activeRecFormularyIds = currentActiveRecsForFormularyIds.Select(rec => rec.FormularyId)?.ToList();

            var descendentListForFormularyIds = await GetDescendentsForFormularyIdsAsLkp(activeRecFormularyIds);

            var resultsList = await BuildFormularyDetailByRule(resultsFromDB, descendentListForFormularyIds);

            return resultsList;

            #region old code - ref only
            /*
            var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var currentActiveRecsForCodes = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && codes.Contains(rec.Code)).ToList();

            if (!currentActiveRecsForCodes.IsCollectionValid()) return null;

            currentActiveRecsForCodes = currentActiveRecsForCodes.Where(rec =>
            {
                //EPMA-2702 - Recordstatus will be there for VTM and VMPs also and that need to be checked
                //if (string.Compare(rec.ProductType, "amp", true) != 0) return true;

                return rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE;
            }).ToList();

            if (!currentActiveRecsForCodes.IsCollectionValid()) return null;

            var idForCurrentActiveRecsForCodes = currentActiveRecsForCodes.Select(rec => rec.FormularyVersionId).ToList();

            var resultsFromDB = repo.GetFormularyListForIds(idForCurrentActiveRecsForCodes).ToList();

            if (resultsFromDB == null) return null;

            var activeRecCodes = currentActiveRecsForCodes.Select(rec => rec.Code)?.ToList();

            var descendentListForCode = await GetDescendentsForCodesAsLkp(activeRecCodes);

            var resultsList = await BuildFormularyDetailByRule(resultsFromDB, descendentListForCode);

            return resultsList;
            */
            #endregion old code - ref only
        }

        //private async Task<List<ActiveFormularyBasicDTO>> BuildFormularyBasicDetailByRule(List<FormularyHeader> resultsFromDB, ConcurrentDictionary<string, List<FormularyHeader>> descendentListForCode)
        private async Task<List<ActiveFormularyBasicDTO>> BuildFormularyBasicDetailByRule(List<FormularyHeader> resultsFromDB, ConcurrentDictionary<string, List<FormularyHeader>> descendentListForFormularyId)
        {
            var resultsList = new ConcurrentBag<ActiveFormularyBasicDTO>();

            foreach (var rec in resultsFromDB)
            {

                //var descendents = descendentListForCode.ContainsKey(rec.Code) ? descendentListForCode[rec.Code] : null;
                var descendents = descendentListForFormularyId.ContainsKey(rec.FormularyId) ? descendentListForFormularyId[rec.FormularyId] : null;
                var builderType = GetFormBuilderTypeForActiveFormularyBasic(rec);

                var orchestrator = new RuleBoundFormularyBuilderOrchestrator(builderType);

                await orchestrator.BuildBasicFormulary(rec, null, descendents);

                //To be refactored

                if (builderType.ActiveFormularyBasicDTO.Detail.LicensedUses.IsCollectionValid())
                {
                    builderType.ActiveFormularyBasicDTO.Detail.LicensedUses.Clear();
                }

                if (builderType.ActiveFormularyBasicDTO.Detail.UnLicensedUses.IsCollectionValid())
                {
                    builderType.ActiveFormularyBasicDTO.Detail.UnLicensedUses.Clear();
                }


                builderType.ActiveFormularyBasicDTO.Detail.LocalLicensedUses.Each(rec =>
                {
                    FormularyLookupItemDTO formularyLookupItemDTO = new FormularyLookupItemDTO();

                    formularyLookupItemDTO.AdditionalProperties = rec.AdditionalProperties;
                    formularyLookupItemDTO.Cd = rec.Cd;
                    formularyLookupItemDTO.Desc = rec.Desc;
                    formularyLookupItemDTO.IsDefault = rec.IsDefault;
                    formularyLookupItemDTO.Recordstatus = rec.Recordstatus;
                    formularyLookupItemDTO.Source = rec.Source;
                    formularyLookupItemDTO.Type = rec.Type;

                    if (builderType.ActiveFormularyBasicDTO.Detail.LicensedUses.IsCollectionValid())
                    {
                        builderType.ActiveFormularyBasicDTO.Detail.LicensedUses.Add(formularyLookupItemDTO);
                    }
                    else
                    {
                        builderType.ActiveFormularyBasicDTO.Detail.LicensedUses = new List<FormularyLookupItemDTO>();

                        builderType.ActiveFormularyBasicDTO.Detail.LicensedUses.Add(formularyLookupItemDTO);
                    }
                });

                builderType.ActiveFormularyBasicDTO.Detail.LocalUnLicensedUses.Each(rec =>
                {
                    FormularyLookupItemDTO formularyLookupItemDTO = new FormularyLookupItemDTO();

                    formularyLookupItemDTO.AdditionalProperties = rec.AdditionalProperties;
                    formularyLookupItemDTO.Cd = rec.Cd;
                    formularyLookupItemDTO.Desc = rec.Desc;
                    formularyLookupItemDTO.IsDefault = rec.IsDefault;
                    formularyLookupItemDTO.Recordstatus = rec.Recordstatus;
                    formularyLookupItemDTO.Source = rec.Source;
                    formularyLookupItemDTO.Type = rec.Type;

                    if (builderType.ActiveFormularyBasicDTO.Detail.UnLicensedUses.IsCollectionValid())
                    {
                        builderType.ActiveFormularyBasicDTO.Detail.UnLicensedUses.Add(formularyLookupItemDTO);
                    }
                    else
                    {
                        builderType.ActiveFormularyBasicDTO.Detail.UnLicensedUses = new List<FormularyLookupItemDTO>();

                        builderType.ActiveFormularyBasicDTO.Detail.UnLicensedUses.Add(formularyLookupItemDTO);
                    }
                });

                if (builderType.ActiveFormularyBasicDTO.Detail.LocalLicensedUses.IsCollectionValid())
                {
                    builderType.ActiveFormularyBasicDTO.Detail.LocalLicensedUses.Clear();
                }

                if (builderType.ActiveFormularyBasicDTO.Detail.LocalUnLicensedUses.IsCollectionValid())
                {
                    builderType.ActiveFormularyBasicDTO.Detail.LocalUnLicensedUses.Clear();
                }

                if (builderType.ActiveFormularyBasicDTO.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)
                {
                    resultsList.Add(builderType.ActiveFormularyBasicDTO);
                }
            }

            return resultsList.ToList();
        }

        //private async Task<List<FormularyDTO>> BuildFormularyDetailByRule(List<FormularyHeader> resultsFromDB, ConcurrentDictionary<string, List<FormularyHeader>> descendentListForCode, bool getAllAdditionalCodes = false, bool ignoreRecStsCd = false)
        private async Task<List<FormularyDTO>> BuildFormularyDetailByRule(List<FormularyHeader> resultsFromDB, ConcurrentDictionary<string, List<FormularyHeader>> descendentListForFormularyId, bool getAllAdditionalCodes = false, bool ignoreRecStsCd = false)
        {
            var resultsList = new List<FormularyDTO>();

            foreach (var rec in resultsFromDB)
            {
                //var descendents = descendentListForCode.ContainsKey(rec.Code) ? descendentListForCode[rec.Code] : null;
                var descendents = descendentListForFormularyId.ContainsKey(rec.FormularyId) ? descendentListForFormularyId[rec.FormularyId] : null;
                var builderType = GetFormBuilderType(rec);

                var orchestrator = new RuleBoundFormularyBuilderOrchestrator(builderType);

                await orchestrator.BuildFormulary(rec, null, descendents, getAllAdditionalCodes);

                if (ignoreRecStsCd || builderType.FormularyDTO.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)
                {
                    ReManipulateBuildResult(builderType);
                    //To be refactored
                    resultsList.Add(builderType.FormularyDTO);
                }
            }

            return resultsList;
        }

        private void ReManipulateBuildResult(RuleBoundBaseFormularyBuilder builderType)
        {
            if (builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
            {
                builderType.FormularyDTO.FormularyRouteDetails.Clear();
            }

            if (builderType.FormularyDTO.Detail.LicensedUses.IsCollectionValid())
            {
                builderType.FormularyDTO.Detail.LicensedUses.Clear();
            }

            if (builderType.FormularyDTO.Detail.UnLicensedUses.IsCollectionValid())
            {
                builderType.FormularyDTO.Detail.UnLicensedUses.Clear();
            }

            if (builderType.FormularyDTO.FormularyLocalRouteDetails.IsCollectionValid())
            {
                var localRouteDetailDTOs = builderType.FormularyDTO.FormularyLocalRouteDetails.Where(x => x.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL).ToList();

                if (localRouteDetailDTOs.IsCollectionValid())
                {
                    var formularyRouteDetailDTOs = _mapper.Map<List<FormularyRouteDetailDTO>>(localRouteDetailDTOs);

                    if (!builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
                        builderType.FormularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

                    builderType.FormularyDTO.FormularyRouteDetails.AddRange(formularyRouteDetailDTOs);
                }

                var localUnLicensedRouteDTOs = builderType.FormularyDTO.FormularyLocalRouteDetails.Where(x => x.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED).ToList();

                if (localUnLicensedRouteDTOs.IsCollectionValid())
                {
                    var unlicensedRouteDTOs = _mapper.Map<List<FormularyRouteDetailDTO>>(localUnLicensedRouteDTOs);

                    if (!builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
                        builderType.FormularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

                    builderType.FormularyDTO.FormularyRouteDetails.AddRange(unlicensedRouteDTOs);
                }

                #region old code - ref only

                //builderType.FormularyDTO.FormularyLocalRouteDetails.Where(x => x.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL).Each(rec =>
                //{
                //FormularyRouteDetailDTO formularyRouteDetailDTO = new FormularyRouteDetailDTO();

                //formularyRouteDetailDTO.Createdby = rec.Createdby;
                //formularyRouteDetailDTO.Createddate = rec.Createddate;
                //formularyRouteDetailDTO.FormularyVersionId = rec.FormularyVersionId;
                //formularyRouteDetailDTO.RouteCd = rec.RouteCd;
                //formularyRouteDetailDTO.RouteDesc = rec.RouteDesc;
                //formularyRouteDetailDTO.RouteFieldTypeCd = rec.RouteFieldTypeCd;
                //formularyRouteDetailDTO.RouteFieldTypeDesc = rec.RouteFieldTypeDesc;
                //formularyRouteDetailDTO.RowId = rec.RowId;
                //formularyRouteDetailDTO.Source = rec.Source;
                //formularyRouteDetailDTO.Updatedby = rec.Updatedby;
                //formularyRouteDetailDTO.Updateddate = rec.Updateddate;

                //if (builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
                //{
                //    builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
                //}
                //else
                //{
                //    builderType.FormularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

                //    builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
                //}

                //});

                //builderType.FormularyDTO.FormularyLocalRouteDetails.Where(x => x.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED).Each(rec =>
                //{
                //    FormularyRouteDetailDTO formularyRouteDetailDTO = new FormularyRouteDetailDTO();

                //    formularyRouteDetailDTO.Createdby = rec.Createdby;
                //    formularyRouteDetailDTO.Createddate = rec.Createddate;
                //    formularyRouteDetailDTO.FormularyVersionId = rec.FormularyVersionId;
                //    formularyRouteDetailDTO.RouteCd = rec.RouteCd;
                //    formularyRouteDetailDTO.RouteDesc = rec.RouteDesc;
                //    formularyRouteDetailDTO.RouteFieldTypeCd = rec.RouteFieldTypeCd;
                //    formularyRouteDetailDTO.RouteFieldTypeDesc = rec.RouteFieldTypeDesc;
                //    formularyRouteDetailDTO.RowId = rec.RowId;
                //    formularyRouteDetailDTO.Source = rec.Source;
                //    formularyRouteDetailDTO.Updatedby = rec.Updatedby;
                //    formularyRouteDetailDTO.Updateddate = rec.Updateddate;

                //    if (builderType.FormularyDTO.FormularyRouteDetails.IsCollectionValid())
                //    {
                //        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
                //    }
                //    else
                //    {
                //        builderType.FormularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

                //        builderType.FormularyDTO.FormularyRouteDetails.Add(formularyRouteDetailDTO);
                //    }

                //});
                #endregion old code - ref only
            }

            if (builderType.FormularyDTO.Detail.LocalLicensedUses.IsCollectionValid())
            {
                var localLicensedUses = _mapper.Map<List<FormularyLookupItemDTO>>(builderType.FormularyDTO.Detail.LocalLicensedUses);
                if (!builderType.FormularyDTO.Detail.LicensedUses.IsCollectionValid())
                    builderType.FormularyDTO.Detail.LicensedUses = new List<FormularyLookupItemDTO>();

                builderType.FormularyDTO.Detail.LicensedUses.AddRange(localLicensedUses);
            }

            if (builderType.FormularyDTO.Detail.LocalUnLicensedUses.IsCollectionValid())
            {
                var localUnLicensedUses = _mapper.Map<List<FormularyLookupItemDTO>>(builderType.FormularyDTO.Detail.LocalUnLicensedUses);
                if (!builderType.FormularyDTO.Detail.UnLicensedUses.IsCollectionValid())
                    builderType.FormularyDTO.Detail.UnLicensedUses = new List<FormularyLookupItemDTO>();

                builderType.FormularyDTO.Detail.UnLicensedUses.AddRange(localUnLicensedUses);
            }

            #region old code - ref only
            //builderType.FormularyDTO.Detail.LocalLicensedUses.Each(rec =>
            //{
            //    FormularyLookupItemDTO formularyLookupItemDTO = new FormularyLookupItemDTO();

            //    formularyLookupItemDTO.AdditionalProperties = rec.AdditionalProperties;
            //    formularyLookupItemDTO.Cd = rec.Cd;
            //    formularyLookupItemDTO.Desc = rec.Desc;
            //    formularyLookupItemDTO.IsDefault = rec.IsDefault;
            //    formularyLookupItemDTO.Recordstatus = rec.Recordstatus;
            //    formularyLookupItemDTO.Source = rec.Source;
            //    formularyLookupItemDTO.Type = rec.Type;

            //    if (builderType.FormularyDTO.Detail.LicensedUses.IsCollectionValid())
            //    {
            //        builderType.FormularyDTO.Detail.LicensedUses.Add(formularyLookupItemDTO);
            //    }
            //    else
            //    {
            //        builderType.FormularyDTO.Detail.LicensedUses = new List<FormularyLookupItemDTO>();

            //        builderType.FormularyDTO.Detail.LicensedUses.Add(formularyLookupItemDTO);
            //    }
            //});

            //builderType.FormularyDTO.Detail.LocalUnLicensedUses.Each(rec =>
            //{
            //    FormularyLookupItemDTO formularyLookupItemDTO = new FormularyLookupItemDTO();

            //    formularyLookupItemDTO.AdditionalProperties = rec.AdditionalProperties;
            //    formularyLookupItemDTO.Cd = rec.Cd;
            //    formularyLookupItemDTO.Desc = rec.Desc;
            //    formularyLookupItemDTO.IsDefault = rec.IsDefault;
            //    formularyLookupItemDTO.Recordstatus = rec.Recordstatus;
            //    formularyLookupItemDTO.Source = rec.Source;
            //    formularyLookupItemDTO.Type = rec.Type;

            //    if (builderType.FormularyDTO.Detail.UnLicensedUses.IsCollectionValid())
            //    {
            //        builderType.FormularyDTO.Detail.UnLicensedUses.Add(formularyLookupItemDTO);
            //    }
            //    else
            //    {
            //        builderType.FormularyDTO.Detail.UnLicensedUses = new List<FormularyLookupItemDTO>();

            //        builderType.FormularyDTO.Detail.UnLicensedUses.Add(formularyLookupItemDTO);
            //    }

            //});
            #endregion old code - ref only

            if (builderType.FormularyDTO.FormularyLocalRouteDetails.IsCollectionValid())
            {
                builderType.FormularyDTO.FormularyLocalRouteDetails.Clear();
            }

            if (builderType.FormularyDTO.Detail.LocalLicensedUses.IsCollectionValid())
            {
                builderType.FormularyDTO.Detail.LocalLicensedUses.Clear();
            }

            if (builderType.FormularyDTO.Detail.LocalUnLicensedUses.IsCollectionValid())
            {
                builderType.FormularyDTO.Detail.LocalUnLicensedUses.Clear();
            }
        }

        private RuleBoundBaseFormularyBuilder GetFormBuilderTypeForActiveFormularyBasic(FormularyHeader resultObj)
        {
            if (string.Compare(resultObj.ProductType, "vtm", true) == 0)
                return new RuleBoundVTMBasicActiveFormularyBuilder(this._provider);

            if (string.Compare(resultObj.ProductType, "vmp", true) == 0)
                return new RuleBoundVMPBasicActiveFormularyBuilder(this._provider);

            if (string.Compare(resultObj.ProductType, "amp", true) == 0)
                return new RuleBoundAMPBasicActiveFormularyBuilder(this._provider);

            return new RuleBoundNullFormularyBuilder(this._provider);
        }

        private RuleBoundBaseFormularyBuilder GetFormBuilderType(FormularyHeader resultObj)
        {
            if (string.Compare(resultObj.ProductType, "vtm", true) == 0)
                return new RuleBoundVTMFormularyBuilder(this._provider);

            if (string.Compare(resultObj.ProductType, "vmp", true) == 0)
                return new RuleBoundVMPFormularyBuilder(this._provider);

            if (string.Compare(resultObj.ProductType, "amp", true) == 0)
                return new RuleBoundAMPFormularyBuilder(this._provider);

            return new RuleBoundNullFormularyBuilder(this._provider);
        }
    }
}
