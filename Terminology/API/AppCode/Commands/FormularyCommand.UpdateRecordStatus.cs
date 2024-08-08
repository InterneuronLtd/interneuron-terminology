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
using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
using Interneuron.Terminology.API.AppCode.Extensions;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Model.Search;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Commands
{
    public partial class FormularyCommand : IFormularyCommands
    {
        const string NO_MATCHING_RECORDS_MSG = "No matching records found in the system.\n";
        const string NO_MATCHING_RECORD_MSG = "No matching record for {0} found in the system.\n";
        const string ALREADY_UPDATED_RECORD_MSG = "This record {0} has already been updated in the system.";


        public async Task<UpdateFormularyRecordStatusDTO> UpdateFormularyRecordStatus(UpdateFormularyRecordStatusRequest request, Action<List<string>> onUpdate = null)
        {
            var response = new UpdateFormularyRecordStatusDTO
            {
                Status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS, StatusMessage = "", ErrorMessages = new List<string>() },
                Data = new List<FormularyDTO>()
            };

            if (request == null || !request.RequestData.IsCollectionValid())
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

                return response;
            }

            var recordsToUpdate = request.RequestData;

            var requests = new List<UpdateFormularyRecordStatusRequestData>();

            recordsToUpdate.Each(r =>
            {
                if (r.FormularyVersionId.IsNotEmpty() && r.RecordStatusCode.IsNotEmpty())
                {
                    r.FormularyVersionId = r.FormularyVersionId.Trim();
                    r.RecordStatusCode = r.RecordStatusCode.Trim();
                    r.RecordStatusCodeChangeMsg = r.RecordStatusCodeChangeMsg?.Trim();
                    requests.Add(r);
                }
            });

            if (!requests.IsCollectionValid())
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

                return response;
            }

            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var uniqueFormularyVersionIds = requests.Select(r => r.FormularyVersionId).Distinct().ToList();

            //var formulariesFromDbForIds = formularyRepo.GetFormularyListForIds(uniqueFormularyVersionIds, true).ToList();

            var formulariesHeaderOnlyFromDbForIds = GetFormularyHeaderOnlyForFVIds(uniqueFormularyVersionIds);

            var hasValidRecs = ValidateRecords(formulariesHeaderOnlyFromDbForIds, requests, response);

            if (!hasValidRecs)
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                return response;
            }

            #region old code ref only
            //var uniqueCodes = formulariesHeaderOnlyFromDbForIds.Select(req => req.Code).Distinct().ToList();

            //get existing max versionids of these codes including non-latest
            //var codeVersionIdList = formularyRepo.ItemsAsReadOnly.Where(rec => uniqueCodes.Contains(rec.Code)).ToList();

            //var codeVersionIdLkp = codeVersionIdList?
            //   .Select(rec => new { CodeStatus = $"{rec.Code}|{rec.RecStatusCode}", VersionId = rec.VersionId })
            //   .GroupBy(rec => rec.CodeStatus, rec => rec.VersionId, (k, v) => new { CodeStatus = k, VersionId = v.Max() })
            //   .Distinct(rec => rec.CodeStatus)
            //   .ToDictionary(k => k.CodeStatus, v => v.VersionId) ?? new Dictionary<string, int?>();

            //This is to get all versions of records
            //var existingFormulariesByCodes = formularyRepo.GetLatestFormulariesByCodes(uniqueCodes.ToArray()).ToList();

            //foreach (var recId in uniqueFormularyVersionIds)
            //{
            //    var recordToUpdate = requests.SingleOrDefault(rec => rec.FormularyVersionId == recId);
            //    //UpdateRecordStatus(recordToUpdate, response, formulariesFromDbForIds, codeVersionIdLkp, formularyRepo, existingFormulariesByCodes);
            //    UpdateRecordStatus(recordToUpdate, response, formulariesHeaderOnlyFromDbForIds, formularyRepo, formulariesFromDbForIdsAsReadOnly, editableFormulariesHeaderOnly);
            //}
            #endregion

            var batchSize = 100;
            var batchedRequests = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < uniqueFormularyVersionIds.Count; reqIndex += batchSize)
            {
                var batches = uniqueFormularyVersionIds.Skip(reqIndex).Take(batchSize);
                batchedRequests.Add(batches.ToList());
            }

            var allUniqueCodes = new List<string>();

            foreach (var batch in batchedRequests)
            {
                var formulariesFromDbForIdsAsReadOnly = GetFormularyListForIdsInBatchAsReadLOnly(batch);
                var editableFormulariesHeaderOnly = new Dictionary<string, FormularyHeader>();
                var uniqueCodes = new List<string>();

                formularyRepo.Items.Where(rec => batch.Contains(rec.FormularyVersionId))?.ToList()?.Each(rec =>
                {
                    uniqueCodes.Add(rec.Code);
                    allUniqueCodes.Add(rec.Code);
                    editableFormulariesHeaderOnly[rec.FormularyVersionId] = rec;
                });

                //var existingTargetStatusRecordsForCodes = new ConcurrentDictionary<string, List<FormularyHeader>>();

                var interestedStatuses = new HashSet<string> { TerminologyConstants.RECORDSTATUS_DRAFT, TerminologyConstants.RECORDSTATUS_ACTIVE, TerminologyConstants.RECORDSTATUS_APPROVED };

                var existingTargetStatusRecordsForCodes = formularyRepo.GetLatestFormulariesByCodes(uniqueCodes.ToArray(), true)
                    .AsEnumerable()
                    ?.Where(rec => rec.IsLatest == true && interestedStatuses.Contains(rec.RecStatusCode))
                    //?.ToList()
                    .AsParallel()
                    .ToLookup(rec => rec.Code, rec => rec);

                var recordsToUpdateInTheBatch = requests.Where(rec => batch.Contains(rec.FormularyVersionId)).ToList();

                UpdateRecordStatus(recordsToUpdateInTheBatch, response, formulariesHeaderOnlyFromDbForIds, formularyRepo, formulariesFromDbForIdsAsReadOnly, editableFormulariesHeaderOnly, existingTargetStatusRecordsForCodes);
            }

            ////Need to end lock here
            //if (allUniqueCodes.IsCollectionValid()) 
            //{
            //    var allModifiedFVIds = formularyRepo.ItemsAsReadOnly
            //        .Where(rec => allUniqueCodes.Contains(rec.Code))
            //        .Select(rec => rec.FormularyVersionId)
            //        .Distinct()
            //        .ToList();
            //try
            //    {

            //        await formularyRepo.ReleaseHeaderRecordsLock(allModifiedFVIds);
            //    }
            //    catch
            //    {
            //        //Re-try releasing the lock
            //        try
            //        {
            //            await formularyRepo.ReleaseHeaderRecordsLock(allModifiedFVIds);
            //        }
            //        catch { }
            //        throw;
            //    }
            //}

            if (onUpdate != null)
            {
                var codes = formulariesHeaderOnlyFromDbForIds?.Select(rec => rec.Code)?.Distinct()?.ToList();

                var completeHierarchy = await PostUpdate(codes);

                if (completeHierarchy.IsCollectionValid())
                {
                    onUpdate?.Invoke(completeHierarchy);
                }
            }

            return response;
        }

        private Dictionary<string, FormularyHeader> GetFormularyListForIdsInBatchAsReadLOnly(List<string> uniqueFormularyVersionIds)
        {
            var formulariesFromDbForIds = new Dictionary<string, FormularyHeader>();
            var formulariesDetailFromDbForIds = new Dictionary<string, List<FormularyDetail>>();
            var formulariesAdditionalCodeFromDbForIds = new Dictionary<string, List<FormularyAdditionalCode>>();
            var formulariesRouteDetailFromDbForIds = new Dictionary<string, List<FormularyRouteDetail>>();
            var formulariesLocalRouteDetailFromDbForIds = new Dictionary<string, List<FormularyLocalRouteDetail>>();
            var formulariesIngredientFromDbForIds = new Dictionary<string, List<FormularyIngredient>>();

            if (!uniqueFormularyVersionIds.IsCollectionValid()) return formulariesFromDbForIds;

            var formularyHeaderRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;
            var formularyDetailRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyDetail>)) as IFormularyRepository<FormularyDetail>;
            var formularyRouteRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyRouteDetail>)) as IFormularyRepository<FormularyRouteDetail>;
            var formularyLocalRouteDetailRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyLocalRouteDetail>)) as IFormularyRepository<FormularyLocalRouteDetail>;
            var formularyIngredientRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyIngredient>)) as IFormularyRepository<FormularyIngredient>;
            var formularyAdditionalCodeRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyAdditionalCode>)) as IFormularyRepository<FormularyAdditionalCode>;

            var batchSize = 1000;
            var batchedRequests = new List<List<string>>();

            for (var reqIndex = 0; reqIndex < uniqueFormularyVersionIds.Count; reqIndex += batchSize)
            {
                var batches = uniqueFormularyVersionIds.Skip(reqIndex).Take(batchSize);
                batchedRequests.Add(batches.ToList());
            }

            foreach (var batch in batchedRequests)
            {
                var headerRecs = formularyHeaderRepo.ItemsAsReadOnly.Where(rec => batch.Contains(rec.FormularyVersionId)).ToList();//.GetFormularyListForIds(batch).ToList();
                if (!headerRecs.IsCollectionValid()) continue;
                formularyDetailRepo.ItemsAsReadOnly.Where(rec => batch.Contains(rec.FormularyVersionId))?.ToList()?.Each(rec =>
                {
                    if (!formulariesDetailFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        formulariesDetailFromDbForIds[rec.FormularyVersionId] = new List<FormularyDetail>();
                    formulariesDetailFromDbForIds[rec.FormularyVersionId].Add(rec);
                });
                formularyRouteRepo.ItemsAsReadOnly.Where(rec => batch.Contains(rec.FormularyVersionId))?.ToList()?.Each(rec =>
                {
                    if (!formulariesRouteDetailFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        formulariesRouteDetailFromDbForIds[rec.FormularyVersionId] = new List<FormularyRouteDetail>();
                    formulariesRouteDetailFromDbForIds[rec.FormularyVersionId].Add(rec);
                });
                formularyLocalRouteDetailRepo.ItemsAsReadOnly.Where(rec => batch.Contains(rec.FormularyVersionId))?.ToList()?.Each(rec =>
                {
                    if (!formulariesLocalRouteDetailFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        formulariesLocalRouteDetailFromDbForIds[rec.FormularyVersionId] = new List<FormularyLocalRouteDetail>();
                    formulariesLocalRouteDetailFromDbForIds[rec.FormularyVersionId].Add(rec);
                });
                formularyAdditionalCodeRepo.ItemsAsReadOnly.Where(rec => batch.Contains(rec.FormularyVersionId))?.ToList()?.Each(rec =>
                {
                    if (!formulariesAdditionalCodeFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        formulariesAdditionalCodeFromDbForIds[rec.FormularyVersionId] = new List<FormularyAdditionalCode>();
                    formulariesAdditionalCodeFromDbForIds[rec.FormularyVersionId].Add(rec);
                });
                formularyIngredientRepo.ItemsAsReadOnly.Where(rec => batch.Contains(rec.FormularyVersionId))?.ToList()?.Each(rec =>
                {
                    if (!formulariesIngredientFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        formulariesIngredientFromDbForIds[rec.FormularyVersionId] = new List<FormularyIngredient>();
                    formulariesIngredientFromDbForIds[rec.FormularyVersionId].Add(rec);
                });

                headerRecs.Each(rec =>
                {
                    if (formulariesDetailFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        rec.FormularyDetail = formulariesDetailFromDbForIds[rec.FormularyVersionId];
                    if (formulariesAdditionalCodeFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        rec.FormularyAdditionalCode = formulariesAdditionalCodeFromDbForIds[rec.FormularyVersionId];
                    if (formulariesIngredientFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        rec.FormularyIngredient = formulariesIngredientFromDbForIds[rec.FormularyVersionId];
                    if (formulariesRouteDetailFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        rec.FormularyRouteDetail = formulariesRouteDetailFromDbForIds[rec.FormularyVersionId];
                    if (formulariesLocalRouteDetailFromDbForIds.ContainsKey(rec.FormularyVersionId))
                        rec.FormularyLocalRouteDetail = formulariesLocalRouteDetailFromDbForIds[rec.FormularyVersionId];

                    formulariesFromDbForIds[rec.FormularyVersionId] = rec;
                });
            }
            return formulariesFromDbForIds;
        }

        private List<FormularyHeader> GetFormularyHeaderOnlyForFVIds(List<string> fvIds)
        {
            if (!fvIds.IsCollectionValid()) return new List<FormularyHeader>();
            var repo = this._provider.GetService(typeof(IReadOnlyRepository<FormularyHeader>)) as IReadOnlyRepository<FormularyHeader>;
            var formulariesHeaders = repo.ItemsAsReadOnly.Where(rec => fvIds.Contains(rec.FormularyVersionId)).ToList();
            if (!formulariesHeaders.IsCollectionValid()) return new List<FormularyHeader>();

            return formulariesHeaders;
        }

        public async Task<UpdateFormularyRecordStatusDTO> BulkUpdateFormularyRecordStatus(UpdateFormularyRecordStatusRequest request)
        {
            var response = new UpdateFormularyRecordStatusDTO
            {
                Status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS, StatusMessage = "", ErrorMessages = new List<string>() },
                Data = new List<FormularyDTO>()
            };

            if (request == null || !request.RequestData.IsCollectionValid())
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

                return response;
            }

            var recordsToUpdate = request.RequestData;

            var requests = new List<UpdateFormularyRecordStatusRequestData>();

            recordsToUpdate.Each(r =>
            {
                if (r.FormularyVersionId.IsNotEmpty() && r.RecordStatusCode.IsNotEmpty())
                {
                    r.FormularyVersionId = r.FormularyVersionId.Trim();
                    r.RecordStatusCode = r.RecordStatusCode.Trim();
                    requests.Add(r);
                }
            });

            if (!recordsToUpdate.IsCollectionValid())
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

                return response;
            }

            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            var uniqueFormularyVersionIds = recordsToUpdate.Select(r => r.FormularyVersionId).Distinct().ToList();

            var formulariesFromDbForIds = GetFormularyHeaderOnlyForFVIds(uniqueFormularyVersionIds);// formularyRepo.GetFormularyListForIds(uniqueFormularyVersionIds, true).ToList();

            var hasValidRecs = ValidateRecords(formulariesFromDbForIds, recordsToUpdate, response);

            if (!hasValidRecs)
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                return response;
            }

            return response;
        }


        private bool ValidateRecords(List<FormularyHeader> formulariesFromDbForIds, List<UpdateFormularyRecordStatusRequestData> recordsToUpdate, UpdateFormularyRecordStatusDTO response)
        {
            if (!formulariesFromDbForIds.IsCollectionValid())
            {
                response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                response.Status.ErrorMessages.Add(NO_MATCHING_RECORDS_MSG);
                return false;
            }

            //if (formulariesFromDbForIds.Any(rec => string.Compare(rec.ProductType, "amp", true) != 0))
            //{
            //    response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
            //    response.Status.ErrorMessages.Add("Only records at the AMP level can be edited.");
            //    return false;
            //}

            var hasInvalidRecs = false;

            var uniqueCodes = formulariesFromDbForIds.Select(rec => rec.Code).ToList();

            //var uniqueFormularyIds = formulariesFromDbForIds.Select(rec => rec.FormularyId).ToList();
            //MMC-477 - Should check againt formularyversionid
            var uniqueFormularyVersionIds = formulariesFromDbForIds.Select(rec => rec.FormularyVersionId).ToList();

            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            //This will get the other records with same code that are in non-deleted or non-archived status
            //MMC-477 - Should check againt formularyversionid
            //var otherFormulariesForCodesFromDb = formularyRepo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && uniqueCodes.Contains(rec.Code) && !uniqueFormularyIds.Contains(rec.FormularyId) && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED).ToList();
            //Not required for now
            //var otherFormulariesForCodesFromDb = formularyRepo.ItemsAsReadOnly
            //    .Where(rec => rec.IsLatest == true && uniqueCodes.Contains(rec.Code) && !uniqueFormularyVersionIds.Contains(rec.FormularyVersionId) && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED)
            //    .ToList();

            //MMC-477 - Performance tuning
            var codeNameFormularyVersionIdLkp = formularyRepo.ItemsAsReadOnly
                .Where(rec => rec.IsLatest == true && uniqueFormularyVersionIds.Contains(rec.FormularyVersionId))?
                .Select(rec => new { FormularyVersionId = rec.FormularyVersionId, CodeName = $"{rec.Code}|{rec.Name}" })
                .AsEnumerable()
                .Distinct(rec => rec.FormularyVersionId)
                .ToDictionary(k => k.FormularyVersionId, v => v.CodeName);

            recordsToUpdate.AsParallel().ForAll(recToUpdate =>
            {
                //MMC-477 - Performance tuning
                //var code = formularyRepo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && rec.FormularyVersionId == recToUpdate.FormularyVersionId).Select(rec => rec.Code).FirstOrDefault();
                //var name = formularyRepo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && rec.FormularyVersionId == recToUpdate.FormularyVersionId).Select(rec => rec.Name).FirstOrDefault();
                var codeName = (codeNameFormularyVersionIdLkp != null && codeNameFormularyVersionIdLkp.ContainsKey(recToUpdate.FormularyVersionId) ? codeNameFormularyVersionIdLkp[recToUpdate.FormularyVersionId] : " | ").Split("|");
                var code = codeName[0];
                var name = codeName[1];

                //Is this record exist and is a latest record in the system - It might have updated already in the system - Do not update then
                if (!formulariesFromDbForIds.Any(rec => rec.FormularyVersionId == recToUpdate.FormularyVersionId && rec.IsLatest.GetValueOrDefault() == true))
                {
                    hasInvalidRecs = true;
                    response.Status.ErrorMessages.Add("This record does not exist or is not latest in the system: Id: {0}".ToFormat("(" + code + ")" + " " + name));
                }

                //If any other record with same code exists and with same record status (other than Deleted or Archived), then it cannot be updated
                //MMC-477: Not relevant now. The older one will be archived.
                //if (otherFormulariesForCodesFromDb.IsCollectionValid() && otherFormulariesForCodesFromDb.Any(rec => rec.RecStatusCode == recToUpdate.RecordStatusCode && rec.Code == code))
                //{
                //    hasInvalidRecs = true;
                //    response.Status.ErrorMessages.Add("The record with the same code and same status already exists in the system. Please archive the other record and re-try. Code: {0}".ToFormat("(" + code + ")" + " " + name));
                //}

                if (formulariesFromDbForIds.Any(rec => rec.RecStatusCode == recToUpdate.RecordStatusCode && rec.FormularyVersionId == recToUpdate.FormularyVersionId && rec.IsLatest == true))
                {
                    hasInvalidRecs = true;
                    response.Status.ErrorMessages.Add("The record you are trying to update already has the same status. Please use different status and re-try. Code: {0}".ToFormat("(" + code + ")" + " " + name));
                }
            });


            if (hasInvalidRecs) return false;
            return true;
        }

        //private bool UpdateRecordStatus(UpdateFormularyRecordStatusRequestData recordToUpdate, UpdateFormularyRecordStatusDTO response, List<FormularyHeader> formulariesFromDb, IFormularyRepository<FormularyHeader> formularyRepo)
        //private void UpdateRecordStatus(UpdateFormularyRecordStatusRequestData recordToUpdate, UpdateFormularyRecordStatusDTO response, List<FormularyHeader> formulariesFromDb, Dictionary<string, int?> codeVersionIdLkp, IFormularyRepository<FormularyHeader> formularyRepo, List<FormularyHeader> existingFormulariesByCodes)
        private void UpdateRecordStatus(List<UpdateFormularyRecordStatusRequestData> recordsToUpdate, UpdateFormularyRecordStatusDTO response, List<FormularyHeader> formulariesHeaderOnlyFromDbForIds, IFormularyRepository<FormularyHeader> formularyRepo, Dictionary<string, FormularyHeader> formulariesFromDbForIdsAsReadOnly, Dictionary<string, FormularyHeader> editableFormulariesHeaderOnly, ILookup<string, FormularyHeader> existingTargetStatusRecordsForCodes)
        {
            foreach (var recordToUpdate in recordsToUpdate)
            {
                var isUpdatable = false;

                var matchingRecordInDb = formulariesHeaderOnlyFromDbForIds.Where(rec => rec.FormularyVersionId == recordToUpdate.FormularyVersionId).FirstOrDefault();

                if (matchingRecordInDb == null)
                {
                    response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
                    response.Status.ErrorMessages.Add(NO_MATCHING_RECORD_MSG.ToFormat(recordToUpdate.FormularyVersionId));
                    //return false;
                    return;
                }

                //No need to specially handle for approved as there cannot  be records or codes in the same status
                //if (recordToUpdate.RecordStatusCode == TerminologyConstants.RECORDSTATUS_APPROVED)
                //{
                //    isUpdatable = HandleApprovedStatus(matchingRecordInDb, recordToUpdate, formularyRepo);
                //}
                //else
                //{
                //    isUpdatable = UpdateStatusForRecord(matchingRecordInDb, recordToUpdate, formularyRepo);
                //}

                //isUpdatable = UpdateStatusForRecord(matchingRecordInDb, existingFormulariesByCodes, recordToUpdate, codeVersionIdLkp, formularyRepo);
                isUpdatable = UpdateStatusForRecord(matchingRecordInDb, recordToUpdate, formularyRepo, formulariesFromDbForIdsAsReadOnly, editableFormulariesHeaderOnly, existingTargetStatusRecordsForCodes);

                if (!isUpdatable)
                {
                    response.Status.StatusCode = TerminologyConstants.STATUS_FAIL;
                    response.Status.ErrorMessages.Add(NO_MATCHING_RECORD_MSG.ToFormat(recordToUpdate.FormularyVersionId));
                }

                //formularyRepo.SaveChanges();

                //return isUpdatable;
            }
            formularyRepo.SaveChanges();
        }

        //No need to specially handle for approved as there cannot  be records or codes in the same status
        //private bool HandleApprovedStatus(FormularyHeader matchingRecordInDb, UpdateFormularyRecordStatusRequestData recordToUpdate, IFormularyRepository<FormularyHeader> formularyRepo)
        //{
        //    var allFormulariesWithSameCodeFromDb = formularyRepo.GetLatestFormulariesByCodes(new string[] { matchingRecordInDb.Code }, true).ToList();//gets only the latest and also non-deleted

        //    if (!allFormulariesWithSameCodeFromDb.IsCollectionValid()) return false;

        //    //Any record other than the status change record and non-archived and non-deleted
        //    var duplicateFormulariesWithSameCodeFromDb = allFormulariesWithSameCodeFromDb.Where(dup => dup.FormularyVersionId != matchingRecordInDb.FormularyVersionId && dup.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED);

        //    if (duplicateFormulariesWithSameCodeFromDb.IsCollectionValid())
        //    {
        //        //For all these records - mark the status as deleted and update to db
        //        DeleteRecordWithSimilarCode(duplicateFormulariesWithSameCodeFromDb, formularyRepo);
        //    }

        //    return UpdateStatusForRecord(matchingRecordInDb, recordToUpdate, formularyRepo, true);


        //    ////Check whether this record is duplicate of any other record
        //    ////Then, delete the original record, else just update this record
        //    //if (matchingRecordInDb.IsDuplicate.HasValue && matchingRecordInDb.IsDuplicate.Value)
        //    //{
        //    //    var allFormulariesWithSameCodeFromDb = formularyRepo.GetLatestFormulariesByCodes(new string[] { matchingRecordInDb.Code }, true).ToList();//gets only the latest and also non-deleted

        //    //    if (!allFormulariesWithSameCodeFromDb.IsCollectionValid()) return false;



        //    //    //Any record other than the status change record and non-archived and non-deleted
        //    //    var duplicateFormulariesWithSameCodeFromDb = allFormulariesWithSameCodeFromDb.Where(dup => dup.FormularyVersionId != matchingRecordInDb.FormularyVersionId && dup.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED);

        //    //    if (duplicateFormulariesWithSameCodeFromDb.IsCollectionValid())
        //    //    {
        //    //        //For all these records - mark the status as deleted and update to db
        //    //        DeleteRecordWithSimilarCode(duplicateFormulariesWithSameCodeFromDb, formularyRepo);
        //    //    }

        //    //    return UpdateStatusForRecord(matchingRecordInDb, recordToUpdate, formularyRepo);
        //    //}

        //    //return UpdateStatusForRecord(matchingRecordInDb, recordToUpdate, formularyRepo);
        //}

        private bool UpdateStatusForRecord(FormularyHeader updatableFormulary, UpdateFormularyRecordStatusRequestData recordToUpdate, IFormularyRepository<FormularyHeader> formularyRepo, Dictionary<string, FormularyHeader> existingFormulariesForFVIdsAsReadOnly, Dictionary<string, FormularyHeader> editableFormulariesHeaderOnly, ILookup<string, FormularyHeader> existingTargetStatusRecordsForCodes, bool removeDuplicateFlag = false)
        {
            //Update the existing record with IsLatest = false and 
            //Create new record with new status

            if (updatableFormulary != null)
            {
                //Get Updatable record
                //var existingRecord = formularyRepo.GetFormularyListForIds(new List<string> { updatableFormulary.FormularyVersionId }).FirstOrDefault();
                var existingRecordinReadOnly = (existingFormulariesForFVIdsAsReadOnly.IsCollectionValid() && existingFormulariesForFVIdsAsReadOnly.ContainsKey(updatableFormulary.FormularyVersionId)) ? existingFormulariesForFVIdsAsReadOnly[updatableFormulary.FormularyVersionId] : formularyRepo.GetFormularyListForIds(new List<string> { updatableFormulary.FormularyVersionId }).FirstOrDefault();

                var existingEditableRecord = editableFormulariesHeaderOnly.ContainsKey(updatableFormulary.FormularyVersionId) ? editableFormulariesHeaderOnly[updatableFormulary.FormularyVersionId] : null;

                if (existingRecordinReadOnly == null || existingEditableRecord == null) return false;

                //var newFormularyHeader = existingRecord.CloneFormulary(_mapper, rootEntityIdentifier);
                var newFormularyHeader = existingRecordinReadOnly.CloneFormularyV2(_mapper);

                newFormularyHeader.RecStatusCode = recordToUpdate.RecordStatusCode;
                newFormularyHeader.RecStatuschangeMsg = recordToUpdate.RecordStatusCodeChangeMsg;
                newFormularyHeader.IsLatest = true;
                newFormularyHeader.RecStatuschangeDate = DateTime.UtcNow;

                //if is 'dmd deleted', then new record will be saved as 'Deleted'.
                if (newFormularyHeader.IsDmdDeleted == true && newFormularyHeader.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)
                    newFormularyHeader.RecStatusCode = TerminologyConstants.RECORDSTATUS_DELETED;

                //newFormularyHeader.FormularyVersionId = rootEntityIdentifier;
                //newFormularyHeader.VersionId = existingRecord.VersionId + 1;

                if (removeDuplicateFlag)
                {
                    newFormularyHeader.IsDuplicate = false;
                    newFormularyHeader.DuplicateOfFormularyId = null;
                }

                formularyRepo.Add(newFormularyHeader);

                //Update existing
                existingEditableRecord.IsLatest = false;
                formularyRepo.Update(existingEditableRecord);

                //MMC-477: If recstscd of new updatable rec is changed and is set '002' or '003' - move the record already in 002 or 003 to archive i.e '004'
                if (existingRecordinReadOnly.RecStatusCode != recordToUpdate.RecordStatusCode && (recordToUpdate.RecordStatusCode == TerminologyConstants.RECORDSTATUS_APPROVED || recordToUpdate.RecordStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE))
                {
                    var existingTargetStatusRecords = existingTargetStatusRecordsForCodes.IsCollectionValid() && existingTargetStatusRecordsForCodes.Contains(newFormularyHeader.Code) ? existingTargetStatusRecordsForCodes[newFormularyHeader.Code].ToList() : formularyRepo.GetLatestFormulariesByCodes(new string[] { newFormularyHeader.Code }, true)
                    .Where(rec => rec.Code == newFormularyHeader.Code && rec.IsLatest == true)
                    .ToList();

                    var existingTargetRecs = existingTargetStatusRecords?.Where(rec => recordToUpdate.RecordStatusCode == rec.RecStatusCode)?.ToList();
                    if (existingTargetRecs.IsCollectionValid())
                        UpdateExistingRecordWithTargetStatusToArchiveIfExists(formularyRepo, newFormularyHeader.Code, new string[] { recordToUpdate.RecordStatusCode }, existingTargetRecs);
                }

                if (existingRecordinReadOnly.RecStatusCode != recordToUpdate.RecordStatusCode && (recordToUpdate.RecordStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT && existingRecordinReadOnly.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED))
                {
                    var existingTargetStatusRecords = existingTargetStatusRecordsForCodes.IsCollectionValid() && existingTargetStatusRecordsForCodes.Contains(newFormularyHeader.Code) ? existingTargetStatusRecordsForCodes[newFormularyHeader.Code].ToList() : formularyRepo.GetLatestFormulariesByCodes(new string[] { newFormularyHeader.Code }, true)
                    .Where(rec => rec.Code == newFormularyHeader.Code && rec.IsLatest == true)
                    .ToList();

                    var existingTargetRecs = existingTargetStatusRecords?.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?.ToList();

                    existingTargetRecs = existingTargetRecs?.Where(rec => rec.FormularyId != newFormularyHeader.FormularyId).ToList();

                    if (existingTargetRecs.IsCollectionValid())
                        UpdateExistingRecordsWithDraftStatusToArchiveIfExists(formularyRepo, newFormularyHeader.Code, newFormularyHeader.FormularyId, existingTargetRecs);
                }

                #region ref-only
                //mmc-477 - this case incorrect added and removed -- for ref only
                //if the previous code is different, set the islatest of that also to false - not required now
                //if (existingRecFromDbWithSameStatus != null && (existingRecFromDbWithSameStatus.Code == existingRecord.Code && existingRecFromDbWithSameStatus.RecStatusCode != existingRecord.RecStatusCode))
                //{
                //    existingRecFromDbWithSameStatus.IsLatest = false;
                //    formularyRepo.Update(existingRecFromDbWithSameStatus);
                //}

                //updatableFormulary.RecStatusCode = recordToUpdate.RecordStatusCode;
                //updatableFormulary.RecStatuschangeMsg = recordToUpdate.RecordStatusCodeChangeMsg;

                //updatableFormulary.RecStatuschangeDate = DateTime.UtcNow;

                //updatableFormulary.FormularyVersionId = Guid.NewGuid().ToString();
                //updatableFormulary.VersionId = updatableFormulary.VersionId+1;

                //var detail = updatableFormulary.FormularyDetail.First();
                //detail.FormularyVersionId = updatableFormulary.FormularyVersionId;
                //detail.RowId = null;

                //if (updatableFormulary.FormularyAdditionalCode.IsCollectionValid())
                //{
                //    updatableFormulary.FormularyAdditionalCode.Each(ac =>
                //    {
                //        ac.RowId = null;
                //        ac.FormularyVersionId = updatableFormulary.FormularyVersionId;
                //    });
                //}

                //if (updatableFormulary.FormularyIndication.IsCollectionValid())
                //{
                //    updatableFormulary.FormularyIndication.Each(ac =>
                //    {
                //        ac.RowId = null;
                //        ac.FormularyVersionId = updatableFormulary.FormularyVersionId;
                //    });
                //}

                //if (updatableFormulary.FormularyIngredient.IsCollectionValid())
                //{
                //    updatableFormulary.FormularyIngredient.Each(ac =>
                //    {
                //        ac.RowId = null;
                //        ac.FormularyVersionId = updatableFormulary.FormularyVersionId;
                //    });
                //}

                //if (updatableFormulary.FormularyRouteDetail.IsCollectionValid())
                //{
                //    updatableFormulary.FormularyRouteDetail.Each(ac =>
                //    {
                //        ac.RowId = null;
                //        ac.FormularyVersionId = updatableFormulary.FormularyVersionId;
                //    });
                //}

                //formularyRepo.Add(updatableFormulary);
                #endregion
            }

            return true;
        }

        #region old code - ref only
        //private bool UpdateStatusForRecord(FormularyHeader updatableFormulary, List<FormularyHeader> existingFormulariesByCodes, UpdateFormularyRecordStatusRequestData recordToUpdate, Dictionary<string, int?> codeVersionIdLkp, IFormularyRepository<FormularyHeader> formularyRepo,  bool removeDuplicateFlag = false)
        //{
        //    //Update the existing record with IsLatest = false and 
        //    //Create new record with new status

        //    if (updatableFormulary != null)
        //    {
        //        //Get Updatable record
        //        var existingRecord = formularyRepo.GetFormularyListForIds(new List<string> { updatableFormulary.FormularyVersionId }).FirstOrDefault();

        //        if (existingRecord == null) return false;

        //        //var existingRecFromDbWithSameStatus = existingFormulariesByCodes.FirstOrDefault(rec => rec.Code == updatableFormulary.Code && rec.RecStatusCode == recordToUpdate.RecordStatusCode);

        //        //var rootEntityIdentifier = Guid.NewGuid().ToString();

        //        //var newFormularyHeader = existingRecord.CloneFormulary(_mapper, rootEntityIdentifier);
        //        var newFormularyHeader = existingRecord.CloneFormularyV2(_mapper);

        //        newFormularyHeader.RecStatusCode = recordToUpdate.RecordStatusCode;
        //        newFormularyHeader.RecStatuschangeMsg = recordToUpdate.RecordStatusCodeChangeMsg;
        //        newFormularyHeader.IsLatest = true;
        //        newFormularyHeader.RecStatuschangeDate = DateTime.UtcNow;

        //        //newFormularyHeader.FormularyVersionId = rootEntityIdentifier;
        //        //newFormularyHeader.VersionId = existingRecord.VersionId + 1;

        //        if (removeDuplicateFlag)
        //        {
        //            newFormularyHeader.IsDuplicate = false;
        //            newFormularyHeader.DuplicateOfFormularyId = null;
        //        }

        //        formularyRepo.Add(newFormularyHeader);

        //        //Update existing
        //        existingRecord.IsLatest = false;
        //        formularyRepo.Update(existingRecord);

        //        var existingTargetStatusRecords = formularyRepo.GetLatestFormulariesByCodes(new string[] { newFormularyHeader.Code }, true)
        //            .Where(rec => rec.Code == newFormularyHeader.Code && rec.IsLatest == true)
        //            .ToList();

        //        //MMC-477: If recstscd of new updatable rec is changed and is set '002' or '003' - move the record already in 002 or 003 to archive i.e '004'
        //        if (existingRecord.RecStatusCode != recordToUpdate.RecordStatusCode && (recordToUpdate.RecordStatusCode == TerminologyConstants.RECORDSTATUS_APPROVED || recordToUpdate.RecordStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE))
        //        {
        //            var existingTargetRecs = existingTargetStatusRecords.Where(rec => recordToUpdate.RecordStatusCode == rec.RecStatusCode)?.ToList();
        //            if (existingTargetRecs.IsCollectionValid())
        //                UpdateExistingRecordWithTargetStatusToArchiveIfExists(formularyRepo, newFormularyHeader.Code, new string[] { recordToUpdate.RecordStatusCode }, existingTargetRecs);
        //        }

        //        if (existingRecord.RecStatusCode != recordToUpdate.RecordStatusCode && (recordToUpdate.RecordStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT && existingRecord.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //        {
        //            var existingTargetRecs = existingTargetStatusRecords.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?.ToList();

        //            existingTargetRecs = existingTargetRecs?.Where(rec=> rec.FormularyId != newFormularyHeader.FormularyId).ToList();

        //            if (existingTargetRecs.IsCollectionValid())
        //                UpdateExistingRecordsWithDraftStatusToArchiveIfExists(formularyRepo, newFormularyHeader.Code, newFormularyHeader.FormularyId, existingTargetRecs);
        //        }



        //        //mmc-477 - this case incorrect added and removed -- for ref only
        //        //if the previous code is different, set the islatest of that also to false - not required now
        //        //if (existingRecFromDbWithSameStatus != null && (existingRecFromDbWithSameStatus.Code == existingRecord.Code && existingRecFromDbWithSameStatus.RecStatusCode != existingRecord.RecStatusCode))
        //        //{
        //        //    existingRecFromDbWithSameStatus.IsLatest = false;
        //        //    formularyRepo.Update(existingRecFromDbWithSameStatus);
        //        //}

        //        //updatableFormulary.RecStatusCode = recordToUpdate.RecordStatusCode;
        //        //updatableFormulary.RecStatuschangeMsg = recordToUpdate.RecordStatusCodeChangeMsg;

        //        //updatableFormulary.RecStatuschangeDate = DateTime.UtcNow;

        //        //updatableFormulary.FormularyVersionId = Guid.NewGuid().ToString();
        //        //updatableFormulary.VersionId = updatableFormulary.VersionId+1;

        //        //var detail = updatableFormulary.FormularyDetail.First();
        //        //detail.FormularyVersionId = updatableFormulary.FormularyVersionId;
        //        //detail.RowId = null;

        //        //if (updatableFormulary.FormularyAdditionalCode.IsCollectionValid())
        //        //{
        //        //    updatableFormulary.FormularyAdditionalCode.Each(ac =>
        //        //    {
        //        //        ac.RowId = null;
        //        //        ac.FormularyVersionId = updatableFormulary.FormularyVersionId;
        //        //    });
        //        //}

        //        //if (updatableFormulary.FormularyIndication.IsCollectionValid())
        //        //{
        //        //    updatableFormulary.FormularyIndication.Each(ac =>
        //        //    {
        //        //        ac.RowId = null;
        //        //        ac.FormularyVersionId = updatableFormulary.FormularyVersionId;
        //        //    });
        //        //}

        //        //if (updatableFormulary.FormularyIngredient.IsCollectionValid())
        //        //{
        //        //    updatableFormulary.FormularyIngredient.Each(ac =>
        //        //    {
        //        //        ac.RowId = null;
        //        //        ac.FormularyVersionId = updatableFormulary.FormularyVersionId;
        //        //    });
        //        //}

        //        //if (updatableFormulary.FormularyRouteDetail.IsCollectionValid())
        //        //{
        //        //    updatableFormulary.FormularyRouteDetail.Each(ac =>
        //        //    {
        //        //        ac.RowId = null;
        //        //        ac.FormularyVersionId = updatableFormulary.FormularyVersionId;
        //        //    });
        //        //}

        //        //formularyRepo.Add(updatableFormulary);
        //    }

        //    return true;
        //}
        #endregion

        /// <summary>
        /// If recsts cd is changed from Archive to Draft, then Archive the other 'Draft' records of the same code
        /// </summary>
        /// <param name="formularyRepo"></param>
        /// <param name="code"></param>
        /// <param name="strings"></param>
        /// <param name="existingTargetRecs"></param>
        private void UpdateExistingRecordsWithDraftStatusToArchiveIfExists(IFormularyRepository<FormularyHeader> formularyRepo, string code, string formularyIdToIgnore = null, List<FormularyHeader> existingTargetRecs = null)
        {
            if (code.IsEmpty()) return;

            if (!existingTargetRecs.IsCollectionValid())
            {
                existingTargetRecs = formularyRepo.GetLatestFormulariesByCodes(new string[] { code }, true)
                    .Where(rec => rec.Code == code && rec.IsLatest == true && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)
                    .ToList();
            }

            if (formularyIdToIgnore.IsNotEmpty())
                existingTargetRecs = existingTargetRecs?.Where(rec => rec.FormularyId != formularyIdToIgnore).ToList();

            if (!existingTargetRecs.IsCollectionValid()) return;

            foreach (var existingRecord in existingTargetRecs)
            {
                var newCloned = existingRecord.CloneFormularyV2(_mapper);

                newCloned.VersionId = existingRecord.VersionId + 1;
                newCloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
                formularyRepo.Add(newCloned);

                existingRecord.IsLatest = false;
                formularyRepo.Update(existingRecord);
            }
        }

        /// <summary>
        /// If recstscd of new updatable rec is '002' or '003' - move to archive i.e '004'
        /// </summary>
        /// <param name="formularyRepo"></param>
        /// <param name="code"></param>
        /// <param name="targetStatusToLookFor"></param>
        private void UpdateExistingRecordWithTargetStatusToArchiveIfExists(IFormularyRepository<FormularyHeader> formularyRepo, string code, string[] targetStatusToLookFor, List<FormularyHeader> existingTargetRecords = null)
        {
            if (code.IsEmpty() || !targetStatusToLookFor.IsCollectionValid()) return;

            var existingTargetStatusRecords = existingTargetRecords;
            if (!existingTargetStatusRecords.IsCollectionValid())
            {
                existingTargetStatusRecords = formularyRepo.GetLatestFormulariesByCodes(new string[] { code }, true)
                    .Where(rec => rec.Code == code && rec.IsLatest == true && targetStatusToLookFor.Contains(rec.RecStatusCode))
                    .ToList();
            }

            if (!existingTargetStatusRecords.IsCollectionValid()) return;

            foreach (var existingRecord in existingTargetStatusRecords)
            {
                var newCloned = existingRecord.CloneFormularyV2(_mapper);

                newCloned.VersionId = existingRecord.VersionId + 1;
                newCloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
                formularyRepo.Add(newCloned);

                existingRecord.IsLatest = false;
                formularyRepo.Update(existingRecord);
            }
        }

        //MMC-477-Fix01- Not in use
        //private bool DeleteRecordWithSimilarCode(IEnumerable<FormularyHeader> duplicateFormulariesWithSameCodeFromDb, IFormularyRepository<FormularyHeader> formularyRepo)
        //{
        //    //Update the old records with IsLatest = false
        //    //Add new record with Delete status

        //    //var uniqueFormularyVersions = duplicateFormulariesWithSameCodeFromDb.Select(dup => dup.FormularyVersionId).ToList();

        //    //var updatableFormularies = formularyRepo.GetFormularyListForIds(uniqueFormularyVersions).ToList();

        //    //if (updatableFormularies.IsCollectionValid())
        //    //{
        //    //    updatableFormularies.Each(recToUpdate =>
        //    //    {
        //    //        recToUpdate.IsLatest = false;
        //    //        formularyRepo.Update(recToUpdate);
        //    //    });
        //    //}

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

        //            //var detail = dupAsNew.FormularyDetail.First();

        //            //detail.FormularyVersionId = dupAsNew.FormularyVersionId;

        //            //if (dupAsNew.FormularyAdditionalCode.IsCollectionValid())
        //            //{
        //            //    dupAsNew.FormularyAdditionalCode.Each(ac =>
        //            //    {
        //            //        ac.FormularyVersionId = dupAsNew.FormularyVersionId;
        //            //    });
        //            //}

        //            //if (dupAsNew.FormularyIndication.IsCollectionValid())
        //            //{
        //            //    dupAsNew.FormularyIndication.Each(ac =>
        //            //    {
        //            //        ac.FormularyVersionId = dupAsNew.FormularyVersionId;
        //            //    });
        //            //}

        //            //if (dupAsNew.FormularyIngredient.IsCollectionValid())
        //            //{
        //            //    dupAsNew.FormularyIngredient.Each(ac =>
        //            //    {
        //            //        ac.FormularyVersionId = dupAsNew.FormularyVersionId;
        //            //    });
        //            //}

        //            //if (dupAsNew.FormularyRouteDetail.IsCollectionValid())
        //            //{
        //            //    dupAsNew.FormularyRouteDetail.Each(ac =>
        //            //    {
        //            //        ac.FormularyVersionId = dupAsNew.FormularyVersionId;
        //            //    });
        //            //}

        //            //formularyRepo.Add(dupAsNew);
        //        }
        //    });

        //    return true;
        //}

        //public async Task<UpdateFormularyRecordStatusDTO> UpdateVMPFormularyRecordStatus(UpdateFormularyRecordStatusRequest request)
        //{
        //    var response = new UpdateFormularyRecordStatusDTO
        //    {
        //        Status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS, StatusMessage = "", ErrorMessages = new List<string>() },
        //        Data = new List<FormularyDTO>()
        //    };

        //    if (request == null || !request.RequestData.IsCollectionValid())
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

        //        return response;
        //    }

        //    var recordsToUpdate = request.RequestData;

        //    if (!recordsToUpdate.IsCollectionValid())
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

        //        return response;
        //    }

        //    var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

        //    var uniqueFormularyVersionIds = recordsToUpdate.Select(r => r.FormularyVersionId).Distinct().ToList();

        //    var ampFormularies = formularyRepo.GetFormularyListForIds(uniqueFormularyVersionIds, true).ToList();

        //    List<string> lstParentCodes = new List<string>();

        //    ampFormularies.Each(rec => {
        //        lstParentCodes.Add(rec.ParentCode);
        //    });

        //    string[] parentCodes = lstParentCodes.ToArray();

        //    var vmpFormularies = formularyRepo.GetLatestFormulariesByCodes(parentCodes).ToList();

        //    if (!vmpFormularies.IsCollectionValid())
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

        //        return response;
        //    }

        //    var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

        //    var nodes = await repo.GetFormularyDescendentForCodes(parentCodes);

        //    string recordStatus = "";

        //    if (nodes.IsCollectionValid())
        //    {
        //        var childAMPNodes = nodes.Where(rec => string.Compare(rec.ProductType, "AMP", true) == 0);

        //        if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_ACTIVE;
        //        }
        //        else if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_APPROVED) && (!(childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE))
        //            || !(childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED)) || !(childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DELETED))))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_APPROVED;
        //        }
        //        else if (childAMPNodes.All(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_DRAFT;
        //        }
        //        else if (childAMPNodes.All(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_ARCHIVED;
        //        }
        //        else if (childAMPNodes.All(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DELETED))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_DELETED;
        //        }
        //        else if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_DRAFT;
        //        }
        //        else if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_ARCHIVED;
        //        }
        //        else if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DELETED))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_DELETED;
        //        }
        //    }

        //    List<UpdateFormularyRecordStatusRequestData> recsToUpdate = new List<UpdateFormularyRecordStatusRequestData>();

        //    vmpFormularies.Each(rec => {
        //        UpdateFormularyRecordStatusRequestData req = new UpdateFormularyRecordStatusRequestData();
        //        req.FormularyVersionId = rec.FormularyVersionId;
        //        req.RecordStatusCode = recordStatus;
        //        req.RecordStatusCodeChangeMsg = rec.RecStatuschangeMsg;
        //        recsToUpdate.Add(req);
        //    });

        //    var hasValidRecs = ValidateRecords(vmpFormularies, recsToUpdate, response);

        //    if (!hasValidRecs)
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        return response;
        //    }

        //    foreach (var rec in vmpFormularies)
        //    {
        //        var recordToUpdate = recsToUpdate.SingleOrDefault(rec => rec.FormularyVersionId == rec.FormularyVersionId);
        //        UpdateRecordStatus(recordToUpdate, response, vmpFormularies, formularyRepo);
        //    }

        //    return response;
        //}

        //public async Task<UpdateFormularyRecordStatusDTO> UpdateVTMFormularyRecordStatus(UpdateFormularyRecordStatusRequest request)
        //{
        //    var response = new UpdateFormularyRecordStatusDTO
        //    {
        //        Status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS, StatusMessage = "", ErrorMessages = new List<string>() },
        //        Data = new List<FormularyDTO>()
        //    };

        //    if (request == null || !request.RequestData.IsCollectionValid())
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

        //        return response;
        //    }

        //    var recordsToUpdate = request.RequestData;

        //    if (!recordsToUpdate.IsCollectionValid())
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

        //        return response;
        //    }

        //    var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

        //    var uniqueFormularyVersionIds = recordsToUpdate.Select(r => r.FormularyVersionId).Distinct().ToList();

        //    var ampFormularies = formularyRepo.GetFormularyListForIds(uniqueFormularyVersionIds, true).ToList();

        //    List<string> lstParentCodes = new List<string>();

        //    ampFormularies.Each(rec => {
        //        lstParentCodes.Add(rec.ParentCode);
        //    });

        //    string[] parentCodes = lstParentCodes.ToArray();

        //    var vmpFormularies = formularyRepo.GetLatestFormulariesByCodes(parentCodes).ToList();

        //    if (!vmpFormularies.IsCollectionValid())
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

        //        return response;
        //    }

        //    List<string> lstGrandParentCodes = new List<string>();

        //    vmpFormularies.Each(rec => {
        //        lstGrandParentCodes.Add(rec.ParentCode);
        //    });

        //    string[] grandParentCodes = lstGrandParentCodes.ToArray();

        //    var vtmFormularies = formularyRepo.GetLatestFormulariesByCodes(grandParentCodes).ToList();

        //    if (!vtmFormularies.IsCollectionValid())
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        response.Status.ErrorMessages.Add(INVALID_INPUT_MSG);

        //        return response;
        //    }

        //    var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

        //    var nodes = await repo.GetFormularyDescendentForCodes(grandParentCodes);

        //    string recordStatus = "";

        //    if (nodes.IsCollectionValid())
        //    {
        //        var childAMPNodes = nodes.Where(rec => string.Compare(rec.ProductType, "VMP", true) == 0);

        //        if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_ACTIVE;
        //        }
        //        else if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_APPROVED) && (!(childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE))
        //            || !(childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED)) || !(childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DELETED))))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_APPROVED;
        //        }
        //        else if (childAMPNodes.All(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_DRAFT;
        //        }
        //        else if (childAMPNodes.All(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_ARCHIVED;
        //        }
        //        else if (childAMPNodes.All(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DELETED))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_DELETED;
        //        }
        //        else if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_DRAFT;
        //        }
        //        else if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_ARCHIVED;
        //        }
        //        else if (childAMPNodes.Any(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DELETED))
        //        {
        //            recordStatus = TerminologyConstants.RECORDSTATUS_DELETED;
        //        }
        //    }

        //    List<UpdateFormularyRecordStatusRequestData> recsToUpdate = new List<UpdateFormularyRecordStatusRequestData>();

        //    vtmFormularies.Each(rec => {
        //        UpdateFormularyRecordStatusRequestData req = new UpdateFormularyRecordStatusRequestData();
        //        req.FormularyVersionId = rec.FormularyVersionId;
        //        req.RecordStatusCode = recordStatus;
        //        req.RecordStatusCodeChangeMsg = rec.RecStatuschangeMsg;
        //        recsToUpdate.Add(req);
        //    });

        //    var hasValidRecs = ValidateRecords(vtmFormularies, recsToUpdate, response);

        //    if (!hasValidRecs)
        //    {
        //        response.Status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;
        //        return response;
        //    }

        //    foreach (var rec in vtmFormularies)
        //    {
        //        var recordToUpdate = recsToUpdate.SingleOrDefault(rec => rec.FormularyVersionId == rec.FormularyVersionId);
        //        UpdateRecordStatus(recordToUpdate, response, vtmFormularies, formularyRepo);
        //    }

        //    return response;
        //}
    }
}
