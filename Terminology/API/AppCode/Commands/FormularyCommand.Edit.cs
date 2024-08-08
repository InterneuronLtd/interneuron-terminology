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
using Interneuron.Terminology.API.AppCode.Commands.EditMergeHandlers;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
using Interneuron.Terminology.API.AppCode.Extensions;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Model.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Commands
{
    public partial class FormularyCommand
    {
        public async Task<CreateEditFormularyDTO> UpdateFormulary(CreateEditFormularyRequest request, Action<List<string>> onUpdate = null)
        {
            var response = new CreateEditFormularyDTO
            {
                Status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS, StatusMessage = "", ErrorMessages = new List<string>() },
                Data = new List<FormularyDTO>()
            };

            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var uniqueIds = request.RequestsData.Select(req => req.FormularyVersionId).Distinct().ToList();

            //Considering non-deleted records only, for this comparision
            var existingFormulariesFromDB = formularyRepo.GetFormularyListForIds(uniqueIds, true).ToList();

            var hasValidRecords = ValidateAllRecords(existingFormulariesFromDB, request.RequestsData, response);

            if (!hasValidRecords)
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                return response;
            }

            //handled in studio
            //if (!(await GetHeaderRecordsLock(uniqueIds)))
            //{
            //    var errorMsg = $"This formulary details {string.Join(", ", uniqueIds)} cannot be saved as it has already been modified and a new version of it exists in the system. Please take latest version and try updating.";
            //    response.Status.ErrorMessages.Add(errorMsg);
            //    response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
            //    return response;
            //}

            var uniqueCodes = existingFormulariesFromDB.Select(req => req.Code).Distinct().ToList();

            //get existing max versionids of these codes including non-latest
            var codeVersionIdList = formularyRepo.ItemsAsReadOnly.Where(rec => uniqueCodes.Contains(rec.Code)).ToList();

            var codeVersionIdLkp = new Dictionary<string, int?>();

            //Not required - Arun - For ref only
            //var codeVersionIdLkp = codeVersionIdList?
            //   .Select(rec => new { CodeStatus = $"{rec.Code}|{rec.RecStatusCode}", VersionId = rec.VersionId })
            //   .GroupBy(rec => rec.CodeStatus, rec => rec.VersionId, (k, v) => new { CodeStatus = k, VersionId = v.Max() })
            //   .Distinct(rec => rec.CodeStatus)
            //   .ToDictionary(k => k.CodeStatus, v => v.VersionId) ?? new Dictionary<string, int?>();

            //This is to get all versions of records
            var existingFormulariesByCodes = formularyRepo.GetLatestFormulariesByCodes(uniqueCodes.ToArray()).ToList();

            var toBeSavedFormularies = new List<FormularyHeader>();

            var newRecordsPersisted = new List<FormularyHeader>();

            uniqueIds.Each(recId =>
            {
                var updatedRecord = UpdateRecord(recId, request, existingFormulariesFromDB, formularyRepo, existingFormulariesByCodes, codeVersionIdLkp);
                newRecordsPersisted.Add(updatedRecord);
            });

            formularyRepo.SaveChanges();

            //try
            //{
            //    formularyRepo.SaveChanges();
            //    await formularyRepo.ReleaseHeaderRecordsLock(uniqueIds);
            //}
            //catch
            //{
            //    //Re-try releasing the lock
            //    try
            //    {
            //        await formularyRepo.ReleaseHeaderRecordsLock(uniqueIds);
            //    }
            //    catch { }
            //    throw;
            //}

            if (newRecordsPersisted.IsCollectionValid())
            {
                newRecordsPersisted.Each(rec =>
                {
                    RePopulateDTOPostSave(rec, response);
                });
            }

            return response;
        }

        public async Task<bool> GetHeaderRecordsLock(List<string> formularyVersionIds)
        {
            if (!formularyVersionIds.IsCollectionValid()) return true;
            try
            {
                formularyVersionIds = formularyVersionIds.Distinct().ToList();
                var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

                return await formularyRepo.GetHeaderRecordsLock(formularyVersionIds);
            }
            catch { }
            return false;
        }

        public async Task TryReleaseHeaderRecordsLock(List<string> formularyVersionIds)
        {
            if (!formularyVersionIds.IsCollectionValid()) return;

            try
            {
                var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;
                formularyVersionIds = formularyVersionIds.Distinct().ToList();

                try
                {
                    await formularyRepo.ReleaseHeaderRecordsLock(formularyVersionIds);
                }
                catch
                {
                    //Re-try releasing the lock
                    try
                    {
                        await formularyRepo.ReleaseHeaderRecordsLock(formularyVersionIds);
                    }
                    catch { }
                    throw;
                }
            }
            catch { }
        }

        private async Task<List<string>> PostUpdate(List<string> uniqueCodes)
        {
            var formularyBasicResultsRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

            var ancestors = await formularyBasicResultsRepo.GetFormularyAncestorForCodes(uniqueCodes.ToArray());

            var descendents = await formularyBasicResultsRepo.GetFormularyDescendentForCodes(uniqueCodes.ToArray());

            var allCodes = new List<string>();

            if (ancestors.IsCollectionValid())
            {
                var ancestorUniqueCodes = ancestors.Select(rec => rec.Code).Distinct().ToList();
                allCodes.AddRange(ancestorUniqueCodes);
            }

            if (descendents.IsCollectionValid())
            {
                var descendentUniqueCodes = descendents.Select(rec => rec.Code).Distinct().ToList();
                allCodes.AddRange(descendentUniqueCodes);
            }

            return allCodes?.Distinct()?.ToList();
        }

        private FormularyHeader UpdateRecord(string recId, CreateEditFormularyRequest request, List<FormularyHeader> existingFormulariesFromDB, IFormularyRepository<FormularyHeader> formularyRepo, List<FormularyHeader> existingFormulariesByCodes, Dictionary<string, int?> codeVersionIdLkp)
        {
            var recFromRequest = request.RequestsData.Single(rec => rec.FormularyVersionId == recId);

            var existingRecFromDb = existingFormulariesFromDB.Single(rec => rec.FormularyVersionId == recId);

            //var existingRecFromDbWithSameStatus = existingFormulariesByCodes.FirstOrDefault(rec => rec.Code == recFromRequest.Code && rec.RecStatusCode == recFromRequest.RecStatusCode);

            var newRecordToUpdate = MergeRecordFromDbWithRequestData(existingRecFromDb, recFromRequest);

            //Commented below code to avoid record going into deleted status
            //if (recFromRequest.RecStatusCode == TerminologyConstants.RECORDSTATUS_APPROVED)
            //{
            //    //Check if there are any other latest records with same code apart from the current record
            //    var duplicateFormulariesWithSameCodeFromDb = existingFormulariesByCodes.Where(dup => dup.Code == recFromRequest.Code && dup.FormularyVersionId != recId && dup.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED && dup.IsLatest == true);

            //    //Mark those other records as deleted
            //    if (duplicateFormulariesWithSameCodeFromDb.IsCollectionValid())
            //    {
            //        DeleteRecordWithSimilarCodeForEdit(duplicateFormulariesWithSameCodeFromDb, formularyRepo);
            //    }
            //}

            /* Not required - just increment it from the existing
            newRecordToUpdate.VersionId = 1;
            if (codeVersionIdLkp != null && codeVersionIdLkp.ContainsKey($"{newRecordToUpdate.Code}|{newRecordToUpdate.RecStatusCode}"))
                newRecordToUpdate.VersionId = codeVersionIdLkp[$"{newRecordToUpdate.Code}|{newRecordToUpdate.RecStatusCode}"] + 1;
            */

            newRecordToUpdate.VersionId = existingRecFromDb.VersionId + 1;

            //if is 'dmd deleted', then new record will be saved as 'Deleted'.
            //if the record is a parent node e,g. vtm, then set all its descendents to 'DELETED'
            if (newRecordToUpdate.IsDmdDeleted == true && newRecordToUpdate.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)
                newRecordToUpdate.RecStatusCode = TerminologyConstants.RECORDSTATUS_DELETED;

            formularyRepo.Add(newRecordToUpdate);

            existingRecFromDb.IsLatest = false;

            formularyRepo.Update(existingRecFromDb);

            //mmc-477 - this case incorrect added and removed -- for ref only
            //if the previous code is different, set the islatest of that also to false - not required now
            //if (existingRecFromDbWithSameStatus != null && (existingRecFromDbWithSameStatus.Code == existingRecFromDb.Code && existingRecFromDbWithSameStatus.RecStatusCode != existingRecFromDb.RecStatusCode))
            //{
            //    existingRecFromDbWithSameStatus.IsLatest = false;
            //    formularyRepo.Update(existingRecFromDbWithSameStatus);
            //}

            //if is 'dmd deleted', then new record will be saved as 'Deleted'.
            //if the record is a parent node e,g. vtm, then set all its descendents to 'DELETED'
            if (newRecordToUpdate.IsDmdDeleted == true)
                CheckAndUpdateDescendentRecordsStatus(formularyRepo, newRecordToUpdate);

            var existingTargetStatusRecords = formularyRepo.GetLatestFormulariesByCodes(new string[] { newRecordToUpdate.Code }, true)
                    .Where(rec => rec.Code == newRecordToUpdate.Code && rec.IsLatest == true)
                    .ToList();

            //MMC-477: If recstscd of new updatable rec is changed and is set '002' or '003' - move the record already in 002 or 003 to archive i.e '004'
            if ((newRecordToUpdate.RecStatusCode != existingRecFromDb.RecStatusCode) && (newRecordToUpdate.RecStatusCode == TerminologyConstants.RECORDSTATUS_APPROVED || newRecordToUpdate.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE))
            {
                var existingTargetRecs = existingTargetStatusRecords.Where(rec => newRecordToUpdate.RecStatusCode == rec.RecStatusCode)?.ToList();

                if (existingTargetRecs.IsCollectionValid())
                    UpdateExistingRecordWithTargetStatusToArchiveIfExists(formularyRepo, newRecordToUpdate.Code, new string[] { newRecordToUpdate.RecStatusCode }, existingTargetRecs);
            }

            if ((newRecordToUpdate.RecStatusCode != existingRecFromDb.RecStatusCode) && (newRecordToUpdate.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT && existingRecFromDb.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED))
            {
                var existingTargetRecs = existingTargetStatusRecords.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?.ToList();

                existingTargetRecs = existingTargetRecs?.Where(rec => rec.FormularyId != newRecordToUpdate.FormularyId).ToList();

                if (existingTargetRecs.IsCollectionValid())
                    UpdateExistingRecordsWithDraftStatusToArchiveIfExists(formularyRepo, newRecordToUpdate.Code, newRecordToUpdate.FormularyId, existingTargetRecs);
            }

            return newRecordToUpdate;
        }

        private void CheckAndUpdateDescendentRecordsStatus(IFormularyRepository<FormularyHeader> formularyRepo, FormularyHeader newRecordToUpdate)
        {
            //If VTM or VMP is DM+D deleted
            if (newRecordToUpdate.IsDmdDeleted == true && !((string.Compare(newRecordToUpdate.ProductType, "vtm", true) == 0) || (string.Compare(newRecordToUpdate.ProductType, "vmp", true) == 0)))
                return;

            var descendentsWithParentLkp = formularyRepo.GetDescendentFormularyIdsForFormularyIdsAsLookup(new List<string> { newRecordToUpdate.FormularyId });

            if (!descendentsWithParentLkp.IsCollectionValid()) return;

            var descendentFormularyIds = new List<string>();

            descendentsWithParentLkp.Values.Each(rec => descendentFormularyIds.AddRange(rec));

            if (!descendentFormularyIds.IsCollectionValid()) return;

            var descendentFormularies = formularyRepo.GetFormularyListForFormularyIds(descendentFormularyIds)
                .Where(rec => rec.IsLatest == true && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED)
                ?.ToList();

            if (!descendentFormularies.IsCollectionValid()) return;

            foreach (var descendentFormulary in descendentFormularies)
            {
                var newCloned = descendentFormulary.CloneFormularyV2(_mapper);

                newCloned.VersionId = descendentFormulary.VersionId + 1;
                newCloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_DELETED;
                formularyRepo.Add(newCloned);

                descendentFormulary.IsLatest = false;
                formularyRepo.Update(descendentFormulary);
            }
        }

        private FormularyHeader MergeRecordFromDbWithRequestData(FormularyHeader recToUpdate, FormularyDTO recFromRequest)
        {
            if (string.Compare(recToUpdate.ProductType, "vtm", true) == 0)
                return new VTMEditMergeHandler(_mapper).Merge(recToUpdate, recFromRequest);
            if (string.Compare(recToUpdate.ProductType, "vmp", true) == 0)
                return new VMPEditMergeHandler(_mapper).Merge(recToUpdate, recFromRequest);
            if (string.Compare(recToUpdate.ProductType, "amp", true) == 0)
                return new AMPEditMergeHandler(_mapper).Merge(recToUpdate, recFromRequest);

            return null;
        }


        //MMC-477: Fix-01: Not in use
        //private bool DeleteRecordWithSimilarCodeForEdit(IEnumerable<FormularyHeader> duplicateFormulariesWithSameCodeFromDb, IFormularyRepository<FormularyHeader> formularyRepo)
        //{
        //    //Update the old records with IsLatest = false
        //    //Add new record with Delete status

        //    if (!duplicateFormulariesWithSameCodeFromDb.IsCollectionValid()) return false;

        //    duplicateFormulariesWithSameCodeFromDb.Each(dup =>
        //    {
        //        if (dup != null)
        //        {
        //            var rootEntityIdentifier = Guid.NewGuid().ToString();

        //            var dupAsNew = dup.CloneFormulary(_mapper, rootEntityIdentifier);

        //            dupAsNew.RecStatusCode = TerminologyConstants.RECORDSTATUS_DELETED;
        //            dupAsNew.RecStatuschangeDate = DateTime.UtcNow;
        //            dupAsNew.FormularyVersionId = rootEntityIdentifier;
        //            dupAsNew.IsLatest = true;
        //            dupAsNew.VersionId = dup.VersionId + 1;

        //            formularyRepo.Add(dupAsNew);

        //            dup.IsLatest = false;
        //            formularyRepo.Update(dup);
        //        }
        //    });

        //    return true;
        //}



        private bool ValidateAllRecords(List<FormularyHeader> existingFormulariesFromDB, List<FormularyDTO> requestDTOs, CreateEditFormularyDTO response)
        {
            if (!existingFormulariesFromDB.IsCollectionValid())
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                response.Status.ErrorMessages.Add(NO_MATCHING_RECORDS_MSG);
                return false;
            }

            var hasAnyInvalidRecs = false;

            var uniqueCodes = existingFormulariesFromDB.Select(rec => rec.Code).ToList();

            //var uniqueFormularyIds = existingFormulariesFromDB.Select(rec => rec.FormularyId).ToList();
            //MMC-477 - Should check againt formularyversionid
            var uniqueFormularyVersionIds = existingFormulariesFromDB.Select(rec => rec.FormularyVersionId).ToList();

            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            //This will get the other records with same code that are in non-deleted or non-archived status
            //MMC-477 - Should check againt formularyversionid
            //var otherFormulariesForCodesFromDb = formularyRepo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && uniqueCodes.Contains(rec.Code) && !uniqueFormularyIds.Contains(rec.FormularyId) && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED).ToList();

            //MMC-477: Not relevant now. The older one will be archieved.
            //var otherFormulariesForCodesFromDb = formularyRepo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && uniqueCodes.Contains(rec.Code)
            //&& !uniqueFormularyVersionIds.Contains(rec.FormularyVersionId) && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED).ToList();


            requestDTOs.Each(recDTO =>
            {
                //Is this record exists and is a latest record in the system - It might have updated already in the system - Do not update then
                var existingRec = existingFormulariesFromDB.Where(rec => rec.FormularyVersionId == recDTO.FormularyVersionId)?.FirstOrDefault();
                //if (!existingFormulariesFromDB.Any(rec => rec.FormularyVersionId == recDTO.FormularyVersionId && rec.IsLatest.GetValueOrDefault() == true))
                if (existingRec == null || existingRec.IsLatest == null || existingRec.IsLatest == false)
                {
                    hasAnyInvalidRecs = true;
                    response.Status.ErrorMessages.Add("This record does not exist or is not latest in the system: Name: {0}".ToFormat(existingRec == null ? "" : $"{existingRec.Code}-{existingRec.Name}"));//.FormularyVersionId));
                }


                //If any other record with same code exists and with same record status (other than Deleted or Archived), then it cannot be updated
                //MMC-477: Not relevant now. The older one will be archieved.
                //if (otherFormulariesForCodesFromDb.IsCollectionValid() && otherFormulariesForCodesFromDb.Any(rec => rec.RecStatusCode == recDTO.RecStatusCode))
                //{
                //    hasAnyInvalidRecs = true;
                //    response.Status.ErrorMessages.Add("The record with the same code and same status already exists in the system. Please archive or the delete the other record and re-try. Id: {0}".ToFormat(recDTO.FormularyVersionId));
                //}
            });

            if (hasAnyInvalidRecs) return false;
            return true;
        }



    }
}
