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
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Extensions;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers.Util;
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;
using Interneuron.Terminology.BackgroundTaskService.Model.Search;
using Interneuron.Terminology.BackgroundTaskService.Repository;
using Microsoft.Diagnostics.Tracing;
using Newtonsoft.Json;
using System.Linq;
using System.Runtime.InteropServices;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers
{
    public class FormularyPostImportHandlerResponse
    {
        public List<string> Errors { get; set; }

        public short Status { get; set; }
    }

    public class FormularyPostImportProcessHandler : FormularyImportBaseHandler
    {
        private TerminologyAPIService _terminologyAPIService;

        private readonly FormularyImportHandler _formularyImportHandler;
        private readonly FormularyUtil _formularyUtil;
        private readonly IServiceProvider _serviceProvider;

        //private IMapper _mapper;

        public FormularyPostImportProcessHandler(TerminologyAPIService terminologyAPIService, IMapper mapper, FormularyImportHandler formularyImportHandler, FormularyUtil formularyUtil, IServiceProvider serviceProvider)
        {
            _terminologyAPIService = terminologyAPIService;

            _formularyImportHandler = formularyImportHandler;
            _formularyUtil = formularyUtil;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokePostImportProcessForCodes(List<string> codes, Action<List<string>> onComplete = null)
        {
            var allCodes = new List<string>();

            if (!codes.IsCollectionValid()) return;

            await BuildHierarchyForNewNodes(codes);

            //MMC-477 - FormularyId changes
            //await ArchiveOlderDraftsIfExists(codes);
            //await FindAndCreateMissingAMPDraftsForParents(codes);

            allCodes.AddRange(codes);

            //MMC-477 - FormularyId changes - allcodes has all the records in the hierarchy
            //var (scope, unitOfWork) = GetUoWInNewScope();

            //var ancestors = await unitOfWork.FormularyBasicResultsFormularyRepository.GetFormularyAncestorForCodes(codes.ToArray());

            //var descendents = await unitOfWork.FormularyBasicResultsFormularyRepository.GetFormularyDescendentForCodes(codes.ToArray());

            //DisposeUoWWithScope(scope, unitOfWork);

            //if (ancestors.IsCollectionValid())
            //{
            //    var ancestorUniqueCodes = ancestors.Select(rec => rec.Code).Distinct().ToList();
            //    allCodes.AddRange(ancestorUniqueCodes);
            //}

            //if (descendents.IsCollectionValid())
            //{
            //    var descendentUniqueCodes = descendents.Select(rec => rec.Code).Distinct().ToList();
            //    allCodes.AddRange(descendentUniqueCodes);
            //}

            var allUniqueCodes = allCodes.Distinct().ToList();

            //should act on the recenly ingested data. Bring recent data for these codes.
            var codesFormularyIdsLookup = await GetLatestFormularyIdsForCodes(allUniqueCodes);

            //Create draft of AMPs if it does not exist

            var allWithDetails = GetLatestFormulariesFewHeaderOnlyByCodes(codesFormularyIdsLookup);// GetLatestFormulariesByCodes(allUniqueCodes);

            var vtms = allWithDetails.Where(rec => string.Compare(rec.ProductType, "vtm", true) == 0).ToList();

            var vmps = allWithDetails.Where(rec => string.Compare(rec.ProductType, "vmp", true) == 0).ToList();

            Dictionary<string, List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)>> parentWithDraftChildrenLkp = GetParentWithDraftChildrenAsLkp(allWithDetails);

            if (vtms.IsCollectionValid())
                await UpdateVtmRelatedData(vtms, parentWithDraftChildrenLkp);

            if (vmps.IsCollectionValid())
                UpdateVMPRelatedData(vmps, parentWithDraftChildrenLkp);

            await UpdateDMDDeletedStatusToFormularyIfDeleted();

            //MMC-477-FormularyId changes - No needed anymore
            //UpdateRecordsOfPrevCodes();

            onComplete?.Invoke(allUniqueCodes.ToList());
        }

        private async Task<Dictionary<string, string>> GetLatestFormularyIdsForCodes(List<string> codes)
        {
            #region Sample To Analyze
            /*
             Sample:
                VMP01 -FID01-V1.0 -1July
                VMP01 -FID01-V2.0 -2July
                VMP01 -FID02-V1.0 -3July
                VMP01 -FID02-V2.0 -3July
                VMP01 -FID01-V3.0 -14July
             */
            #endregion Sample To Analyze

            var uniqueCodes = codes.Where(rec => rec.IsNotEmpty())?.Distinct().ToList();

            if (!uniqueCodes.IsCollectionValid()) return null;

            var batchsize = 10;

            var batchedRequests = new List<string[]>();

            for (var reqIndex = 0; reqIndex < uniqueCodes.Count; reqIndex += batchsize)
            {
                var batches = uniqueCodes.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToArray());
            }
            var codeFormularyIdLookup = new Dictionary<string, string>();

            var codesWithRootVersions = new List<dynamic>();

            foreach (var batchedReq in batchedRequests)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                var rootVersions = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                    .Where(rec => batchedReq.Contains(rec.Code) && rec.VersionId == 1)
                    ?.Select(rec => new { rec.Code, rec.FormularyId, rec.Createdtimestamp })
                    .ToList();

                DisposeUoWWithScope(scope, unitOfWork);

                if (rootVersions.IsCollectionValid())
                    rootVersions.Each(rec => codesWithRootVersions.Add(rec));
            }

            if (!codesWithRootVersions.IsCollectionValid()) return null;

            foreach (var uniqueCode in uniqueCodes)
            {
                //this should select the formulary id of the now added code
                var firstFormularyId = codesWithRootVersions.Where(rec => rec.Code == uniqueCode)
                    ?.OrderByDescending(rec => rec.Createdtimestamp)
                    .Select(rec => rec.FormularyId)
                    .FirstOrDefault();

                codeFormularyIdLookup[uniqueCode] = firstFormularyId;
            }

            //codesWithRootVersions?.Each(rec =>
            //{
            //    if (!codeFormularyIdLookup.ContainsKey(rec.Code))
            //        codeFormularyIdLookup[rec.Code] = rec.FormularyId;
            //});

            return codeFormularyIdLookup;
        }

        private async Task ArchiveOlderDraftsIfExists(List<string> allCodes)
        {
            //the AMP Draft records that are not newly created 
            if (!allCodes.IsCollectionValid()) return;

            var codesB = allCodes.Where(rec => rec != null && rec.IsNotEmpty())?.Select(rec => rec.Trim()).ToList();

            if (!codesB.IsCollectionValid()) return;

            var batchsizeForDesc = 10;

            var batchedRequestsForDesc = new List<string[]>();

            for (var reqIndex = 0; reqIndex < codesB.Count; reqIndex += batchsizeForDesc)
            {
                var batches = codesB.Skip(reqIndex).Take(batchsizeForDesc);
                batchedRequestsForDesc.Add(batches.ToArray());
            }

            //var latestDescendents = new List<Model.Search.FormularyBasicSearchResultModel>();
            var allVersionsOfLatestDescendents = new List<FormularyHeader>();

            foreach (var batchedReq in batchedRequestsForDesc)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                //this will bring all the latest descendents
                var latestDescendentsTemp = await unitOfWork.FormularyBasicResultsFormularyRepository.GetFormularyDescendentForCodes(batchedReq);

                if (latestDescendentsTemp.IsCollectionValid())
                {
                    //latestDescendents.AddRange(latestDescendentsTemp);

                    var codesFromDescendents = latestDescendentsTemp.Select(rec => rec.Code).ToList();

                    //bring all latest records for those above codes
                    var allLatestCodesOfDescendents = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly.Where(rec => codesFromDescendents.Contains(rec.Code) && rec.IsLatest == true).ToList();

                    if (allLatestCodesOfDescendents.IsCollectionValid())
                        allVersionsOfLatestDescendents.AddRange(allLatestCodesOfDescendents);
                }

                DisposeUoWWithScope(scope, unitOfWork);
            }

            if (!allVersionsOfLatestDescendents.IsCollectionValid()) return;

            //for these descendents - check if it added as 'draft' now as part of import process or it is still the old draft
            //if old - archive it and create new draft
            allVersionsOfLatestDescendents = allVersionsOfLatestDescendents.Where(rec => !codesB.Contains(rec.Code)).ToList();

            if (!allVersionsOfLatestDescendents.IsCollectionValid()) return;

            var ampsInDraft = allVersionsOfLatestDescendents
                .Where(rec => (string.Compare(rec.ProductType, "amp", true) == 0) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?
                .Select(rec => rec.FormularyVersionId)
                .Distinct()
                .ToList();

            //These records to be used for cloning new draft
            //is it part of recordsToArchive (input codes), then ignore - since it has been added now as part of import. For others archive first.
            //New entry will be in the next step (next fn)
            //var ampsInDraftToRecreateDraft = latestDescendents
            //    .Where(rec=> !codesB.Contains(rec.Code))?
            //    .Where(rec => (string.Compare(rec.ProductType, "amp", true) == 0) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)
            //    .Select(rec => rec.FormularyVersionId)
            //    .Distinct(rec=> rec)
            //    .ToList();


            if (!ampsInDraft.IsCollectionValid()) return;


            //var ampsInDraftToArchive = ampsInDraft.Where(rec => !codesB.Contains(rec)).ToList();

            //if (!ampsInDraftToArchive.IsCollectionValid()) return;

            var batchsize = 10;

            var batchedRequests = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < ampsInDraft.Count; reqIndex += batchsize)
            {
                var batches = ampsInDraft.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToList());
            }

            foreach (var batch in batchedRequests)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                var resultsTobeCloned = unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking()
                    .Where(rec => rec.IsLatest == true && batch.Contains(rec.FormularyVersionId))
                .OrderBy(rec => rec.VersionId)?
                .ToList();

                if (!resultsTobeCloned.IsCollectionValid()) continue;

                var draftResultsTobeCloned = resultsTobeCloned.Where(rec => rec.IsLatest == true && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT);

                var existingDraftsTobeupdated = unitOfWork.FormularyHeaderFormularyRepository.Items.Where(rec => rec.IsLatest == true && batch.Contains(rec.FormularyVersionId) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT);

                if (!existingDraftsTobeupdated.IsCollectionValid()) continue;

                var clonedFormulariesToSave = new List<FormularyHeader>();

                foreach (var fvId in batch)
                {
                    var existingDraft = existingDraftsTobeupdated.FirstOrDefault(rec => rec.FormularyVersionId == fvId && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT);

                    if (existingDraft == null) continue;

                    existingDraft.IsLatest = false;
                    unitOfWork.FormularyHeaderFormularyRepository.Update(existingDraft);

                    //clone and create an archive of the old draft
                    var tobeCloned = draftResultsTobeCloned.Where(rec => rec.FormularyVersionId == existingDraft.FormularyVersionId).FirstOrDefault();

                    if (tobeCloned != null)
                    {
                        //this increments the existing version during clone
                        var cloned = _formularyUtil.CloneFormulary(tobeCloned);
                        cloned.IsLatest = true;//archived always true
                        cloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
                        unitOfWork.FormularyHeaderFormularyRepository.Add(cloned);
                    }
                }
                unitOfWork.FormularyHeaderFormularyRepository.SaveChanges();

                DisposeUoWWithScope(scope, unitOfWork);
            }
        }

        private void UpdateVMPRelatedData(List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)> vmps, Dictionary<string, List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)>> parentWithDraftChildrenLkp)
        {
            if (!vmps.IsCollectionValid()) return;

            foreach (var vmp in vmps)
            {
                //MMC-477 - Modify only for 'Draft' AMP. Should not modify the 'Active' ones
                //var ampsForVMP = parentWithDraftChildrenLkp.ContainsKey(vmp.Code) ? parentWithDraftChildrenLkp[vmp.Code] : null;
                var ampsForVMP = parentWithDraftChildrenLkp.ContainsKey(vmp.FormularyId) ? parentWithDraftChildrenLkp[vmp.FormularyId] : null;

                //var ampsForVMP = allWithDetails.Where(rec => rec.ParentCode == vmp.Code && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT).ToList();
                //var ampsForVMP = allWithDetails.Where(rec => rec.IsLatest == true && rec.ParentCode == vmp.Code && (rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED)).ToList();

                if (!ampsForVMP.IsCollectionValid()) continue;

                var (scope, unitOfWork) = GetUoWInNewScope();

                var vmpInDb = GetLatestFormulariesByFormularyVersionIds(new string[] { vmp.FormularyVersionId }, unitOfWork).FirstOrDefault();

                var ampsForVMPFVIds = ampsForVMP.Select(rec => rec.FormularyVersionId).ToArray();

                var ampsForVMPInDb = GetLatestFormulariesByFormularyVersionIds(ampsForVMPFVIds, unitOfWork);

                AssignAMPsWithVMPProps(ampsForVMPInDb, vmpInDb, unitOfWork);

                unitOfWork.FormularyHeaderFormularyRepository.SaveChanges();

                DisposeUoWWithScope(scope, unitOfWork);
            }
        }

        private (IServiceScope scope, IUnitOfWork? unitOfWork) GetUoWInNewScope()
        {
            var scope = _serviceProvider.CreateScope();
            var svp = scope.ServiceProvider;
            var unitOfWork = svp.GetService<IUnitOfWork>();

            return (scope, unitOfWork);
        }

        private void DisposeUoWWithScope(IServiceScope scope, IUnitOfWork? unitOfWork)
        {
            if (unitOfWork != null) unitOfWork.Dispose();
            if (scope != null) scope.Dispose();
        }

        private async Task UpdateVtmRelatedData(List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)> vtms, Dictionary<string, List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode1, string RecStatusCode, string ParentFormularyId)>> parentWithDraftChildrenLkp)
        {
            if (!vtms.IsCollectionValid()) return;

            foreach (var vtm in vtms)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                //var vmpsForVTM = allWithDetails.Where(rec => rec.ParentCode == vtm.Code).ToList();
                //var vmpsForVTM = parentWithDraftChildrenLkp.ContainsKey(vtm.Code) ? parentWithDraftChildrenLkp[vtm.Code] : null;
                var vmpsForVTM = parentWithDraftChildrenLkp.ContainsKey(vtm.FormularyId) ? parentWithDraftChildrenLkp[vtm.FormularyId] : null;

                if (!vmpsForVTM.IsCollectionValid()) continue;

                var vtmInDb = GetLatestFormulariesByFormularyVersionIds(new string[] { vtm.FormularyVersionId }, unitOfWork).FirstOrDefault();

                var vmpsForVTMFVIds = vmpsForVTM.Select(rec => rec.FormularyVersionId).ToArray();
                var vmpsForVTMInDb = GetLatestFormulariesByFormularyVersionIds(vmpsForVTMFVIds, unitOfWork);

                AssignVTMsWithVMPProps(vmpsForVTMInDb, vtmInDb, unitOfWork);

                foreach (var vmp in vmpsForVTM)
                {
                    //MMC-477 - Modify only for 'Draft' AMP. Should not modify the 'Active' ones
                    //var ampsForVMP = parentWithDraftChildrenLkp.ContainsKey(vmp.Code) ? parentWithDraftChildrenLkp[vmp.Code] : null;
                    var ampsForVMP = parentWithDraftChildrenLkp.ContainsKey(vmp.FormularyId) ? parentWithDraftChildrenLkp[vmp.FormularyId] : null;

                    //var ampsForVMP = allWithDetails.Where(rec => rec.ParentCode == vmp.Code && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT).ToList();
                    //var ampsForVMP = allWithDetails.Where(rec => rec.IsLatest == true && rec.ParentCode == vmp.Code && (rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED)).ToList();

                    if (!ampsForVMP.IsCollectionValid()) continue;

                    var ampsForVMPIds = ampsForVMP.Select(rec => rec.FormularyVersionId).ToArray();
                    var ampsForVMPInDb = GetLatestFormulariesByFormularyVersionIds(ampsForVMPIds, unitOfWork);

                    await ProcessInhalationRule8(ampsForVMPInDb, vtmInDb, unitOfWork);
                }

                unitOfWork.FormularyHeaderFormularyRepository.SaveChanges();

                DisposeUoWWithScope(scope, unitOfWork);
            }
        }

        /// <summary>
        /// Return list of formularies header only for the codes passes
        /// </summary>
        /// <param name="allUniqueCodesWithFIdsLookup"></param>
        /// <returns></returns>
        private List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)> GetLatestFormulariesFewHeaderOnlyByCodes(Dictionary<string, string> allUniqueCodesWithFIdsLookup)
        {
            if (!allUniqueCodesWithFIdsLookup.IsCollectionValid()) return null;

            var batchsize = 10;

            var allUniqueForumlaryIds = allUniqueCodesWithFIdsLookup.Values?.Where(rec => rec.IsNotEmpty())?.Distinct().ToList() ?? new List<string>();

            var batchedRequests = new List<string[]>();

            for (var reqIndex = 0; reqIndex < allUniqueForumlaryIds.Count; reqIndex += batchsize)
            {
                var batches = allUniqueForumlaryIds.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToArray());
            }

            var resultsFromDb = new List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)>();

            foreach (var batchedReq in batchedRequests)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                //var results = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                //    .Where(rec => rec.IsLatest == true && batchedReq.Contains(rec.Code) && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED)
                //    .AsEnumerable()
                //    .Select(rec => (rec.Code, rec.FormularyVersionId, rec.ProductType, rec.ParentCode, rec.RecStatusCode))
                //    .ToList();

                var results = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                    .Where(rec => rec.IsLatest == true && batchedReq.Contains(rec.FormularyId) && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED)
                    .AsEnumerable()
                    .Select(rec => (Code1: rec.Code, rec.FormularyId, rec.FormularyVersionId, rec.ProductType, rec.ParentCode, rec.RecStatusCode, rec.ParentFormularyId))
                    .ToList();

                if (results.IsCollectionValid())
                    resultsFromDb.AddRange(results);

                DisposeUoWWithScope(scope, unitOfWork);

            }
            return resultsFromDb;
        }

        /// <summary>
        /// Return list of formularies for the FVIds passes with tracking (handle with care)
        /// </summary>
        /// <param name="allUniqueFVIds"></param>
        /// <returns></returns>
        private List<FormularyHeader>? GetLatestFormulariesByFormularyVersionIds(string[] allUniqueFVIds, IUnitOfWork unitOfWorkIn)
        {
            if (!allUniqueFVIds.IsCollectionValid()) return null;

            var batchsize = 10;

            var batchedRequests = new List<string[]>();
            for (var reqIndex = 0; reqIndex < allUniqueFVIds.Length; reqIndex += batchsize)
            {
                var batches = allUniqueFVIds.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToArray());
            }

            var resultsFromDb = new List<FormularyHeader>();

            foreach (var batchedReq in batchedRequests)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();
                if (unitOfWorkIn != null)
                    unitOfWork = unitOfWorkIn;

                var results = unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryable(true)
                    .Where(rec => rec.IsLatest == true && batchedReq.Contains(rec.FormularyVersionId))
                    .ToList();

                if (results.IsCollectionValid())
                    resultsFromDb.AddRange(results);

                if (unitOfWorkIn == null)
                    DisposeUoWWithScope(scope, unitOfWork);
            }
            return resultsFromDb;
        }

        /// <summary>
        /// Return list of formularies for the codes passes with tracking (handle with care)
        /// </summary>
        /// <param name="allUniqueCodes"></param>
        /// <returns></returns>
        private List<FormularyHeader> GetLatestFormulariesByCodes(string[] allUniqueCodes)
        {
            if (!allUniqueCodes.IsCollectionValid()) return null;

            var batchsize = 10;

            var batchedRequests = new List<string[]>();
            for (var reqIndex = 0; reqIndex < allUniqueCodes.Length; reqIndex += batchsize)
            {
                var batches = allUniqueCodes.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToArray());
            }

            var resultsFromDb = new List<FormularyHeader>();

            foreach (var batchedReq in batchedRequests)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                var results = unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryable(true)
                    .Where(rec => rec.IsLatest == true && batchedReq.Contains(rec.Code))
                    .ToList();

                if (results.IsCollectionValid())
                    resultsFromDb.AddRange(results);

                DisposeUoWWithScope(scope, unitOfWork);
            }
            return resultsFromDb;
        }

        private Dictionary<string, List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)>>? GetParentWithDraftChildrenAsLkp(List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)> allWithDetails)
        {
            Dictionary<string, List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)>> lkp = new();

            if (!allWithDetails.IsCollectionValid()) return lkp;

            foreach (var item in CollectionsMarshal.AsSpan(allWithDetails))
            {
                //if (item == null || item.ParentCode.IsEmpty() || item.RecStatusCode != TerminologyConstants.RECORDSTATUS_DRAFT) continue;
                //if (item.ParentCode.IsEmpty() || (string.Compare(item.ProductType, "amp", true) == 0 && item.RecStatusCode != TerminologyConstants.RECORDSTATUS_DRAFT)) continue;

                if (item.ParentFormularyId.IsEmpty() || (string.Compare(item.ProductType, "amp", true) == 0 && item.RecStatusCode != TerminologyConstants.RECORDSTATUS_DRAFT)) continue;

                if (!lkp.ContainsKey(item.ParentFormularyId))
                    lkp[item.ParentFormularyId] = new List<(string Code1, string FormularyId, string FormularyVersionId, string ProductType, string ParentCode, string RecStatusCode, string ParentFormularyId)>();

                lkp[item.ParentFormularyId].Add(item);
            }

            return lkp;
        }

        #region Old code - ref only
        /*
        private async Task FindAndCreateMissingAMPDraftsForParents(List<string>? codes)
        {
            if (!codes.IsCollectionValid()) return;

            codes = codes.Where(rec => rec != null && rec.IsNotEmpty())?.Select(rec => rec.Trim()).ToList();

            if (!codes.IsCollectionValid()) return;

            var batchsizeForDesc = 10;

            var batchedRequestsForDesc = new List<string[]>();
            for (var reqIndex = 0; reqIndex < codes.Count; reqIndex += batchsizeForDesc)
            {
                var batches = codes.Skip(reqIndex).Take(batchsizeForDesc);
                batchedRequestsForDesc.Add(batches.ToArray());
            }

            var latestDescendents = new List<Model.Search.FormularyBasicSearchResultModel>();

            //these amps have both latest and non-latest amps
            var ampsByParentCodes = new List<FormularyHeader>();

            foreach (var batchedReq in batchedRequestsForDesc)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                //this will bring all the latest descendents
                var latestDescendentsTemp = await unitOfWork.FormularyBasicResultsFormularyRepository.GetFormularyDescendentForCodes(batchedReq);

                if (latestDescendentsTemp.IsCollectionValid())
                    latestDescendents.AddRange(latestDescendentsTemp);

                GetAllDescendentAMPs(batchedReq, ampsByParentCodes, unitOfWork);

                DisposeUoWWithScope(scope, unitOfWork);
            }


            if (!latestDescendents.IsCollectionValid()) return;

            //get all the amps which are in draft and other list with any status
            //Take all codes except that are in draft and then create a draft of those amps

            var ampsInAllStatus = latestDescendents
                .Where(rec => (string.Compare(rec.ProductType, "amp", true) == 0) && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED)?
                .Select(rec => rec.Code)
                .Distinct()
                .ToList();

            if (!ampsInAllStatus.IsCollectionValid()) return;

            var ampsInDraft = latestDescendents
                .Where(rec => (string.Compare(rec.ProductType, "amp", true) == 0) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?
                .Select(rec => rec.Code)
                .Distinct()
                .ToList();

            var missingDraftAMPs = ampsInAllStatus;

            if (ampsInDraft.IsCollectionValid())
                missingDraftAMPs = ampsInAllStatus.Except(ampsInDraft)?.ToList();

            if (!missingDraftAMPs.IsCollectionValid()) return;

            //For these amps which are not in draft, import from DMD formulary (or Terminology) and create a new 'draft' of it.
            if (!missingDraftAMPs.IsCollectionValid()) return;

            var batchsize = 10;

            var batchedRequests = new List<List<string>>();
            for (var reqIndex = 0; reqIndex < missingDraftAMPs.Count; reqIndex += batchsize)
            {
                var batches = missingDraftAMPs.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToList());
            }
            foreach (var batchedReq in batchedRequests)
            {
                CloneAndCreateDraftForAMPs(batchedReq, ampsByParentCodes);
                //await _formularyImportHandler.ImportByCodes(batchedReq);
            }
        }
        
        private void GetAllDescendentAMPs(string[] batchedReq, List<FormularyHeader> ampsByParentCodes, IUnitOfWork unitOfWork)
        {
            if (!batchedReq.IsCollectionValid()) return;

            var allByParentCodes = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                    .Where(rec => batchedReq.Contains(rec.ParentCode)).ToArray();

            if (!allByParentCodes.IsCollectionValid()) return;

            var ampsByParentCodesTemp = allByParentCodes.Where(rec => string.Compare(rec.ProductType, "amp", true) == 0).ToArray();

            if (ampsByParentCodesTemp.IsCollectionValid())
                ampsByParentCodes.AddRange(ampsByParentCodesTemp);

            var descendentCodes = allByParentCodes.Select(rec => rec.Code).ToArray();

            GetAllDescendentAMPs(descendentCodes, ampsByParentCodes, unitOfWork);
        }
        

        private void CloneAndCreateDraftForAMPs(List<string> codes, List<FormularyHeader> ampsByParentCodes)
        {
            if (!codes.IsCollectionValid()) return;

            var results = new List<FormularyHeader>();
            //var existingAllVersDrafts = new List<FormularyHeader>();

            var (scope, unitOfWork) = GetUoWInNewScope();

            //var results = unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesByCodes(codes.ToArray())?.ToArray();

            var resultsTemp = unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking().
            Where(rec => rec.IsLatest == true && codes.Contains(rec.Code));

            if (resultsTemp.IsCollectionValid())
                results.AddRange(resultsTemp);

            var resultsFromSameParent = new List<FormularyHeader>();

            if (ampsByParentCodes.IsCollectionValid())
            {
                var formulariesVerIdsForCodes = ampsByParentCodes.Where(rec => codes.Contains(rec.Code)).Select(rec => rec.FormularyVersionId).ToList();

                if (formulariesVerIdsForCodes.IsCollectionValid())
                {
                    var resultsFromSameParentTemp = unitOfWork.FormularyHeaderFormularyRepository.GetAllFormulariesAsQueryableWithNoTracking().
                    Where(rec => rec.IsLatest == true && formulariesVerIdsForCodes.Contains(rec.FormularyVersionId));

                    if (resultsFromSameParentTemp.IsCollectionValid())
                        resultsFromSameParent.AddRange(resultsFromSameParentTemp);
                }
            }

            //var existingAllVersDraftsTemp = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly.Where(rec => codes.Contains(rec.Code));//consider all versions

            //if (existingAllVersDraftsTemp.IsCollectionValid())
            //    existingAllVersDrafts.AddRange(existingAllVersDraftsTemp);


            //var codeVersionIdLkp = existingAllVersDrafts?
            //   .Select(rec => new { CodeStatus = $"{rec.Code}|{rec.RecStatusCode}", VersionId = rec.VersionId })
            //   .GroupBy(rec => rec.CodeStatus, rec => rec.VersionId, (k, v) => new { CodeStatus = k, VersionId = v.Max() })
            //   .Distinct(rec => rec.CodeStatus)
            //   .ToDictionary(k => k.CodeStatus, v => v.VersionId) ?? new Dictionary<string, int?>();

            if (!results.IsCollectionValid()) return;

            var clonedFormulariesToSave = new List<FormularyHeader>();

            foreach (var code in codes)
            {
                var existingDraft = results.FirstOrDefault(rec => rec.Code == code && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT);
                var existingActive = results.FirstOrDefault(rec => rec.Code == code && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE);
                var existingArchived = results.FirstOrDefault(rec => rec.Code == code && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED);

                //already has an existing draft
                if (existingDraft != null) continue;

                var existingRecToConsider = existingActive ?? existingArchived;

                //give priority to same parent
                if (resultsFromSameParent.IsCollectionValid())
                {
                    var existingActivesFromSameParent = resultsFromSameParent.Where(rec => rec.Code == code && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE);
                    var existingArchivedsFromSameParent = resultsFromSameParent.Where(rec => rec.Code == code && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED);

                    if (existingActivesFromSameParent.IsCollectionValid())
                    {
                        var existingActiveFromSameParent = existingActivesFromSameParent.Where(rec => rec.IsLatest == true).FirstOrDefault();
                        if (existingActiveFromSameParent == null)
                            existingActiveFromSameParent = existingActivesFromSameParent.FirstOrDefault();

                        existingRecToConsider = existingActiveFromSameParent;
                    }
                    else if (existingArchivedsFromSameParent.IsCollectionValid())
                    {
                        var existingArchivedFromSameParent = existingArchivedsFromSameParent.Where(rec => rec.IsLatest == true).FirstOrDefault();
                        if (existingArchivedFromSameParent == null)
                            existingArchivedFromSameParent = existingArchivedsFromSameParent.FirstOrDefault();

                        existingRecToConsider = existingArchivedFromSameParent;
                    }
                }


                if (existingRecToConsider == null) continue;

                var cloned = _formularyUtil.CloneFormulary(existingRecToConsider);
                if (cloned == null) continue;

                //Since it is a draft - create a new formularyid- a tracker id
                cloned.FormularyId = Guid.NewGuid().ToString();

                cloned.VersionId = 1;
                cloned.IsLatest = true;
                // Draft AMP version id always set to 1
                //if (codeVersionIdLkp != null && codeVersionIdLkp.ContainsKey($"{cloned.Code}|{TerminologyConstants.RECORDSTATUS_DRAFT}"))
                //    cloned.VersionId = codeVersionIdLkp[$"{cloned.Code}|{TerminologyConstants.RECORDSTATUS_DRAFT}"] + 1;
                

                cloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT;
                cloned.RecSource = TerminologyConstants.RECORD_SOURCE_IMPORT;// "Import";
                cloned.RecStatuschangeDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
                cloned.RecStatuschangeTs = DateTime.UtcNow;

                clonedFormulariesToSave.Add(cloned);
            }

            if (!clonedFormulariesToSave.IsCollectionValid()) return;


            unitOfWork.FormularyHeaderFormularyRepository.AddRange(clonedFormulariesToSave.ToArray());
            unitOfWork.FormularyHeaderFormularyRepository.SaveChanges();

            DisposeUoWWithScope(scope, unitOfWork);
        }
        */
        #endregion Old code - ref only

        private async Task ProcessInhalationRule8(List<FormularyHeader> ampsForVTM, FormularyHeader vtm, IUnitOfWork unitOfWork)
        {
            if (!ampsForVTM.IsCollectionValid() || vtm == null || !vtm.FormularyDetail.IsCollectionValid()) return;

            var vtmDetail = vtm.FormularyDetail.First();

            var routesLookup = await _terminologyAPIService.GetRouteLookup();// await dmdQueries.GetLookup<DmdLookupRouteDTO>(LookupType.DMDRoute);

            var inhalationRoute = routesLookup?.Data?.Where(rec => string.Compare(rec.Desc, "inhalation", true) == 0).FirstOrDefault();

            if (inhalationRoute == null) return;

            //Check if any of the amp for this vtm has Inhalation as route - then mark it as not prescribable

            var hasInhalationRoute = false;

            foreach (var ampForVTM in ampsForVTM)
            {
                if (ampForVTM.FormularyRouteDetail.IsCollectionValid())
                {
                    hasInhalationRoute = ampForVTM.FormularyRouteDetail.Any(rec => rec.RouteCd == inhalationRoute.Cd);
                    if (hasInhalationRoute) break;
                }
            }

            if (hasInhalationRoute)
            {
                //MMC-477-if previously modified by user, no need to change it.
                if (string.Compare(vtmDetail.PrescribableSource ?? "", TerminologyConstants.DMD_DATA_SRC, true) != 0) return;

                vtmDetail.Prescribable = false;
                vtmDetail.PrescribableSource = TerminologyConstants.DMD_DATA_SRC;
                unitOfWork.FormularyHeaderFormularyRepository.Update(vtm);
            }
        }

        private void AssignVTMsWithVMPProps(List<FormularyHeader> vmpsForVTM, FormularyHeader vtm, IUnitOfWork unitOfWork)
        {
            if (!vmpsForVTM.IsCollectionValid()) return;

            //var isWitnessRequired = false;
            //var details = vmpsForVTM
            //    .Select(rec => rec.FormularyDetail.FirstOrDefault())?
            //    .Each(vmpDetail =>
            //    {
            //        //Update witness required flag
            //        isWitnessRequired = (vmpDetail != null && vmpDetail.ControlledDrugCategoryCd != "0");
            //    });

            var isWitnessRequired = vmpsForVTM
                .SelectMany(rec => rec.FormularyDetail)?
                .Any(vmpDetail => vmpDetail != null && vmpDetail.ControlledDrugCategoryCd != "0");

            var vtmDetail = vtm.FormularyDetail.FirstOrDefault();

            if (vtmDetail != null)
            {
                vtmDetail.WitnessingRequired = isWitnessRequired == true ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
                unitOfWork.FormularyHeaderFormularyRepository.Update(vtm);
            }
        }

        private void AssignAMPsWithVMPProps(List<FormularyHeader> ampsForVMP, FormularyHeader vmp, IUnitOfWork unitOfWork)
        {
            if (!ampsForVMP.IsCollectionValid()) return;

            var ampsForVMPCodes = ampsForVMP.Select(rec => rec.Code).Distinct().ToList();

            //var existingFormulariesForAllAMPCodes = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly.Where(rec => ampsForVMPCodes.Contains(rec.Code)).ToList();

            var activeExistingFormulariesForAllAMPCodesWithControlledDrugCtgrs = new Dictionary<string, string>();

            var activeExistingFormulariesForAllAMPCodes = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => ampsForVMPCodes.Contains(rec.Code) && rec.IsLatest == true && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)
                ?.Select(rec=> new { rec.Code, rec.RecStatusCode, rec.FormularyId, rec.FormularyVersionId })
                .ToList();

            if (activeExistingFormulariesForAllAMPCodes.IsCollectionValid())
            {
                var fvIds = activeExistingFormulariesForAllAMPCodes.Select(rec=> rec.FormularyVersionId).Distinct().ToList();

                var activeExistingFormulariesDetailForAllAMPCodes = unitOfWork.FormularyDetailFormularyRepository.ItemsAsReadOnly
                .Where(rec => fvIds.Contains(rec.FormularyVersionId))
                ?.Select(rec => new { rec.ControlledDrugCategoryCd, rec.FormularyVersionId })
                .Distinct(rec=> rec.FormularyVersionId)
                .ToDictionary(k=> k.FormularyVersionId, v=> v.ControlledDrugCategoryCd);

                foreach (var item in activeExistingFormulariesForAllAMPCodes)
                {
                    var cd = activeExistingFormulariesDetailForAllAMPCodes.IsCollectionValid() && activeExistingFormulariesDetailForAllAMPCodes.ContainsKey(item.FormularyVersionId) ? activeExistingFormulariesDetailForAllAMPCodes[item.FormularyVersionId] : "0";//Non controller
                    activeExistingFormulariesForAllAMPCodesWithControlledDrugCtgrs[$"{item.Code}"] = cd;
                }
            }

            //var ampsForVMPFormularyIds = ampsForVMP.Select(rec => rec.FormularyId).Distinct().ToList();

            List<FormularyHeader> existingFormulariesForAllAMPFormularyIds = null;//unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly.Where(rec => ampsForVMPFormularyIds.Contains(rec.FormularyId)).ToList();

            ampsForVMP.Each(amp =>
            {
                PopulateFormularyDetailForAMPFromVMP(amp, vmp, existingFormulariesForAllAMPFormularyIds, activeExistingFormulariesForAllAMPCodesWithControlledDrugCtgrs);
                PopulateFormularyAdditionalCodesForAMPFromVMP(amp, vmp);
                PopulateFormularyIngredientsForAMPFromVMP(amp, vmp);

                PopulateFormularyUnlicensedRouteForAMPFromVMP(amp, vmp);
                SyncAMPLicensedAndUnLicensedRoutes(amp);

                //MMC-477-Call to update descriptions for amp
                amp.FormularyDetail?.Each(rec => UpdateFormularyDetailDMDLookup(rec));
                amp.FormularyExcipient?.Each(exc => UpdateFormularyExcipientDMDLookup(exc));
                amp.FormularyRouteDetail?.Each(rec => UpdateFormularyRoutesDMDLookup(rec));
                amp.FormularyLocalRouteDetail?.Each(rec => UpdateFormularyLocalRoutesDMDLookup(rec));
                amp.FormularyIngredient?.Each(rec => UpdateFormularyIngredientsDMDLookup(rec));

                //After replicating to AMP Level - Update AMP to db
                unitOfWork.FormularyHeaderFormularyRepository.Update(amp);
            });
        }

        private void PopulateFormularyDetailForAMPFromVMP(FormularyHeader ampForVMP, FormularyHeader vmp, List<FormularyHeader> existingFormulariesForAllAMPFormularyIds, Dictionary<string, string> activeExistingFormulariesForAllAMPCodesWithControlledDrugCtgrs)
        {
            if (vmp == null || ampForVMP == null || !vmp.FormularyDetail.IsCollectionValid() || !ampForVMP.FormularyDetail.IsCollectionValid())
                return;

            var vmpDetail = vmp.FormularyDetail.First();
            var ampDetail = ampForVMP.FormularyDetail.First();

            //MMC-477:This below line is trackable and not required here
            //var existingFormularies = _unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesByCodes(new[] { ampForVMP.Code }).ToList();
            //var existingFormularies = existingFormulariesForAllAMPCodes.Where(rec => rec.Code == ampForVMP.Code)?.ToList();

            //var existingFormularies = existingFormulariesForAllAMPFormularyIds?.Where(rec => rec.Code == ampForVMP.Code)?.ToList();
            //var existingFormulariesForFormularyIds = existingFormulariesForAllAMPFormularyIds?.Where(rec => rec.FormularyId == ampForVMP.FormularyId)?.ToList();

            ampDetail.BasisOfPreferredNameCd = vmpDetail.BasisOfPreferredNameCd;

            ampDetail.ControlledDrugCategoryCd = vmpDetail.ControlledDrugCategoryCd;
            ampDetail.ControlledDrugCategorySource = vmpDetail.ControlledDrugCategorySource;
            ampDetail.DoseFormCd = vmpDetail.DoseFormCd;

            ampDetail.FormCd = vmpDetail.FormCd;

            ampDetail.CfcFree = vmpDetail.CfcFree;
            ampDetail.GlutenFree = vmpDetail.GlutenFree;
            ampDetail.PrescribingStatusCd = vmpDetail.PrescribingStatusCd;

            ampDetail.PreservativeFree = vmpDetail.PreservativeFree;
            ampDetail.SugarFree = vmpDetail.SugarFree;
            ampDetail.UnitDoseFormSize = vmpDetail.UnitDoseFormSize;
            ampDetail.UnitDoseFormUnits = vmpDetail.UnitDoseFormUnits;
            ampDetail.UnitDoseUnitOfMeasureCd = vmpDetail.UnitDoseUnitOfMeasureCd;

            var isControlledDrugInVMPChanged = false;

            if (activeExistingFormulariesForAllAMPCodesWithControlledDrugCtgrs.IsCollectionValid())
            {
                if (!activeExistingFormulariesForAllAMPCodesWithControlledDrugCtgrs.ContainsKey(ampForVMP.Code))
                    isControlledDrugInVMPChanged = ampDetail.ControlledDrugCategoryCd.IsNotEmpty();
                else
                    isControlledDrugInVMPChanged = activeExistingFormulariesForAllAMPCodesWithControlledDrugCtgrs[ampForVMP.Code] != ampDetail.ControlledDrugCategoryCd;
            }
            else
            {
                isControlledDrugInVMPChanged = ampDetail.ControlledDrugCategoryCd.IsNotEmpty();
            }

            //if isControlledDrugInVMPChanged has changed go with the rule. Else go with what is in previous
            if (isControlledDrugInVMPChanged)//go with rule - otherwise taken care by merge handler of AMP
            {
                if (ampDetail.ControlledDrugCategoryCd.IsNotEmpty() && ampDetail.ControlledDrugCategoryCd != "0")
                {
                    ampDetail.IsCustomControlledDrug = true;
                    ampDetail.IsPrescriptionPrintingRequired = true;
                    ampDetail.IsIndicationMandatory = true;
                    ampDetail.WitnessingRequired =  TerminologyConstants.STRINGIFIED_BOOL_TRUE;
                }
                else
                {
                    ampDetail.IsCustomControlledDrug = false;
                    ampDetail.IsPrescriptionPrintingRequired = false;
                    ampDetail.IsIndicationMandatory = false;
                    ampDetail.WitnessingRequired = TerminologyConstants.STRINGIFIED_BOOL_FALSE;
                }
            }

            #region old code - ref only
            /*
            if (existingFormulariesForFormularyIds.Any(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE) && !existingFormulariesForFormularyIds.Any(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT))
            {
                if (ampDetail.ControlledDrugCategoryCd.IsEmpty() || ampDetail.ControlledDrugCategoryCd == "0")
                {
                    ampDetail.IsCustomControlledDrug = false;
                    ampDetail.IsPrescriptionPrintingRequired = false;
                    ampDetail.IsIndicationMandatory = false;
                }
                else
                {
                    ampDetail.IsCustomControlledDrug = true;
                    ampDetail.IsPrescriptionPrintingRequired = true;
                    ampDetail.IsIndicationMandatory = true;
                }
            }
            else if (!existingFormulariesForFormularyIds.Any(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE) && existingFormulariesForFormularyIds.Any(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT))
            {
                if (ampDetail.ControlledDrugCategoryCd.IsEmpty() || ampDetail.ControlledDrugCategoryCd == "0")
                {
                    ampDetail.IsCustomControlledDrug = false;
                    ampDetail.IsPrescriptionPrintingRequired = false;
                    ampDetail.IsIndicationMandatory = false;
                }
                else
                {
                    ampDetail.IsCustomControlledDrug = true;
                    ampDetail.IsPrescriptionPrintingRequired = true;
                    ampDetail.IsIndicationMandatory = true;
                }
            }

            if (existingFormulariesForFormularyIds.Any(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE) && !existingFormulariesForFormularyIds.Any(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT))
            {
                ampDetail.WitnessingRequired = vmpDetail.ControlledDrugCategoryCd != "0" ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            else if (!existingFormulariesForFormularyIds.Any(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE) && existingFormulariesForFormularyIds.Any(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT))
            {
                ampDetail.WitnessingRequired = vmpDetail.ControlledDrugCategoryCd != "0" ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;
            }
            */
            #endregion old code - ref only

            SyncAMPLicensedAndUnLicensedUse(ampDetail);
        }

        private void PopulateFormularyAdditionalCodesForAMPFromVMP(FormularyHeader amp, FormularyHeader vmp)
        {
            //Copy ATC Code only as BNF is already mapped at AMP level
            amp.FormularyAdditionalCode = amp.FormularyAdditionalCode ?? new List<FormularyAdditionalCode>();
            var ampBNFs = new HashSet<string>();
            var ampBNFsSevenChars = new HashSet<string>();

            if (amp.FormularyAdditionalCode.IsCollectionValid())
            {
                var ampAtcs = amp.FormularyAdditionalCode.Where(rec => string.Compare(rec.AdditionalCodeSystem, "atc", true) == 0).ToList();

                amp.FormularyAdditionalCode.Where(rec => string.Compare(rec.AdditionalCodeSystem, "bnf", true) == 0 && rec.AdditionalCode != null)?
                    .Each(rec =>
                    {
                        if (!ampBNFs.Contains(rec.AdditionalCode))
                            ampBNFs.Add(rec.AdditionalCode);

                        var sevenCharCode = rec.AdditionalCode.Length >= 7 ? rec.AdditionalCode.Substring(0, 7) : rec.AdditionalCode;// null;

                        if (sevenCharCode != null && !ampBNFsSevenChars.Contains(sevenCharCode))
                            ampBNFsSevenChars.Add(sevenCharCode);
                    });

                //Added to delete the unmapped (null fk records in additinal codes table)
                //ampAtcs?.Each(rec => _unitOfWork.FormularyAdditionalCodeFormularyRepository?.Remove(rec));

                ampAtcs?.Each(rec => amp.FormularyAdditionalCode.Remove(rec));
            }

            /* Change for MMC-455
             * Need to add for both ATC and BNF
            if (vmp.FormularyAdditionalCode.IsCollectionValid())
            {
                var vmpAtcs = vmp.FormularyAdditionalCode.Where(rec => string.Compare(rec.AdditionalCodeSystem, "atc", true) == 0);

                if (vmpAtcs.IsCollectionValid())
                {
                    foreach (var vmpAtc in vmpAtcs)
                    {
                        var ampATC = _mapper.Map<FormularyAdditionalCode>(vmpAtc);//Cloning
                        ampATC.FormularyVersionId = amp.FormularyVersionId;
                        ampATC.RowId = null;//Remove the primary key - should not get copied from vmp

                        amp.FormularyAdditionalCode.Add(ampATC);
                    }
                }
            }*/
            //Change for MMC-455
            if (vmp.FormularyAdditionalCode.IsCollectionValid())
            {
                var vmpAtcBnfs = vmp.FormularyAdditionalCode.Where(rec => string.Compare(rec.AdditionalCodeSystem, "bnf", true) == 0 || string.Compare(rec.AdditionalCodeSystem, "atc", true) == 0).ToList();

                if (vmpAtcBnfs.IsCollectionValid())
                {
                    foreach (var vmpAtcBNF in CollectionsMarshal.AsSpan(vmpAtcBnfs))
                    {
                        var sevenCharBNFFRomVMP = vmpAtcBNF.AdditionalCode == null ? null : (vmpAtcBNF.AdditionalCode.Length >= 7 ? vmpAtcBNF.AdditionalCode.Substring(0, 7) : vmpAtcBNF.AdditionalCode);

                        if ((string.Compare(vmpAtcBNF.AdditionalCodeSystem, "bnf", true) == 0 && vmpAtcBNF.AdditionalCode != null) && ((ampBNFs.IsCollectionValid() &&
                             ampBNFs.Contains(vmpAtcBNF.AdditionalCode)) || (sevenCharBNFFRomVMP != null && ampBNFsSevenChars.IsCollectionValid() && ampBNFsSevenChars.Contains(sevenCharBNFFRomVMP))))
                            continue;

                        /*
                        var ampATCBNF = _mapper.Map<FormularyAdditionalCode>(vmpAtcBNF);//Cloning
                        ampATCBNF.FormularyVersionId = amp.FormularyVersionId;
                        ampATCBNF.RowId = null;//Remove the primary key - should not get copied from vmp
                        */
                        FormularyAdditionalCode ampATCBNF = new()
                        {
                            FormularyVersionId = amp.FormularyVersionId,
                            AdditionalCode = vmpAtcBNF.AdditionalCode,
                            AdditionalCodeDesc = vmpAtcBNF.AdditionalCodeDesc,
                            AdditionalCodeSystem = vmpAtcBNF.AdditionalCodeSystem,
                            Attr1 = vmpAtcBNF.Attr1,
                            CodeType = vmpAtcBNF.CodeType,
                            MetaJson = vmpAtcBNF.MetaJson,
                            Source = vmpAtcBNF.Source,
                        };
                        amp.FormularyAdditionalCode.Add(ampATCBNF);
                    }
                }
            }
        }

        private void PopulateFormularyIngredientsForAMPFromVMP(FormularyHeader amp, FormularyHeader vmp)
        {
            amp.FormularyIngredient = amp.FormularyIngredient ?? new List<FormularyIngredient>();

            //Added to delete the unmapped (null fk records in ingredients table)
            //amp.FormularyIngredient?.Each(rec => _unitOfWork.FormularyIngredientFormularyRepository?.Remove(rec));

            amp.FormularyIngredient?.Clear();

            if (vmp.FormularyIngredient.IsCollectionValid())
            {
                var vmpIngredients = vmp.FormularyIngredient.ToList();

                foreach (var ing in CollectionsMarshal.AsSpan(vmpIngredients))
                {
                    //var ampIng = _mapper.Map<FormularyIngredient>(ing);//Cloning
                    //ampIng.FormularyVersionId = amp.FormularyVersionId;
                    //ampIng.RowId = null;//Remove the primary key - should not get copied from vmp
                    FormularyIngredient ampIng = new()
                    {
                        BasisOfPharmaceuticalStrengthCd = ing.BasisOfPharmaceuticalStrengthCd,
                        BasisOfPharmaceuticalStrengthDesc = ing.BasisOfPharmaceuticalStrengthDesc,
                        IngredientCd = ing.IngredientCd,
                        IngredientName = ing.IngredientName,
                        FormularyVersionId = amp.FormularyVersionId,
                        StrengthValueDenominator = ing.StrengthValueDenominator,
                        StrengthValueDenominatorUnitCd = ing.StrengthValueDenominatorUnitCd,
                        StrengthValueDenominatorUnitDesc = ing.StrengthValueDenominatorUnitDesc,
                        StrengthValueNumerator = ing.StrengthValueNumerator,
                        StrengthValueNumeratorUnitCd = ing.StrengthValueNumeratorUnitCd,
                        StrengthValueNumeratorUnitDesc = ing.StrengthValueNumeratorUnitDesc
                    };

                    amp.FormularyIngredient.Add(ampIng);
                }
            }
        }

        private void PopulateFormularyUnlicensedRouteForAMPFromVMP(FormularyHeader amp, FormularyHeader vmp)
        {
            var vmpRoutes = vmp.FormularyRouteDetail;
            var ampRoutes = amp.FormularyRouteDetail;

            var ampRouteCds = ampRoutes?.Select(rec => rec.RouteCd);
            var vmpRoutesCds = vmpRoutes?.Select(rec => rec.RouteCd);

            var diffCds = vmpRoutesCds?.Except(ampRouteCds ?? new List<string>())?.ToList();

            //if no diffcds then no unlicensed
            if (!diffCds.IsCollectionValid()) return;

            //if (ampRoutes != null && ampRoutes.Count == 0 && vmpRoutes.IsCollectionValid())
            //{
            //foreach (var vmpRoute in vmpRoutes)
            var newRouteCodes = amp.FormularyRouteDetail?.Select(rec => rec.RouteCd).Distinct().ToHashSet();
            var lookupProvider = getDMDLookupProvider();
            var prevCodeRoutesLkp = lookupProvider?._routesLkpWithAllAttributesForPrevCode;

            foreach (var vmpRouteCd in CollectionsMarshal.AsSpan(diffCds))
            {
                var vmpRoute = vmpRoutes.First(rec => rec.RouteCd == vmpRouteCd);

                if (vmpRoute == null || vmpRoute.RouteCd == null) continue;
                //check whether the same or new code added in licensed already
                if (newRouteCodes.IsCollectionValid() && newRouteCodes.Contains(vmpRoute.RouteCd)) continue;
                if (prevCodeRoutesLkp.IsCollectionValid() && prevCodeRoutesLkp.ContainsKey(vmpRoute.RouteCd))
                {
                    var newCd = prevCodeRoutesLkp[vmpRoute.RouteCd].Cd;
                    if (newCd != null && newRouteCodes.IsCollectionValid() && newRouteCodes.Contains(newCd))
                        continue;
                }

                FormularyRouteDetail unlicensedRoute = new();

                unlicensedRoute.FormularyVersionId = amp.FormularyVersionId;
                unlicensedRoute.RouteCd = vmpRoute.RouteCd;
                unlicensedRoute.RouteDesc = vmpRoute.RouteDesc;
                unlicensedRoute.RouteFieldTypeCd = TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED;

                amp.FormularyRouteDetail.Add(unlicensedRoute);
            }
            //}
        }

        private void SyncAMPLicensedAndUnLicensedRoutes(FormularyHeader amp)
        {
            amp.FormularyLocalRouteDetail = amp.FormularyLocalRouteDetail ?? new List<FormularyLocalRouteDetail>();
            var ampLicensedRoutes = amp.FormularyRouteDetail?.Where(rec => rec.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL);
            var ampLocalLicensedRoutes = amp.FormularyLocalRouteDetail?.Where(rec => rec.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL);

            var ampUnlicensedRoutes = amp.FormularyRouteDetail?.Where(rec => rec.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED);
            var ampLocalUnlicensedRoutes = amp.FormularyLocalRouteDetail?.Where(rec => rec.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED);

            var ampLicRouteCds = ampLicensedRoutes?.Select(rec => rec.RouteCd);
            var ampLocalLicRoutesCds = ampLocalLicensedRoutes?.Select(rec => rec.RouteCd) ?? new List<string>();

            var ampUnlicRouteCds = ampUnlicensedRoutes?.Select(rec => rec.RouteCd);
            var ampLocalUnlicRoutesCds = ampLocalUnlicensedRoutes?.Select(rec => rec.RouteCd) ?? new List<string>();

            var diffLicCds = ampLicRouteCds?.Except(ampLocalLicRoutesCds ?? new List<string>())?.ToList();
            var diffUnlicCds = ampUnlicRouteCds?.Except(ampLocalUnlicRoutesCds ?? new List<string>())?.ToList();

            AddToLocalRoute(diffLicCds, amp, TerminologyConstants.ROUTEFIELDTYPE_NORMAL, ampLicensedRoutes);
            AddToLocalRoute(diffUnlicCds, amp, TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED, ampUnlicensedRoutes);

            PurgeOldCodeIfBothOldAndNewLocalRouteCodeExists(amp);
        }

        private void PurgeOldCodeIfBothOldAndNewLocalRouteCodeExists(FormularyHeader amp)
        {
            if (!amp.FormularyLocalRouteDetail.IsCollectionValid()) return;

            var lookupProvider = getDMDLookupProvider();

            if (lookupProvider == null || !lookupProvider._routesLkpWithAllAttributesForPrevCode.IsCollectionValid()) return;

            var ampLocalUnlicensedRoutes = amp.FormularyLocalRouteDetail.Where(rec => rec.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED)?.ToList();
            var ampLocalLicensedRoutes = amp.FormularyLocalRouteDetail.Where(rec => rec.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL)?.ToList();

            var ampLocalUnlicensedRoutesCodesDeleteMarker = new HashSet<string>();
            var ampLocalLicensedRoutesCodesDeleteMarker = new HashSet<string>();

            FillLocalRoutesDeleteMarkers(ampLocalUnlicensedRoutes, lookupProvider, ampLocalUnlicensedRoutesCodesDeleteMarker);
            FillLocalRoutesDeleteMarkers(ampLocalLicensedRoutes, lookupProvider, ampLocalLicensedRoutesCodesDeleteMarker);

            if (!ampLocalUnlicensedRoutesCodesDeleteMarker.IsCollectionValid() && !ampLocalLicensedRoutesCodesDeleteMarker.IsCollectionValid()) 
                return;
            var unLicenseRoutesFiltered = new List<FormularyLocalRouteDetail>();
            var licenseRoutesFiltered = new List<FormularyLocalRouteDetail>();

            FillValidLocalRoutes(ampLocalUnlicensedRoutes, unLicenseRoutesFiltered, ampLocalUnlicensedRoutesCodesDeleteMarker);
            
            FillValidLocalRoutes(ampLocalLicensedRoutes, licenseRoutesFiltered, ampLocalLicensedRoutesCodesDeleteMarker);

            amp.FormularyLocalRouteDetail.Clear();

            var licAdded = new HashSet<string>();
            var unLicAdded = new HashSet<string>();
            if (licenseRoutesFiltered.IsCollectionValid())
            {
                licenseRoutesFiltered.Each(rec =>
                {
                    if (rec.RouteCd != null && !licAdded.Contains(rec.RouteCd))
                    {
                        licAdded.Add(rec.RouteCd);
                        amp.FormularyLocalRouteDetail.Add(rec);
                    }
                });
            }
            if (unLicenseRoutesFiltered.IsCollectionValid())
            {
                var newRouteCodes = amp.FormularyLocalRouteDetail?.Select(rec => rec.RouteCd).Distinct().ToHashSet();
                foreach (var rec in unLicenseRoutesFiltered)
                {
                    if (rec == null || rec.RouteCd == null) continue;
                    //check whether the same or new code added in licensed already
                    if (newRouteCodes.IsCollectionValid() && newRouteCodes.Contains(rec.RouteCd)) continue;
                    if (lookupProvider._routesLkpWithAllAttributesForPrevCode.ContainsKey(rec.RouteCd))
                    {
                        var newCd = lookupProvider._routesLkpWithAllAttributesForPrevCode[rec.RouteCd].Cd;
                        if (newCd != null && newRouteCodes.IsCollectionValid() && newRouteCodes.Contains(newCd))
                            continue;
                    }
                    if (unLicAdded.Contains(rec.RouteCd)) continue;
                    unLicAdded.Add(rec.RouteCd);
                    amp.FormularyLocalRouteDetail.Add(rec);
                }
            }
        }

        private void FillValidLocalRoutes(List<FormularyLocalRouteDetail>? ampLocalUnlicensedRoutes, List<FormularyLocalRouteDetail> unLicenseRoutesFiltered, HashSet<string> ampLocalUnlicensedRoutesCodesDeleteMarker)
        {
            if (!ampLocalUnlicensedRoutes.IsCollectionValid()) return;
            if (!ampLocalUnlicensedRoutesCodesDeleteMarker.IsCollectionValid())
            {
                ampLocalUnlicensedRoutes.Each(rec => unLicenseRoutesFiltered.Add(rec));
                return;
            }

            foreach (var unLicRoute in ampLocalUnlicensedRoutes)
            {
                if (unLicRoute.RouteCd != null && !ampLocalUnlicensedRoutesCodesDeleteMarker.Contains(unLicRoute.RouteCd))
                    unLicenseRoutesFiltered.Add(unLicRoute);
            }
        }

        private void FillLocalRoutesDeleteMarkers(List<FormularyLocalRouteDetail>? ampLocalUnlicensedRoutes, DMDLookupProvider lookupProvider, HashSet<string> ampLocalUnlicensedRoutesCodesDeleteMarker)
        {
            if (!ampLocalUnlicensedRoutes.IsCollectionValid()) return;
            var ampLocalUnlicensedRoutesCodes = ampLocalUnlicensedRoutes
                .Where(rec => rec.RouteCd.IsNotEmpty())?
                .Select(rec => rec.RouteCd)
                .Distinct().ToHashSet();

            if (!ampLocalUnlicensedRoutesCodes.IsCollectionValid()) return;
            foreach (var localUnLicRouteCd in ampLocalUnlicensedRoutesCodes)
            {
                if (!lookupProvider._routesLkpWithAllAttributesForPrevCode.ContainsKey(localUnLicRouteCd)) continue;
                var currentCd = lookupProvider._routesLkpWithAllAttributesForPrevCode[localUnLicRouteCd]?.Cd;
                if (currentCd == null || currentCd == localUnLicRouteCd) continue;
                if (!ampLocalUnlicensedRoutesCodes.Contains(currentCd)) continue;
                if (ampLocalUnlicensedRoutesCodesDeleteMarker.Contains(localUnLicRouteCd)) continue;
                ampLocalUnlicensedRoutesCodesDeleteMarker.Add(localUnLicRouteCd);
            }
        }

        private void AddToLocalRoute(List<string?>? diffCds, FormularyHeader amp, string ROUTEFIELDTYPE, IEnumerable<FormularyRouteDetail>? ampRoutes)
        {
            //if diffcds then sync
            if (!diffCds.IsCollectionValid() || amp == null || !ampRoutes.IsCollectionValid()) return;

            amp.FormularyLocalRouteDetail = amp.FormularyLocalRouteDetail ?? new List<FormularyLocalRouteDetail>();

            foreach (var ampRouteCd in CollectionsMarshal.AsSpan(diffCds))
            {
                var ampRoute = ampRoutes.First(rec => rec.RouteCd == ampRouteCd);

                FormularyLocalRouteDetail localroute = new();
                localroute.FormularyVersionId = amp.FormularyVersionId;
                localroute.RouteCd = ampRoute.RouteCd;
                localroute.RouteFieldTypeCd = ROUTEFIELDTYPE;
                localroute.RouteDesc = ampRoute.RouteDesc;
                localroute.Source = TerminologyConstants.MANUAL_DATA_SRC;
                amp.FormularyLocalRouteDetail.Add(localroute);
            }
        }


        private void SyncAMPLicensedAndUnLicensedUse(FormularyDetail formularyDetail)
        {
            var ampLicensedUses = GetFormularyLookupItemFromString(formularyDetail.LicensedUse);
            var ampUnLicensedUses = GetFormularyLookupItemFromString(formularyDetail.UnlicensedUse);

            var ampLocalLicensedUses = GetFormularyLookupItemFromString(formularyDetail.LocalLicensedUse)?.ToList() ?? new List<FormularyLookupItemDTO>();

            var ampLocalUnLicensedUses = GetFormularyLookupItemFromString(formularyDetail.LocalUnlicensedUse)?.ToList() ?? new List<FormularyLookupItemDTO>();

            var ampLicUseCds = ampLicensedUses?.Select(rec => rec.Cd);
            var ampLocalLicUseCds = ampLocalLicensedUses?.Select(rec => rec.Cd) ?? new List<string>();

            var ampUnlicUseCds = ampUnLicensedUses?.Select(rec => rec.Cd);
            var ampLocalUnlicUseCds = ampLocalUnLicensedUses?.Select(rec => rec.Cd) ?? new List<string>();

            var diffLicCds = ampLicUseCds?.Except(ampLocalLicUseCds ?? new List<string>())?.ToList();
            var diffUnlicCds = ampUnlicUseCds?.Except(ampLocalUnlicUseCds ?? new List<string>())?.ToList();

            AddLocalUses(diffLicCds, formularyDetail, ampLicensedUses, ampLocalLicensedUses, true);
            AddLocalUses(diffUnlicCds, formularyDetail, ampUnLicensedUses, ampLocalUnLicensedUses, false);

            /* TBR
            //if diffcds then sync
            if (diffLicCds.IsCollectionValid())
            {
                foreach (var ampLicUseCd in diffLicCds)
                {
                    var ampLicUse = ampLicensedUses.First(rec => rec.Cd == ampLicUseCd);

                    FormularyLookupItemDTO localLicensedUse = new();
                    localLicensedUse.Cd = ampLicUse.Cd;
                    localLicensedUse.Desc = ampLicUse.Desc;
                    ampLocalLicensedUses?.Add(localLicensedUse);
                }
            }
            if (ampLocalLicensedUses.IsCollectionValid())
                formularyDetail.LocalLicensedUse = StringifyFDBCodeDescData(ampLocalLicensedUses);

            if (diffUnlicCds.IsCollectionValid())
            {
                foreach (var ampUnlicRouteCd in diffUnlicCds)
                {
                    var ampUnlicUse = ampUnLicensedUses.First(rec => rec.Cd == ampUnlicRouteCd);
                    FormularyLookupItemDTO UnlicensedUse = new();
                    UnlicensedUse.Cd = ampUnlicUse.Cd;
                    UnlicensedUse.Desc = ampUnlicUse.Desc;
                    ampLocalUnLicensedUses?.Add(UnlicensedUse);
                }
            }

            if (ampLocalUnLicensedUses.IsCollectionValid())
                formularyDetail.LocalUnlicensedUse = StringifyFDBCodeDescData(ampLocalUnLicensedUses);
            */
        }

        private void AddLocalUses(List<string>? diffCds, FormularyDetail formularyDetail, List<FormularyLookupItemDTO>? ampUses, List<FormularyLookupItemDTO>? ampLocalUses, bool isLicensed)
        {
            ampLocalUses = ampLocalUses ?? new List<FormularyLookupItemDTO>();

            //if diffcds then sync
            if (!diffCds.IsCollectionValid()) return;

            foreach (var ampUseCd in diffCds)
            {
                var ampUse = ampUses.First(rec => rec.Cd == ampUseCd);

                FormularyLookupItemDTO localUse = new();
                localUse.Cd = ampUse.Cd;
                localUse.Desc = ampUse.Desc;
                localUse.Source = TerminologyConstants.MANUAL_DATA_SRC;
                ampLocalUses?.Add(localUse);
            }

            if (ampLocalUses.IsCollectionValid())
            {
                if (isLicensed)
                    formularyDetail.LocalLicensedUse = StringifyFDBCodeDescData(ampLocalUses);
                else
                    formularyDetail.LocalUnlicensedUse = StringifyFDBCodeDescData(ampLocalUses);
            }
        }

        private async Task UpdateDMDDeletedStatusToFormularyIfDeleted()
        {
            List<string> recordsToBeMarkedDeleted = new();

            //the records that are only in deleted status will be marked deleted. Otherwise not.
            //var pendingDMDForFormularyImportResp = await _terminologyAPIService.GetDMDPendingSyncLogs();

            var pendingDMDForFormularyImport = new List<DmdSyncLog>();

            for (int syncLogIndex = 1; syncLogIndex < int.MaxValue; syncLogIndex++)
            {
                var pendingDMDForFormularyImportResp = await _terminologyAPIService.GetDMDPendingSyncLogsByPagination(syncLogIndex, 200);

                if (pendingDMDForFormularyImportResp != null && (pendingDMDForFormularyImportResp.StatusCode == StatusCode.Fail || (pendingDMDForFormularyImportResp.ErrorMessages.IsCollectionValid())))
                    return;

                if (pendingDMDForFormularyImportResp == null || pendingDMDForFormularyImportResp.Data == null || !pendingDMDForFormularyImportResp.Data.IsCollectionValid())
                    break;

                pendingDMDForFormularyImport.AddRange(pendingDMDForFormularyImportResp.Data);
            }

            if (!pendingDMDForFormularyImport.IsCollectionValid()) return;

            recordsToBeMarkedDeleted.AddRange(await GetEntitiesToDelete(pendingDMDForFormularyImport, "dmd_vtm"));
            recordsToBeMarkedDeleted.AddRange(await GetEntitiesToDelete(pendingDMDForFormularyImport, "dmd_vmp"));

            recordsToBeMarkedDeleted.AddRange(await GetEntitiesToDelete(pendingDMDForFormularyImport, "dmd_amp"));

            if (!recordsToBeMarkedDeleted.IsCollectionValid()) return;

            var (scope, unitOfWork) = GetUoWInNewScope();

            //for amp's mark only for latest drafts and for others (vtm and vmp) latest (will have only 'active' by default)
            //var formularies = unitOfWork.FormularyHeaderFormularyRepository.Items.Where(rec => rec.IsLatest == true && recordsToBeMarkedDeleted.Contains(rec.Code))?.ToList();

            var formularies = unitOfWork.FormularyHeaderFormularyRepository.Items.Where(rec => rec.IsLatest == true && recordsToBeMarkedDeleted.Contains(rec.FormularyId))?.ToList();

            if (!formularies.IsCollectionValid()) return;

            await UpdateRecordsAsDMDDeleted(formularies, "amp", new List<string> { TerminologyConstants.RECORDSTATUS_DRAFT }, unitOfWork);
            await UpdateRecordsAsDMDDeleted(formularies, "vmp", new List<string> { TerminologyConstants.RECORDSTATUS_ACTIVE }, unitOfWork);
            await UpdateRecordsAsDMDDeleted(formularies, "vmp", new List<string> { TerminologyConstants.RECORDSTATUS_ACTIVE }, unitOfWork);

            unitOfWork.FormularyHeaderFormularyRepository.SaveChanges();

            DisposeUoWWithScope(scope, unitOfWork);
        }

        private async Task UpdateRecordsAsDMDDeleted(List<FormularyHeader>? fomularies, string productType, List<string> recstatus, IUnitOfWork unitOfWork)
        {
            if (!fomularies.IsCollectionValid()) return;

            var recs = fomularies?.Where(rec => rec.IsLatest == true && rec.RecStatusCode != null && string.Compare(rec.ProductType, productType, true) == 0 && recstatus.Contains(rec.RecStatusCode))?.ToList();

            if (!recs.IsCollectionValid()) return;

            //MMC-477-FormularyId changes - Not needed anymore
            /*
            if (string.Compare(productType, "amp", true) == 0)
            {
                //if there are amps, check and create draft if no draft version exists in local formulary for these amps.
                //The draft version of the amp will be marked as delete and for the vmp and vtm it is for the 'active'
                var codes = fomularies.Select(rec => rec.Code)?.Distinct().ToList();

                //create new draft amp is not exists
                //Assumption: it is assumed that if the VTM or VMP gets deleted then the child records of these also comes as deleted - so not handling that
                await FindAndCreateMissingAMPDraftsForParents(codes);//passing only amps
            }*/

            recs?.Each(rec =>
            {
                rec.IsDmdDeleted = true;
                unitOfWork.FormularyHeaderFormularyRepository.Update(rec);
            });
        }

        private async Task<List<string>> GetEntitiesToDelete(List<DataService.APIModels.DmdSyncLog> data, string entityName)
        {
            List<string> recordsToBeMarkedDeleted = new();

            //select all vtms, vmps or amps (entityName param) that have only delete action but no insert or update action
            var deletedEntities = data.Where(rec => string.Compare(rec.DmdEntityName, entityName, true) == 0 && string.Compare(rec.RowAction, "d", true) == 0)?.ToList();

            var deletedEntityCodes = deletedEntities?.Select(rec => rec.DmdId)?.ToList();

            //if no deleted records - no action
            if (deletedEntityCodes.IsCollectionValid())
            {
                var recordsToDelete = deletedEntityCodes;

                var insOrUpdEntity = data.Where(rec => string.Compare(rec.DmdEntityName, entityName, true) == 0 && ((string.Compare(rec.RowAction, "i", true) == 0) || (string.Compare(rec.RowAction, "u", true) == 0)))?.ToList();

                var insertedOrUpdatedVTMsCodes = insOrUpdEntity?.Select(rec => rec.DmdId)?.ToList();

                recordsToDelete = deletedEntityCodes.Except(insertedOrUpdatedVTMsCodes ?? new List<string>())?.ToList();

                recordsToBeMarkedDeleted.AddRange(recordsToDelete ?? new List<string>());
            }

            //MMC-477 - FormularyId changes
            if (recordsToBeMarkedDeleted.IsCollectionValid())
            {
                //MMC-477 - FormularyId changes
                var recordsWithFormularyIdsToBeMarkedDeleted = await GetLatestFormularyIdsForCodes(recordsToBeMarkedDeleted);
                if (recordsWithFormularyIdsToBeMarkedDeleted.IsCollectionValid())
                {
                    recordsToBeMarkedDeleted.Clear();
                    recordsWithFormularyIdsToBeMarkedDeleted.Each(rec => recordsToBeMarkedDeleted.Add(rec.Value));
                }
            }

            return recordsToBeMarkedDeleted;
        }

        private List<FormularyLookupItemDTO> GetFormularyLookupItemFromString(string dataAsString)
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

        private string StringifyFDBCodeDescData(List<FormularyLookupItemDTO> codeDescList)
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

        private void UpdateRecordsOfPrevCodes()
        {
            //get the latest records - vmps and vtms (amps will not have prev) with prevcodes
            //check if those prev codes exists in local formulary and set the is latest of those to false

            var (scope, unitOfWork) = GetUoWInNewScope();
            var prevCodes = unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => rec.Prevcode != null && rec.Prevcode != "" && rec.Code != rec.Prevcode && rec.IsLatest == true)?
                .Select(rec => rec.Prevcode)
                .ToList();

            DisposeUoWWithScope(scope, unitOfWork);

            if (!prevCodes.IsCollectionValid()) return;

            var batchsize = 100;

            var batchedRequestsForStatusUpdate = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < prevCodes.Count; reqIndex += batchsize)
            {
                var batches = prevCodes.Skip(reqIndex).Take(batchsize);
                batchedRequestsForStatusUpdate.Add(batches.ToList());
            }

            foreach (var batchedRequest in batchedRequestsForStatusUpdate)
            {
                var (scopeInner, unitOfWorkInner) = GetUoWInNewScope();

                var headerWithPrevCodes = unitOfWorkInner.FormularyHeaderFormularyRepository.Items
                .Where(rec => prevCodes.Contains(rec.Code) && rec.IsLatest == true)?
                .ToList();

                if (!headerWithPrevCodes.IsCollectionValid()) continue;

                headerWithPrevCodes.Each(rec =>
                {
                    rec.IsLatest = false;
                    unitOfWorkInner.FormularyHeaderFormularyRepository.Update(rec);
                });

                unitOfWorkInner.FormularyHeaderFormularyRepository.SaveChanges();

                DisposeUoWWithScope(scopeInner, unitOfWorkInner);
            }
        }


        private async Task BuildHierarchyForNewNodes(List<string> codes)
        {
            if (!codes.IsCollectionValid()) return;

            var uniqueCodes = codes.Where(rec => rec.IsNotEmpty()).Distinct().ToList();

            if (!uniqueCodes.IsCollectionValid()) return;

            var codeProductTypeLookup = GetProdTypesForCodesAsLkp(uniqueCodes);

            if (!codeProductTypeLookup.IsCollectionValid()) return;


            //Go top-down first and if parent has changed, then bring all its children and hence should be imported and associated.
            //So when a parent is imported, a new version of the parent and its children (all its descendents) will be created.
            //Then,Go bottom up and associate the new child nodes with the latest version of the parent node

            //Rules Applied:
            //Top-down:
            //if parent has changed, then bring all its children and hence should be imported and associated. (and added to the input 'codes')
            //Bottom-up: (after applying top-down and association is done.)
            //1. When a new 'AMP' has alone come (not it's VMP or VTM - and also not in the final list of 'codes' in 'BuildHierarchyForNewNodes' fn) then bring (no need to re-import but just find the VMP Code) and add the parent 'VMP' (latest FormularyId of that) to the input 'codes'. This is done since this 'AMP' should inherit from its parent.
            //2. When a new 'VMP' has alone come (but not it's VTM - Note: child will be imported and created in 'BuildHierarchyForNewNodes') then no need to bring the VTM (and no need to add to 'codes' input list) as impact on VTM from VMP is less.

            var newAMPCodes = uniqueCodes.Where(rec => codeProductTypeLookup.ContainsKey(rec) && codeProductTypeLookup[rec] == TerminologyConstants.PRODUCT_TYPE_AMP).ToList() ?? new List<string>();

            var newVMPCodes = uniqueCodes.Where(rec => codeProductTypeLookup.ContainsKey(rec) && codeProductTypeLookup[rec] == TerminologyConstants.PRODUCT_TYPE_VMP).ToList() ?? new List<string>();

            var newVTMCodes = uniqueCodes.Where(rec => codeProductTypeLookup.ContainsKey(rec) && codeProductTypeLookup[rec] == TerminologyConstants.PRODUCT_TYPE_VTM).ToList() ?? new List<string>();

            var vmpsToReImport = GetChildrenToReImport(newVTMCodes, newVMPCodes);
            if (vmpsToReImport.IsCollectionValid())
            {
                //This will add to the 'codes' as these will be re-imported again.
                codes.AddRange(vmpsToReImport);
                newVMPCodes.AddRange(vmpsToReImport);
                await ReImportNodes(vmpsToReImport);
            }

            var ampsToReImport = GetChildrenToReImport(newVMPCodes, newAMPCodes);

            if (ampsToReImport.IsCollectionValid())
            {
                //This will add to the 'codes' as these will be re-imported again.
                codes.AddRange(ampsToReImport);
                newAMPCodes.AddRange(ampsToReImport);
                await ReImportNodes(ampsToReImport);
            }

            await AssociateNewChildCodesWithLatestParentFormularyId(newAMPCodes);
            await AssociateNewChildCodesWithLatestParentFormularyId(newVMPCodes);

            //Rules Applied: (Bottom-up) after applying top-down
            //1. When a new 'AMP' has alone come (not it's VMP or VTM - and also not in the final list of 'codes' in 'BuildHierarchyForNewNodes' fn) then bring (no need to re-import but just find the VMP Code) and add the parent 'VMP' (latest FormularyId of that) to the input 'codes'. This is done since this 'AMP' should inherit from its parent.
            //2. When a new 'VMP' has alone come (but not it's VTM - Note: child will be imported and created in 'BuildHierarchyForNewNodes') then no need to bring the VTM (and no need to add to 'codes' input list) as impact on VTM from VMP is less.

            //Applying rules:
            var vmpFormularyIdWithVMPCodesAsLkp = GetParentFormularyIdWithItsCodeOfNewCodesAsLookup(newAMPCodes);

            if(vmpFormularyIdWithVMPCodesAsLkp.IsCollectionValid())
            {
                var vmpCodesForNewAMPCodes = vmpFormularyIdWithVMPCodesAsLkp.Values.Select(rec => rec.Code).ToList();
                var vmpsNotInCodes = vmpCodesForNewAMPCodes.Except(codes).ToList();
                if (vmpsNotInCodes.IsCollectionValid())
                    codes.AddRange(vmpsNotInCodes);
            }
            //No need to VMP (Rule 2)

            //for thes amp codes - archive the old 'Drafts' with same code and in the same tree
            ArchiveOlderDraftsOfAMPs(newAMPCodes);
        }

        private void ArchiveOlderDraftsOfAMPs(List<string> newAMPCodes)
        {
            if (!newAMPCodes.IsCollectionValid()) return;

            var otherFormularyIdsToArchive = GetAMPFormularyIdsTobeArchived(newAMPCodes);

            if (!otherFormularyIdsToArchive.IsCollectionValid()) return;

            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            foreach (var otherFormularyIdToArchive in otherFormularyIdsToArchive)
            {
                //clone and create an archive of the old draft
                var tobeCloned = unitOfWorkInnerA.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking(true)
                    .Where(rec => rec.FormularyId == otherFormularyIdToArchive && rec.IsLatest == true && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)
                    .FirstOrDefault();

                if (tobeCloned != null)
                {
                    //this increments the existing version during clone
                    var cloned = _formularyUtil.CloneFormulary(tobeCloned);
                    cloned.IsLatest = true;//archived always true
                    cloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
                    unitOfWorkInnerA.FormularyHeaderFormularyRepository.Add(cloned);
                }
            }

            unitOfWorkInnerA.FormularyHeaderFormularyRepository.Items
            .Where(rec => otherFormularyIdsToArchive.Contains(rec.FormularyId) && rec.IsLatest == true && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)
            .Each(rec =>
            {
                rec.IsLatest = false;
            });

            unitOfWorkInnerA.FormularyHeaderFormularyRepository.SaveChanges();


            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);
        }

        private List<string> GetAMPFormularyIdsTobeArchived(List<string> newAMPCodes)
        {
            if (!newAMPCodes.IsCollectionValid()) return null;
            
            newAMPCodes = newAMPCodes.Where(rec=> rec.IsNotEmpty()).ToList();
            
            if (!newAMPCodes.IsCollectionValid()) return null;


            var newFVIdWithAmpCodeAsLkp = GetFVIdsOfNewCodesAsLookup(newAMPCodes);

            if (!newFVIdWithAmpCodeAsLkp.IsCollectionValid()) return null;

            var fvIdsAskeys = newFVIdWithAmpCodeAsLkp.Keys;

            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            //Get all missing VMPs and add to the 'codes' list
            var currentAMPDetails = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => fvIdsAskeys.Contains(rec.FormularyVersionId))
                ?.Select(rec => new { rec.Code, rec.FormularyId })
                .ToList();

            var allAMPsInDraftForSameCode = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => newAMPCodes.Contains(rec.Code) && rec.IsLatest == true && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)
                ?.Select(rec => new { rec.Code, rec.FormularyId })
                .ToList();

            if (!allAMPsInDraftForSameCode.IsCollectionValid()) return null;

            if (!currentAMPDetails.IsCollectionValid())
                return allAMPsInDraftForSameCode.Where(rec=> rec.FormularyId.IsNotEmpty())?.Select(rec => rec.FormularyId).ToList();


            var currentAMPDetailsAsLkp = new Dictionary<string, List<string>>();

            currentAMPDetails.Each(rec =>
            {
                if(!currentAMPDetailsAsLkp.ContainsKey(rec.Code))
                    currentAMPDetailsAsLkp[rec.Code] = new List<string>();

                currentAMPDetailsAsLkp[rec.Code].Add(rec.FormularyId);
            });

            var allAMPsInDraftForSameCodeAsLkp = new Dictionary<string, List<string>>();
            allAMPsInDraftForSameCode.Each(rec =>
            {
                if (!allAMPsInDraftForSameCodeAsLkp.ContainsKey(rec.Code))
                    allAMPsInDraftForSameCodeAsLkp[rec.Code] = new List<string>();

                allAMPsInDraftForSameCodeAsLkp[rec.Code].Add(rec.FormularyId);
            });

            var otherFormularyIdsToArchive = new List<string>();
            //If for the same AMP Code, there are other 'Drafts' with latest - then move it to archived
            if(currentAMPDetailsAsLkp.IsCollectionValid())
            {
                foreach (var currentAMPDetailsKey in currentAMPDetailsAsLkp.Keys)
                {
                    var newAMPsFVIds = currentAMPDetailsAsLkp[currentAMPDetailsKey];
                    if (!newAMPsFVIds.IsCollectionValid()) continue;

                    var existingAMPsFVIds = allAMPsInDraftForSameCodeAsLkp.ContainsKey(currentAMPDetailsKey) ? allAMPsInDraftForSameCodeAsLkp[currentAMPDetailsKey] : null;
                    if (!existingAMPsFVIds.IsCollectionValid()) continue;

                    var otherFormularyIdsOfThisAMP = existingAMPsFVIds.Except(newAMPsFVIds).ToList();

                    if (!otherFormularyIdsOfThisAMP.IsCollectionValid()) continue;

                    otherFormularyIdsOfThisAMP = otherFormularyIdsOfThisAMP.Distinct().ToList();
                    if (!otherFormularyIdsOfThisAMP.IsCollectionValid()) continue;

                    //these 'otherFormularyIds' should be archived
                    if (otherFormularyIdsOfThisAMP.IsCollectionValid())
                        otherFormularyIdsToArchive.AddRange(otherFormularyIdsOfThisAMP);
                }
            }

            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

            return otherFormularyIdsToArchive;
        }

        private List<string> GetAMPFormularyIdsTobeArchived_Old_refOnly(List<string> newAMPCodes)
        {
            var newFVIdWithAmpCodeAsLkp = GetFVIdsOfNewCodesAsLookup(newAMPCodes);

            if (!newFVIdWithAmpCodeAsLkp.IsCollectionValid()) return null;

            var fvIdsAskeys = newFVIdWithAmpCodeAsLkp.Keys;

            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            //Get all missing VMPs and add to the 'codes' list
            var currentAMPDetails = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => fvIdsAskeys.Contains(rec.FormularyVersionId))
                ?.Select(rec => new { rec.Code, rec.VersionId, rec.ParentFormularyId, rec.FormularyId, rec.FormularyVersionId, rec.Createdtimestamp })
                .ToList();

            var currentAMPParentFormularyIds = currentAMPDetails.Select(rec => rec.ParentFormularyId).ToList();

            var allAMPsInDraftForSameParent = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => currentAMPParentFormularyIds.Contains(rec.ParentFormularyId) && rec.IsLatest == true && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)
                ?.Select(rec => new { rec.Code, rec.VersionId, rec.ParentFormularyId, rec.FormularyId, rec.FormularyVersionId, rec.Createdtimestamp })
                .ToList();

            var otherFormularyIdsToArchive = new List<string>();
            //If for the same AMP Code, there are other 'Drafts' with latest - then move it to archived
            foreach (var currentAMP in currentAMPDetails)
            {
                var allFormularyIdsOfSameAMPCodeForParent = allAMPsInDraftForSameParent
                    .Where(rec => rec.Code == currentAMP.Code && rec.ParentFormularyId == currentAMP.ParentFormularyId)
                    .Select(rec => rec.FormularyId)
                    .ToList();

                var otherFormularyIdsOfThisAMP = allFormularyIdsOfSameAMPCodeForParent.Except(new List<string> { currentAMP.FormularyId }).ToList();

                //these 'otherFormularyIds' should be archived
                if (otherFormularyIdsOfThisAMP.IsCollectionValid())
                    otherFormularyIdsToArchive.AddRange(otherFormularyIdsOfThisAMP);
            }

            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

            return otherFormularyIdsToArchive;
        }

        private async Task ReImportNodes(List<string> vmpsToReImport)
        {
            if (!vmpsToReImport.IsCollectionValid()) return;
            var batchSize = 100;

            var batchedRequests = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < vmpsToReImport.Count; reqIndex += batchSize)
            {
                var batches = vmpsToReImport.Skip(reqIndex).Take(batchSize);
                batchedRequests.Add(batches.ToList());
            }

            foreach (var batch in batchedRequests)
            {
                var formularyImportHandler = _serviceProvider.GetService<FormularyImportHandler>();
                formularyImportHandler.getDMDLookupProvider = this.getDMDLookupProvider;
                await formularyImportHandler.ImportByCodes(batch.ToList());
            }
        }

        private Dictionary<string, string> GetProdTypesForCodesAsLkp(List<string> uniqueCodes)
        {
            var codeProductTypeLookup = new Dictionary<string, string>();

            CommonUtil.ProcessInBatch(uniqueCodes, 100, (batch) => {
                var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

                var codeProductTypeLookupTemp = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                    .Where(rec => rec.Code != null && batch.Contains(rec.Code))
                    ?.Select(rec => new { rec.Code, rec.ProductType })
                    .Distinct(rec => rec.Code)
                    .ToDictionary(k => k.Code, v => v.ProductType);

                DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

                if (codeProductTypeLookupTemp.IsCollectionValid())
                {
                    codeProductTypeLookupTemp.Keys.Each(k => codeProductTypeLookup[k] = codeProductTypeLookupTemp[k]);
                }
            });

            return codeProductTypeLookup;
        }

        private new List<string> GetChildrenToReImport(List<string> newParentTypeCodes, List<string> newChildProductTypeCodes)
        {
            //Note: Comment is based on Parent=VTM and Child=VMP
            var childrenToReImport = new List<string>();

            //top down from vtm to vmp - associate the missing latest child for these
            if (!newParentTypeCodes.IsCollectionValid())
                return childrenToReImport;

            var allParentsForCode = new List<dynamic>();

            CommonUtil.ProcessInBatch(newParentTypeCodes, 100, (batch) =>
            {
                var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

                //Get all missing VMPs and add to the 'codes' list
                var allParentsForCodeTemp = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                    .Where(rec => batch.Contains(rec.Code))
                    ?.Select(rec => new { rec.Code, rec.VersionId, rec.FormularyId, rec.FormularyVersionId, rec.Createdtimestamp })
                    .ToList();

                DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

                if (allParentsForCodeTemp.IsCollectionValid())
                    allParentsForCode.AddRange(allParentsForCodeTemp);
            });

            //Select the just previous version of this VTM code in system (if exists)
            if (!allParentsForCode.IsCollectionValid())
                return childrenToReImport;

            var parentFormularyIdsToLookFor = new List<string>();
            foreach (var parentCodeItem in newParentTypeCodes)
            {
                var parentsInSystem = allParentsForCode.Where(rec => rec.Code == parentCodeItem && rec.VersionId == 1)
                    ?.OrderByDescending(rec => rec.Createdtimestamp)
                    .ToList();

                if (parentsInSystem.IsCollectionValid() && parentsInSystem.Count > 1)
                {
                    parentFormularyIdsToLookFor.Add(parentsInSystem[1].FormularyId);
                }
            }

            if (!parentFormularyIdsToLookFor.IsCollectionValid()) return childrenToReImport;

            var (scopeInnerB, unitOfWorkInnerB) = GetUoWInNewScope();

            //ignore these child prevcode as new code for the same has come and these old codes cannot be re-imported
            var childPrevCodes = unitOfWorkInnerB.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                                    .Where(rec => parentFormularyIdsToLookFor.Contains(rec.ParentFormularyId) && rec.Prevcode != null)
                                    ?.Select(rec => rec.Prevcode)
                                    .Distinct()
                                    .ToList();

            //get all vmps associated to this version and re-import the same
            var childCodes = unitOfWorkInnerB.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                                    .Where(rec => parentFormularyIdsToLookFor.Contains(rec.ParentFormularyId))
                                    ?.Select(rec => rec.Code)
                                    .Distinct()
                                    .ToList();

            DisposeUoWWithScope(scopeInnerB, unitOfWorkInnerB);

            //if these vmpCodes are already in the imported 'codes' - ignore from reimporting
            if (!childCodes.IsCollectionValid()) return childrenToReImport;

            childCodes = childCodes.Except(newChildProductTypeCodes).ToList();

            if (childPrevCodes.IsCollectionValid())
                childCodes = childCodes.Except(childPrevCodes).ToList();

            childCodes?.Each(rec => childrenToReImport.Add(rec));

            return childrenToReImport;
        }

        private async Task AssociateNewChildCodesWithLatestParentFormularyId(List<string> newchildCodes)
        {
            if (!newchildCodes.IsCollectionValid()) return;

            var newFVIdWithAmpCodeAsLkp = GetFVIdsOfNewCodesAsLookup(newchildCodes);

            if (!newFVIdWithAmpCodeAsLkp.IsCollectionValid()) return;

            var ampCodeWithVMPFormularyIdAsLkp = GetParentFormularyIdWithItsCodeOfNewCodesAsLookup(newchildCodes);

            if (!ampCodeWithVMPFormularyIdAsLkp.IsCollectionValid()) return;

            var (scopeInnerB, unitOfWorkInnerB) = GetUoWInNewScope();

            //same code can have different parent code when parent code has changed
            var ampsFormularyVersionIds = newFVIdWithAmpCodeAsLkp.Keys.ToList();

            unitOfWorkInnerB.FormularyHeaderFormularyRepository.Items
                           .Where(rec => rec.FormularyVersionId != null && ampsFormularyVersionIds.Contains(rec.FormularyVersionId))
                           .Each(rec => {
                               if (newFVIdWithAmpCodeAsLkp.ContainsKey(rec.FormularyVersionId))
                               {
                                   var ampCode = newFVIdWithAmpCodeAsLkp[rec.FormularyVersionId];
                                   if (ampCodeWithVMPFormularyIdAsLkp.ContainsKey(ampCode))
                                       rec.ParentFormularyId = ampCodeWithVMPFormularyIdAsLkp[ampCode].FormularyId;
                               }
                           });
            unitOfWorkInnerB.FormularyHeaderFormularyRepository.SaveChanges();

            DisposeUoWWithScope(scopeInnerB, unitOfWorkInnerB);
        }

        private Dictionary<string, (string FormularyId, string Code)> GetParentFormularyIdWithItsCodeOfNewCodesAsLookup(List<string> newCodes)
        {
            //Note If newCode is of 'VMP' read variable name for vmp as vtm and amp as vmp
            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            //Get Latest VMPs for these AMPs
            //same code can have different parent code when parent code has changed
            var ampCodeWithParentCodes = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                           .Where(rec => rec.Code != null && newCodes.Contains(rec.Code))
                           ?.Select(rec => new { rec.Code, rec.ParentCode })
                           .ToList();


            if (!ampCodeWithParentCodes.IsCollectionValid())
            {
                DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);
                return null;
            }

            var ampCodeWithParentCodesAsLookup = new Dictionary<string, List<string>>();

            ampCodeWithParentCodes.Each(rec =>
            {
                if (!ampCodeWithParentCodesAsLookup.ContainsKey(rec.Code))
                    ampCodeWithParentCodesAsLookup[rec.Code] = new List<string>();
                ampCodeWithParentCodesAsLookup[rec.Code].Add(rec.ParentCode);
            });

            var onlyParentCodes = ampCodeWithParentCodes.Select(rec => rec.ParentCode)?.Distinct().ToList();

            var vmpList = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                           .Where(rec => rec.Code != null && onlyParentCodes.Contains(rec.Code))
                           ?.Select(rec => new { rec.Code, rec.FormularyId, rec.Createdtimestamp, rec.VersionId })
                           .ToList();

            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

            //cannot associate - but cannot occur unless it is the root
            if (!vmpList.IsCollectionValid()) return null;

            var ampCodeWithVMPFormularyIdAsLkp = new Dictionary<string, (string FormularyId, string Code)>();

            //get latest formularyid for each vmpcode
            foreach (var ampCodeWithParentCodesItemKey in ampCodeWithParentCodesAsLookup.Keys)
            {
                var ampCode = ampCodeWithParentCodesItemKey;
                var ampParentCodes = ampCodeWithParentCodesAsLookup[ampCodeWithParentCodesItemKey];

                if (!ampParentCodes.IsCollectionValid()) continue;

                var vmpFormularyIdWithCodes = vmpList.Where(rec => rec.Code != null && ampParentCodes.Contains(rec.Code) && rec.VersionId == 1)
                    ?.OrderByDescending(rec => rec.Createdtimestamp)
                    .Select(rec => (rec.FormularyId, rec.Code))
                    .ToList();

                if (!vmpFormularyIdWithCodes.IsCollectionValid()) continue;

                ampCodeWithVMPFormularyIdAsLkp[ampCode] = (vmpFormularyIdWithCodes[0].FormularyId, vmpFormularyIdWithCodes[0].Code);
            }

            return ampCodeWithVMPFormularyIdAsLkp;
        }

        private Dictionary<string, string> GetFVIdsOfNewCodesAsLookup(List<string> newCodes)
        {
            //Get Latest FormularyId For these AMPs.
            #region e.g.
            //E.g.
            /*
                VMP01 -FID01-V1.0 -1July
                VMP01 -FID01-V2.0 -2July
                VMP01 -FID02-V1.0 -3July
                VMP01 -FID02-V2.0 -3July
                VMP01 -FID01-V3.0 -14July
            Should consider: VMP01 -FID02-V2.0 -3July (since this is the latest)
             */
            #endregion e.g.

            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            var currentLevelCodeWithAllFormularyVIds = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                           .Where(rec => rec.Code != null && newCodes.Contains(rec.Code) && rec.VersionId == 1)
                           ?.Select(rec => new { rec.Code, rec.FormularyId, rec.FormularyVersionId, rec.Createdtimestamp })
                           .ToList();

            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

            if (!currentLevelCodeWithAllFormularyVIds.IsCollectionValid()) return null;

            //For each of these fvids we need to associate the Parent (VMP's) FormularyId
            var newFVIdWithcurrentLevelCodeAsLkp = new Dictionary<string, string>();

            foreach (var currentLevelCode in newCodes)
            {
                var mappedNewestFormularyVersionId = currentLevelCodeWithAllFormularyVIds.Where(rec => rec.Code == currentLevelCode)
                ?.OrderByDescending(rec => rec.Createdtimestamp)
                .Select(rec => rec.FormularyVersionId)
                .FirstOrDefault();

                if (mappedNewestFormularyVersionId == null) continue;

                if (!newFVIdWithcurrentLevelCodeAsLkp.ContainsKey(mappedNewestFormularyVersionId))
                    newFVIdWithcurrentLevelCodeAsLkp[mappedNewestFormularyVersionId] = currentLevelCode;
            }

            return newFVIdWithcurrentLevelCodeAsLkp;
        }


        #region old code - ref only
        //==========================================
        private void AssociateChildNodes(List<string> codesImported)
        {
            AssociateVTMWithChildNodes(codesImported);

            AssociateVMPWithChildNodes(codesImported);
        }

        #region AssociateVTMWithChildNodes
        private void AssociateVTMWithChildNodes(List<string> codes)
        {
            var vtmCodes = GetVTMsToBeAssociated(codes);
            //var vtmCodes = GetVTMsToBeAssociated(vtmsForUnmappedVMPsList);

            //For each of these VTM codes - get the newest FormularyId
            var newestFormularyIdForVTMCodesAsLookup = GetNewestFormularyIdForVTMCodesAsLookup(vtmCodes);

            if (!vtmCodes.IsCollectionValid() || !newestFormularyIdForVTMCodesAsLookup.IsCollectionValid()) return;

            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            var vmpList = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => rec.ParentCode != null && vtmCodes.Contains(rec.ParentCode) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)
                ?.Select(rec => new { rec.Code, rec.FormularyVersionId, rec.VersionId, rec.FormularyId, rec.ParentCode, rec.ParentFormularyId, rec.IsLatest, rec.Createdtimestamp })
                .ToList();

            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

            if (!vmpList.IsCollectionValid()) return;

            //select VMPs to clone and re-create
            //select VMPs already created just to associate the parent VTM-FormularyId
            var vmpFVIdsToBeClonedAsLkp = new Dictionary<string, HashSet<string>>();
            var vmpFVIdsToBeAssociatedToVTMAsLkp = new Dictionary<string, HashSet<string>>();

            vtmCodes.Each(vtm =>
            {
                AssignVMPFVIDsToBeCloned(vmpFVIdsToBeClonedAsLkp, vmpList, vtm);

                AssignVMPFVIDsToBeAssociated(vmpFVIdsToBeAssociatedToVTMAsLkp, vmpList, vtm);
            });

            if (vmpFVIdsToBeClonedAsLkp.IsCollectionValid())
                CloneCreateAndAssociateNewVMPsToVTM(vmpFVIdsToBeClonedAsLkp, newestFormularyIdForVTMCodesAsLookup);

            if (vmpFVIdsToBeAssociatedToVTMAsLkp.IsCollectionValid())
                AssociateOldVMPsToVTM(vmpFVIdsToBeAssociatedToVTMAsLkp, newestFormularyIdForVTMCodesAsLookup);
        }

        /// <summary>
        /// These are the 'VTM's to which either new VMP need to be created (cloning the existing) [If VMP has not come as a change but only VTM has changed ]
        /// or VMP has come as change and only need to be associated with ParentVTM FormularyId
        /// </summary>
        /// <returns></returns>
        private List<string>? GetVTMsToBeAssociated(List<string> allCodesImported)
        {
            var (scopeInner, unitOfWorkInner) = GetUoWInNewScope();

            var vtmCodesImported = allCodesImported.IsCollectionValid() ? unitOfWorkInner.FormularyHeaderFormularyRepository.ItemsAsReadOnly
            .Where(rec => rec.IsLatest == true && rec.ProductType == TerminologyConstants.PRODUCT_TYPE_VTM && allCodesImported.Contains(rec.Code))
            ?.Select(rec => rec.Code)
            .Distinct(rec => rec)
            .ToList() : new List<string?>();

            var vtmCodeList = unitOfWorkInner.FormularyHeaderFormularyRepository.ItemsAsReadOnly
            .Where(rec => rec.IsLatest == true && rec.ProductType == TerminologyConstants.PRODUCT_TYPE_VTM)
            ?.Select(rec => rec.Code)
            .Distinct(rec => rec)
            .ToList();

            if (!vtmCodeList.IsCollectionValid())
            {
                DisposeUoWWithScope(scopeInner, unitOfWorkInner);
                return vtmCodesImported;
            }

            //e.g. vmp only (not vtm) is newly imported 
            var vtmsForUnmappedVMPsList = unitOfWorkInner.FormularyHeaderFormularyRepository.ItemsAsReadOnly
            .Where(rec => rec.IsLatest == true && vtmCodeList.Contains(rec.ParentCode) && rec.ParentFormularyId == null && rec.ProductType == TerminologyConstants.PRODUCT_TYPE_VMP)
            ?.Select(rec => rec.ParentCode)
            .Distinct()
            .ToList() ?? new List<string?>();

            vtmCodesImported?.Each(rec => vtmsForUnmappedVMPsList.Add(rec));

            DisposeUoWWithScope(scopeInner, unitOfWorkInner);

            return vtmsForUnmappedVMPsList?.Distinct().ToList();

            //var batchsize = 100;
            //var vtms = new List<FormularyHeader>();

            //var batchedRequestsForStatusUpdate = new List<List<string>>();

            //for (var reqIndex = 0; reqIndex < codes.Count; reqIndex += batchsize)
            //{
            //    var batches = codes.Skip(reqIndex).Take(batchsize);
            //    batchedRequestsForStatusUpdate.Add(batches.ToList());
            //}

            //foreach (var batchedRequest in batchedRequestsForStatusUpdate)
            //{
            //    var (scopeInner, unitOfWorkInner) = GetUoWInNewScope();

            //    var vtmList = unitOfWorkInner.FormularyHeaderFormularyRepository.ItemsAsReadOnly
            //    .Where(rec => codes.Contains(rec.Code) && rec.IsLatest == true && rec.ProductType == TerminologyConstants.PRODUCT_TYPE_VTM)
            //    ?.Distinct(rec => rec.Code).ToList();

            //    if (!vtmList.IsCollectionValid()) continue;

            //    vtms.AddRange(vtmList);

            //    DisposeUoWWithScope(scopeInner, unitOfWorkInner);
            //}

            //if (!vtms.IsCollectionValid()) return null;

            //var vtmCodes = vtms.Select(rec => rec.Code).ToList();

            //return vtmCodes;
        }

        private Dictionary<string, string>? GetNewestFormularyIdForVTMCodesAsLookup(List<string>? vtmCodes)
        {
            if (!vtmCodes.IsCollectionValid()) return null;

            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            var vtmList = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => vtmCodes.Contains(rec.Code) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)
                ?.Select(rec => new { rec.Code, rec.FormularyVersionId, rec.VersionId, rec.FormularyId, rec.ParentCode, rec.ParentFormularyId, rec.IsLatest, rec.Createdtimestamp })
                .ToList();

            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

            var vtmFIdsTobeCloned = vtmList.Where(rec => rec.VersionId == 1 && rec.FormularyId.IsNotEmpty())
            ?.OrderByDescending(rec => rec.Createdtimestamp)
            .Select(rec => new { rec.Code, rec.FormularyId })
            .ToList();

            var vtmFIDLookup = new Dictionary<string, string>();

            //select the most recently added vmp to the system to clone 
            vtmFIdsTobeCloned?.Each(vtmCodeWithFId =>
            {
                if (vtmCodeWithFId != null && !vtmFIDLookup.ContainsKey(vtmCodeWithFId.Code))
                    vtmFIDLookup[vtmCodeWithFId.Code] = vtmCodeWithFId.FormularyId;
            });

            return vtmFIDLookup;
        }

        private void AssignVMPFVIDsToBeCloned(Dictionary<string, HashSet<string>> vmpFVIdsToBeClonedAsLkp, IEnumerable<dynamic> vmpList, string vtm)
        {
            //E.g.
            /*
                VMP01 -FID01-V1.0 -1July
                VMP01 -FID01-V2.0 -2July
                VMP01 -FID02-V1.0 -3July
                VMP01 -FID02-V2.0 -3July
                VMP01 -FID01-V3.0 -14July
            Should consider: VMP01 -FID02-V2.0 -3July (since this is the latest)
             */

            //already associated for the previous version of the same VTM code in the previous import
            var vmpFIdsTobeCloned = vmpList.Where(rec => rec.ParentCode == vtm && rec.VersionId == 1 && rec.ParentFormularyId.IsNotEmpty())
            ?.OrderByDescending(rec => rec.Createdtimestamp)
            .Select(rec => rec.FormularyId)
            .Distinct()
            .ToList();

            var vmpCodeCounter = new HashSet<string>();
            var vmpFVIdsTobeCloned = new List<string>();

            //select the most recently added vmp to the system to clone 
            vmpFIdsTobeCloned?.Each(vmpFId =>
            {
                //to be refactored - 0^2
                var vmpCodeFVId = vmpList.Where(rec => vmpFId == rec.FormularyId && rec.IsLatest == true && rec.ParentFormularyId.IsNotEmpty())
                                            ?.OrderByDescending(rec => rec.VersionId)
                                            .Select(rec => new { rec.Code, rec.FormularyVersionId })
                                            .FirstOrDefault();

                if (vmpCodeFVId != null && !vmpCodeCounter.Contains(vmpCodeFVId.Code))
                    vmpFVIdsTobeCloned.Add(vmpCodeFVId.FormularyVersionId);
            });

            //var vmpFVIdsTobeCloned = vmpList.Where(rec => rec.ParentCode == vtm && rec.IsLatest == true && rec.ParentFormularyId.IsNotEmpty())
            //?.OrderByDescending(rec => rec.VersionId)
            //.Select(rec => rec.FormularyVersionId)
            //.ToList();

            if (vmpFVIdsTobeCloned.IsCollectionValid())
            {
                vmpFVIdsTobeCloned.Each(vmp =>
                {
                    if (vmpFVIdsToBeClonedAsLkp.ContainsKey(vtm))
                    {
                        if (!vmpFVIdsToBeClonedAsLkp[vtm].Contains(vmp))
                            vmpFVIdsToBeClonedAsLkp[vtm].Add(vmp);
                    }
                    else
                        vmpFVIdsToBeClonedAsLkp[vtm] = new HashSet<string> { vmp };
                });
            }
        }

        private void CloneCreateAndAssociateNewVMPsToVTM(Dictionary<string, HashSet<string>> vmpFVIdsToBeClonedAsLkp, Dictionary<string, string>? newestFormularyIdForVTMCodesAsLookup)
        {
            if (!vmpFVIdsToBeClonedAsLkp.IsCollectionValid()) return;

            foreach (var vtmVMPFVIdsToBeCloned in vmpFVIdsToBeClonedAsLkp)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                var vmpCodeTobeCloned = vtmVMPFVIdsToBeCloned.Key;
                var vmpFVIdsTobeCloned = vtmVMPFVIdsToBeCloned.Value;

                var resultsTobeCloned = unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking()
                    .Where(rec => rec.IsLatest == true && vmpFVIdsTobeCloned.Contains(rec.FormularyVersionId))
                .ToList();

                if (!resultsTobeCloned.IsCollectionValid()) continue;

                foreach (var tobeCloned in resultsTobeCloned)
                {
                    var cloned = _formularyUtil.CloneFormulary(tobeCloned);
                    cloned.FormularyVersionId = Guid.NewGuid().ToString();
                    cloned.FormularyId = Guid.NewGuid().ToString();
                    cloned.VersionId = 1;
                    cloned.IsLatest = true;
                    cloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_ACTIVE;
                    cloned.ParentFormularyId = newestFormularyIdForVTMCodesAsLookup[vmpCodeTobeCloned];
                    unitOfWork.FormularyHeaderFormularyRepository.Add(cloned);
                }

                unitOfWork.FormularyHeaderFormularyRepository.SaveChanges();

                DisposeUoWWithScope(scope, unitOfWork);
            }
        }

        private void AssociateOldVMPsToVTM(Dictionary<string, HashSet<string>> vmpFVIdsToBeAssociatedToVTMAsLkp, Dictionary<string, string>? newestFormularyIdForVTMCodesAsLookup)
        {
            if (!vmpFVIdsToBeAssociatedToVTMAsLkp.IsCollectionValid()) return;

            foreach (var vtmVMPFVIdsToBeUpdated in vmpFVIdsToBeAssociatedToVTMAsLkp)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                var vmpCodeTobeUpdated = vtmVMPFVIdsToBeUpdated.Key;
                var vmpFVIdsTobeUpdated = vtmVMPFVIdsToBeUpdated.Value;

                var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

                var existingRecs = unitOfWorkInnerA.FormularyHeaderFormularyRepository.Items.Where(rec => vmpFVIdsTobeUpdated.Contains(rec.FormularyVersionId))?.ToList();

                existingRecs?.Each(rec =>
                {
                    rec.ParentFormularyId = newestFormularyIdForVTMCodesAsLookup[vmpCodeTobeUpdated];
                    unitOfWorkInnerA.FormularyHeaderFormularyRepository.Update(rec);
                });

                unitOfWorkInnerA.FormularyHeaderFormularyRepository.SaveChanges();

                DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);
            }
        }

        #endregion AssociateVTMWithChildNodes

        #region AssociateVMPWithChildNodes
        private void AssociateVMPWithChildNodes(List<string> codesImported)
        {
            var vmpCodes = GetVMPsToBeAssociated(codesImported);

            //For each of these VTM codes - get the newest FormularyId
            var newestFormularyIdForVMPCodesAsLookup = GetNewestFormularyIdForVMPCodesAsLookup(vmpCodes);

            if (!vmpCodes.IsCollectionValid() || !newestFormularyIdForVMPCodesAsLookup.IsCollectionValid()) return;

            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            var ampList = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => rec.ParentCode != null && vmpCodes.Contains(rec.ParentCode))
                ?.Select(rec => new { rec.Code, rec.FormularyVersionId, rec.VersionId, rec.FormularyId, rec.ParentCode, rec.ParentFormularyId, rec.IsLatest, rec.Createdtimestamp })
                .ToList();

            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

            if (!ampList.IsCollectionValid()) return;

            //select AMPs to clone and re-create
            //select AMPs already created just to associate the parent VMP-FormularyId
            var ampFVIdsToBeClonedAsLkp = new Dictionary<string, HashSet<string>>();
            var ampFVIdsToBeAssociatedToVMPAsLkp = new Dictionary<string, HashSet<string>>();

            vmpCodes.Each(vtm =>
            {
                AssignAMPFVIDsToBeCloned(ampFVIdsToBeClonedAsLkp, ampList, vtm);

                AssignAMPFVIDsToBeAssociated(ampFVIdsToBeAssociatedToVMPAsLkp, ampList, vtm);
            });

            if (ampFVIdsToBeClonedAsLkp.IsCollectionValid())
                CloneCreateAndAssociateNewAMPsToVMP(ampFVIdsToBeClonedAsLkp, newestFormularyIdForVMPCodesAsLookup);

            if (ampFVIdsToBeAssociatedToVMPAsLkp.IsCollectionValid())
                AssociateOldAMPsToVMP(ampFVIdsToBeAssociatedToVMPAsLkp, newestFormularyIdForVMPCodesAsLookup);
        }


        /// <summary>
        /// These are the 'VMP's to which either new AMP need to be created (cloning the existing) [If AMP has not come as a change but only VMP has changed ]
        /// or AMP has come as change and only need to be associated with ParentVMP FormularyId
        /// </summary>
        /// <returns></returns>
        private List<string?> GetVMPsToBeAssociated(List<string> allCodes)
        {
            var (scopeInner, unitOfWorkInner) = GetUoWInNewScope();

            var vmpCodesImported = allCodes.IsCollectionValid() ? unitOfWorkInner.FormularyHeaderFormularyRepository.ItemsAsReadOnly
            .Where(rec => rec.IsLatest == true && rec.ProductType == TerminologyConstants.PRODUCT_TYPE_VMP && allCodes.Contains(rec.Code))
            ?.Select(rec => rec.Code)
            .Distinct(rec => rec)
            .ToList() : new List<string?>();

            var vmpCodeList = unitOfWorkInner.FormularyHeaderFormularyRepository.ItemsAsReadOnly
            .Where(rec => rec.IsLatest == true && rec.ProductType == TerminologyConstants.PRODUCT_TYPE_VMP)
            ?.Select(rec => rec.Code)
            .Distinct(rec => rec)
            .ToList();

            if (!vmpCodeList.IsCollectionValid())
            {
                DisposeUoWWithScope(scopeInner, unitOfWorkInner);
                return vmpCodesImported;
            }

            var vmpsForUnmappedVMPsList = unitOfWorkInner.FormularyHeaderFormularyRepository.ItemsAsReadOnly
            .Where(rec => rec.IsLatest == true && vmpCodeList.Contains(rec.ParentCode) && rec.ParentFormularyId == null && rec.ProductType == TerminologyConstants.PRODUCT_TYPE_AMP)
            ?.Select(rec => rec.ParentCode)
            .Distinct()
            .ToList() ?? new List<string?>();

            vmpCodesImported?.Each(rec => vmpsForUnmappedVMPsList.Add(rec));

            DisposeUoWWithScope(scopeInner, unitOfWorkInner);

            return vmpsForUnmappedVMPsList?.Distinct().ToList();
        }

        private Dictionary<string, string>? GetNewestFormularyIdForVMPCodesAsLookup(List<string>? vmpCodes)
        {
            if (!vmpCodes.IsCollectionValid()) return null;

            var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

            var vmpList = unitOfWorkInnerA.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => vmpCodes.Contains(rec.Code) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)
                ?.Select(rec => new { rec.Code, rec.FormularyVersionId, rec.VersionId, rec.FormularyId, rec.ParentCode, rec.ParentFormularyId, rec.IsLatest, rec.Createdtimestamp })
                .ToList();

            DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);

            var vmpFIdsTobeCloned = vmpList.Where(rec => rec.VersionId == 1 && rec.FormularyId.IsNotEmpty())
            ?.OrderByDescending(rec => rec.Createdtimestamp)
            .Select(rec => new { rec.Code, rec.FormularyId })
            .ToList();

            var vtmFIDLookup = new Dictionary<string, string>();

            //select the most recently added vmp to the system to clone 
            vmpFIdsTobeCloned?.Each(vtmCodeWithFId =>
            {
                if (vtmCodeWithFId != null && !vtmFIDLookup.ContainsKey(vtmCodeWithFId.Code))
                    vtmFIDLookup[vtmCodeWithFId.Code] = vtmCodeWithFId.FormularyId;
            });

            return vtmFIDLookup;
        }

        /// <summary>
        /// For these AMPs only association of the parent VMPFId is to be done
        /// </summary>
        /// <param name="vmpFVIdsToBeAssociatedToVTMAsLkp"></param>
        /// <param name="vmpList"></param>
        /// <param name="vtm"></param>
        private void AssignAMPFVIDsToBeAssociated(Dictionary<string, HashSet<string>> ampFVIdsToBeAssociatedToVTMAsLkp, IEnumerable<dynamic> ampList, string vmp)
        {
            var ampFVIdsTobeAssociated = ampList.Where(rec => rec.ParentCode == vmp && rec.IsLatest == true && rec.ParentFormularyId.IsEmpty())
            ?.OrderByDescending(rec => rec.Createdtimestamp)
            .Select(rec => rec.FormularyVersionId)
            .ToList();

            if (ampFVIdsTobeAssociated.IsCollectionValid())
            {
                ampFVIdsTobeAssociated.Each(vmp =>
                {
                    if (ampFVIdsToBeAssociatedToVTMAsLkp.ContainsKey(vmp))
                    {
                        if (!ampFVIdsToBeAssociatedToVTMAsLkp[vmp].Contains(vmp))
                            ampFVIdsToBeAssociatedToVTMAsLkp[vmp].Add(vmp);
                    }
                    else
                        ampFVIdsToBeAssociatedToVTMAsLkp[vmp] = new HashSet<string> { vmp };
                });
            }
        }

        /// <summary>
        /// For these VMPs only association of the parent VTMFId is to be done
        /// </summary>
        /// <param name="vmpFVIdsToBeAssociatedToVTMAsLkp"></param>
        /// <param name="vmpList"></param>
        /// <param name="vtm"></param>
        private void AssignVMPFVIDsToBeAssociated(Dictionary<string, HashSet<string>> vmpFVIdsToBeAssociatedToVTMAsLkp, IEnumerable<dynamic> vmpList, string vtm)
        {
            var vmpFVIdsTobeAssociated = vmpList.Where(rec => rec.ParentCode == vtm && rec.IsLatest == true && rec.ParentFormularyId.IsEmpty())
            ?.OrderByDescending(rec => rec.Createdtimestamp)
            .Select(rec => rec.FormularyVersionId)
            .ToList();

            if (vmpFVIdsTobeAssociated.IsCollectionValid())
            {
                vmpFVIdsTobeAssociated.Each(vmp =>
                {
                    if (vmpFVIdsToBeAssociatedToVTMAsLkp.ContainsKey(vtm))
                    {
                        if (!vmpFVIdsToBeAssociatedToVTMAsLkp[vtm].Contains(vmp))
                            vmpFVIdsToBeAssociatedToVTMAsLkp[vtm].Add(vmp);
                    }
                    else
                        vmpFVIdsToBeAssociatedToVTMAsLkp[vtm] = new HashSet<string> { vmp };
                });
            }
        }

        private void AssignAMPFVIDsToBeCloned(Dictionary<string, HashSet<string>> vmpFVIdsToBeClonedAsLkp, IEnumerable<dynamic> ampList, string vmp)
        {
            //E.g.
            /*
                AMP01 -VMP01 -FID01-V1.0 -1July - Draft
                AMP01 -VMP01 -FID01-V2.0 -2July - Draft
            =====================================
                AMP01 -VMP01 -FID01-V1.0 -1July - Archived
                AMP01 -VMP01 -FID01-V2.0 -2July - Archived
                AMP01 -VMP01 -FID02-V1.0 -3July - Draft
                AMP01 -VMP01 -FID02-V2.0 -3July - Draft
            Should consider: AMP01 -FID02-V2.0 -3July(since this is the latest). Note: no need to check the status in amps.
             */

            //already AMP is associated for the previous version of the same VMP code in the previous import
            var ampFIdsTobeCloned = ampList.Where(rec => rec.ParentCode == vmp && rec.VersionId == 1 && rec.ParentFormularyId.IsNotEmpty())
            ?.OrderByDescending(rec => rec.Createdtimestamp)
            .Select(rec => rec.FormularyId)
            .Distinct()
            .ToList();

            var ampCodeCounter = new HashSet<string>();
            var ampFVIdsTobeCloned = new List<string>();

            //select the most recently added amp to the system to clone 
            ampFIdsTobeCloned.Each(ampFId =>
            {
                //to be refactored - 0^2
                var ampCodeFVId = ampList.Where(rec => ampFId == rec.FormularyId && rec.IsLatest == true && rec.ParentFormularyId.IsNotEmpty())
                                            ?.OrderByDescending(rec => rec.VersionId)
                                            .Select(rec => new { rec.Code, rec.FormularyVersionId })
                                            .FirstOrDefault();

                if (ampCodeFVId != null && !ampCodeCounter.Contains(ampCodeFVId.Code))
                    ampFVIdsTobeCloned.Add(ampCodeFVId.FormularyVersionId);
            });

            //var vmpFVIdsTobeCloned = vmpList.Where(rec => rec.ParentCode == vtm && rec.IsLatest == true && rec.ParentFormularyId.IsNotEmpty())
            //?.OrderByDescending(rec => rec.VersionId)
            //.Select(rec => rec.FormularyVersionId)
            //.ToList();

            if (ampFVIdsTobeCloned.IsCollectionValid())
            {
                ampFVIdsTobeCloned.Each(amp =>
                {
                    if (vmpFVIdsToBeClonedAsLkp.ContainsKey(vmp))
                    {
                        if (!vmpFVIdsToBeClonedAsLkp[vmp].Contains(amp))
                            vmpFVIdsToBeClonedAsLkp[vmp].Add(amp);
                    }
                    else
                        vmpFVIdsToBeClonedAsLkp[vmp] = new HashSet<string> { amp };
                });
            }
        }
        
        private void CloneCreateAndAssociateNewAMPsToVMP(Dictionary<string, HashSet<string>> ampFVIdsToBeClonedAsLkp, Dictionary<string, string>? newestFormularyIdForVMPCodesAsLookup)
        {
            if (!ampFVIdsToBeClonedAsLkp.IsCollectionValid()) return;

            foreach (var ampVMPFVIdsToBeCloned in ampFVIdsToBeClonedAsLkp)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                var ampCodeTobeCloned = ampVMPFVIdsToBeCloned.Key;
                var ampFVIdsTobeCloned = ampVMPFVIdsToBeCloned.Value;

                var resultsTobeCloned = unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking()
                    .Where(rec => rec.IsLatest == true && ampFVIdsTobeCloned.Contains(rec.FormularyVersionId))
                .ToList();

                if (!resultsTobeCloned.IsCollectionValid()) continue;

                var ampCodesTobeCloned = resultsTobeCloned.Select(rec=> rec.Code).ToList();

                var resultsTobeArchieved = unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking()
                    .Where(rec => rec.IsLatest == true && ampCodesTobeCloned.Contains(rec.Code) && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)
                .ToList();

                resultsTobeArchieved?.Each(rec =>
                {
                    var cloned = _formularyUtil.CloneFormulary(rec);
                    cloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
                    unitOfWork.FormularyHeaderFormularyRepository.Add(cloned);
                });

                foreach (var tobeCloned in resultsTobeCloned)
                {
                    var cloned = _formularyUtil.CloneFormulary(tobeCloned);
                    cloned.FormularyVersionId = Guid.NewGuid().ToString();
                    cloned.FormularyId = Guid.NewGuid().ToString();
                    cloned.VersionId = 1;
                    cloned.IsLatest = true;
                    cloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_ACTIVE;
                    cloned.ParentFormularyId = newestFormularyIdForVMPCodesAsLookup[ampCodeTobeCloned];
                    unitOfWork.FormularyHeaderFormularyRepository.Add(cloned);

                    //
                    var existingDraftsTobeupdated = unitOfWork.FormularyHeaderFormularyRepository.Items.Where(rec => rec.IsLatest == true && tobeCloned.Code == rec.Code && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT);

                    if (existingDraftsTobeupdated.IsCollectionValid())
                    {
                        existingDraftsTobeupdated.Each(rec => rec.IsLatest = false);
                    }
                }

                unitOfWork.FormularyHeaderFormularyRepository.SaveChanges();

                DisposeUoWWithScope(scope, unitOfWork);
            }
        }

        private void AssociateOldAMPsToVMP(Dictionary<string, HashSet<string>> ampFVIdsToBeAssociatedToVMPAsLkp, Dictionary<string, string>? newestFormularyIdForVMPCodesAsLookup)
        {
            if (!ampFVIdsToBeAssociatedToVMPAsLkp.IsCollectionValid()) return;

            foreach (var vmpVMPFVIdsToBeUpdated in ampFVIdsToBeAssociatedToVMPAsLkp)
            {
                var (scope, unitOfWork) = GetUoWInNewScope();

                var vmpCodeTobeUpdated = vmpVMPFVIdsToBeUpdated.Key;
                var vmpFVIdsTobeUpdated = vmpVMPFVIdsToBeUpdated.Value;

                var (scopeInnerA, unitOfWorkInnerA) = GetUoWInNewScope();

                var existingRecs = unitOfWorkInnerA.FormularyHeaderFormularyRepository.Items.Where(rec => vmpFVIdsTobeUpdated.Contains(rec.FormularyVersionId))?.ToList();

                existingRecs?.Each(rec =>
                {
                    rec.ParentFormularyId = newestFormularyIdForVMPCodesAsLookup[vmpCodeTobeUpdated];
                    unitOfWorkInnerA.FormularyHeaderFormularyRepository.Update(rec);
                });

                unitOfWorkInnerA.FormularyHeaderFormularyRepository.SaveChanges();

                DisposeUoWWithScope(scopeInnerA, unitOfWorkInnerA);
            }
        }

        #endregion AssociateVMPWithChildNodes

        /*
         * 
         //public async Task InvokePostImportProcess(Action onComplete = null)
        //{
        //    PopulateAMPFromVMPs();
        //    PopulateVTMFromVMPs();

        //    await ProcessInhalationRule8();

        //    await CreateCopyOfRoutes();

        //    UpdateLicensedAndUnlicensedUse();

        //    _formularyRepository.SaveChanges();

        //    onComplete?.Invoke();
        //}

        private void PopulateAMPFromVMPs()
        {
            var allFormularyVMPCodes = _unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => rec.ProductType.ToLower() == "vmp")
                .Select(rec => rec.Code)
                .ToList();

            if (!allFormularyVMPCodes.IsCollectionValid()) return;

            var allVmpsWithDetails = _unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesByCodes(allFormularyVMPCodes.ToArray()).ToList();

            if (!allVmpsWithDetails.IsCollectionValid()) return;

            var vmpDictionary = new ConcurrentDictionary<string, FormularyHeader>();

            allVmpsWithDetails.AsParallel().Each(rec => { vmpDictionary[rec.Code] = rec; });

            var allAmpCodesForVmps = _unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => vmpDictionary.Keys.Contains(rec.ParentCode))
                .Select(rec => rec.Code)
                .ToList();

            var allAmpsWithDetails = _unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesByCodes(allAmpCodesForVmps.ToArray()).ToList();

            if (!allAmpsWithDetails.IsCollectionValid()) return;

            var ampDictionary = new ConcurrentDictionary<string, List<FormularyHeader>>();

            allAmpsWithDetails.AsParallel().Each(rec =>
            {
                if (!ampDictionary.ContainsKey(rec.ParentCode))
                {
                    ampDictionary[rec.ParentCode] = new List<FormularyHeader> { rec };
                }
                else
                {
                    ampDictionary[rec.ParentCode].Add(rec);
                }
            });

            vmpDictionary.Each(vmp =>
            {
                var ampsForVMP = ampDictionary.ContainsKey(vmp.Key) ? ampDictionary[vmp.Key] : null;

                if (ampsForVMP.IsCollectionValid())
                {
                    AssignAMPsWithVMPProps(ampsForVMP, vmp.Value);
                }
            });
        }
        private void PopulateVTMFromVMPs()
        {
            var allFormularyVTMCodes = _unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => rec.ProductType.ToLower() == "vtm")
                .Select(rec => rec.Code)
                .ToList();

            if (!allFormularyVTMCodes.IsCollectionValid()) return;

            var allVtmsWithDetails = _unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesByCodes(allFormularyVTMCodes.ToArray()).ToList();

            if (!allVtmsWithDetails.IsCollectionValid()) return;

            var vtmDictionary = new ConcurrentDictionary<string, FormularyHeader>();

            allVtmsWithDetails.AsParallel().Each(rec => { vtmDictionary[rec.Code] = rec; });

            var allvmpCodesForVtms = _unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => vtmDictionary.Keys.Contains(rec.ParentCode))
                .Select(rec => rec.Code)
                .ToList();
            var allvmpsWithDetails = _unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesByCodes(allvmpCodesForVtms.ToArray()).ToList();

            if (!allvmpsWithDetails.IsCollectionValid()) return;

            var vmpDictionary = new ConcurrentDictionary<string, List<FormularyHeader>>();
            allvmpsWithDetails.AsParallel().Each(rec =>
            {
                if (!vmpDictionary.ContainsKey(rec.ParentCode))
                    vmpDictionary[rec.ParentCode] = new List<FormularyHeader> { rec };
                else
                    vmpDictionary[rec.ParentCode].Add(rec);
            });

            vtmDictionary.Each(vtm =>
            {
                var vmpsForVTM = vmpDictionary.ContainsKey(vtm.Key) ? vmpDictionary[vtm.Key] : null;
                if (vmpsForVTM.IsCollectionValid())
                    AssignVTMsWithVMPProps(vmpsForVTM, vtm.Value);
            });
        }

        private ConcurrentBag<string> GetFormularyIdsToUpdate(List<string> vtmCodes)
        {
            var formularyIdsToUpdate = new ConcurrentBag<string>();

            var vtmsWithItsForms = new ConcurrentDictionary<string, HashSet<string>>();

            if (!vtmCodes.IsCollectionValid()) return formularyIdsToUpdate;

            var vmpsForVTMs = _unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => vtmCodes.Contains(rec.ParentCode))
                .Select(rec => new { parentCode = rec.ParentCode, detail = rec.FormularyDetail.FirstOrDefault() })
                .ToList();

            if (!vmpsForVTMs.IsCollectionValid()) return formularyIdsToUpdate;

            vmpsForVTMs.AsParallel().Each(vmp =>
            {
                if (vmp.detail != null && vmp.parentCode.IsNotEmpty())
                {
                    if (vtmsWithItsForms.ContainsKey(vmp.parentCode))
                    {
                        if (!vtmsWithItsForms[vmp.parentCode].Contains(vmp.detail.FormCd))
                        {
                            vtmsWithItsForms[vmp.parentCode].Add(vmp.detail.FormCd);
                        }
                    }
                    else
                    {
                        vtmsWithItsForms[vmp.parentCode] = new HashSet<string> { vmp.detail.FormCd };
                    }
                }
            });


            if (!vtmsWithItsForms.IsCollectionValid()) return formularyIdsToUpdate;

            vtmsWithItsForms.AsParallel().Each(vtmWithForm =>
            {
                if (vtmWithForm.Value != null && vtmWithForm.Value.Count > 1)
                {
                    formularyIdsToUpdate.Add(vtmWithForm.Key);
                }
            });

            return formularyIdsToUpdate;
        }

        
        private List<string> GetVTMsForAMPIds(List<string> uniqueIds)
        {
            if (!uniqueIds.IsCollectionValid()) return null;

            var formularyCodesForRoutes = _unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                 .Where(rec => uniqueIds.Contains(rec.FormularyVersionId))
                 .Select(rec => rec.Code)
                 .ToList();

            if (!formularyCodesForRoutes.IsCollectionValid()) return null;

            var vmpCodesForAMPs = _unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => formularyCodesForRoutes.Contains(rec.Code))
                .Select(rec => rec.ParentCode)
                .ToList();

            if (!vmpCodesForAMPs.IsCollectionValid()) return null;

            var vtmCodes = _unitOfWork.FormularyHeaderFormularyRepository.ItemsAsReadOnly
                .Where(rec => vmpCodesForAMPs.Contains(rec.Code))
                .Select(rec => rec.ParentCode)
                .ToList();

            return vtmCodes;
        }

        
        private async Task CreateCopyOfRoutes()
        {
            var formulariesRoutes = _unitOfWork.FormularyRouteRepository.ItemsAsReadOnly.ToList();

            formulariesRoutes?.Each(rec =>
            {
                if (rec != null)
                {
                    rec.Source = TerminologyConstants.MANUAL_DATA_SRC;
                    _unitOfWork.FormularyLocalRouteFormularyRepository.Add(_mapper.Map<FormularyLocalRouteDetail>(rec));
                }
            });
        }

        private void UpdateLicensedAndUnlicensedUse()
        {
            var updateLicensedUses = _unitOfWork.FormularyDetailFormularyRepository.Items.Where(rec => rec.LicensedUse != null).ToList();

            var updateUnlicensedUses = _unitOfWork.FormularyDetailFormularyRepository.Items.Where(rec => rec.UnlicensedUse != null).ToList();

            updateLicensedUses?.Each(rec =>
            {
                if (rec.LicensedUse.IsNotEmpty())
                {
                    rec.LocalLicensedUse = rec.LicensedUse.Replace("\"Source\":\"FDB\"", "\"Source\":\"" + TerminologyConstants.MANUAL_DATA_SRC + "\"");

                    _unitOfWork.FormularyDetailFormularyRepository.Update(rec);
                }
            });

            updateUnlicensedUses?.Each(rec =>
            {
                if (rec.UnlicensedUse.IsNotEmpty())
                {
                    rec.LocalUnlicensedUse = rec.UnlicensedUse.Replace("\"Source\":\"FDB\"", "\"Source\":\"" + TerminologyConstants.MANUAL_DATA_SRC + "\"");

                    _unitOfWork.FormularyDetailFormularyRepository.Update(rec);
                }
            });

        }
        */
        #endregion old code - ref only
    }
}
