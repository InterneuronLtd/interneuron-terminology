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
﻿//using Elastic.Apm.Api;
//using Interneuron.Common.Extensions;
//using Interneuron.Terminology.API.AppCode.DTOs;
//using Interneuron.Terminology.API.AppCode.DTOs.Formulary;
//using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
//using Interneuron.Terminology.API.AppCode.Extensions;
//using Interneuron.Terminology.Infrastructure.Domain;
//using Interneuron.Terminology.Model.DomainModels;
//using MongoDB.Bson.Serialization.Serializers;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Interneuron.Terminology.API.AppCode.Queries
//{
//    public partial class FormularyQueries : IFormularyQueries
//    {
//        public async Task<ValidateAMPStatusChangeDTO> ValidateAMPStatusChange(ValidateFormularyStatusChangeRequest request)
//        {
//            var response = new ValidateAMPStatusChangeDTO { Status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS, ErrorMessages = new List<string>() } };
//            //Check if any other tree has 'Active' AMPs already
//            //get all amps
//            //find the root of each amp
//            //get the 'code' of the root
//            //check if there are other latest root for the same code
//            //get all actives of the other root of the same code 
//            //Step 1: from the incoming for each root - map all amps

//            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

//            var requestDTOs = request.RequestsData;

//            var uniqueIds = requestDTOs.Select(req => req.FormularyVersionId).Distinct().ToList();

//            //Considering non-deleted records only, for this comparision
//            var existingFormulariesFromDB = formularyRepo.GetFormularyListForIds(uniqueIds, true).ToList();

//            var ampsInDb = existingFormulariesFromDB?.Where(rec => rec.IsLatest == true && rec.ProductType == "AMP").ToList();

//            if (!ampsInDb.IsCollectionValid())
//            {
//                response.Data = true;
//                return response;
//            }

//            var ampsInRequest = ampsInDb.Select(req => new { req.Code, req.Name, RecStatusCode = requestDTOs[0].RecordStatusCode, req.FormularyId, req.FormularyVersionId, req.ParentCode, req.ParentName, req.ParentFormularyId }).Distinct().ToList();


//            //all AMPs will be of the same status
//            var recStatusInRequest = requestDTOs[0].RecordStatusCode;
//            var recStatusInDb = ampsInDb[0].RecStatusCode;

//            if (recStatusInDb == recStatusInRequest)
//            {
//                response.Data = true;
//                return response;//no change in status
//            }

//            //If new record has been changed to 'Active'
//            //check if all the AMPs under this root has been changed to 'Active'
//            //All the 'AMPs' in the existing 'Active' root should match
//            if (recStatusInRequest != TerminologyConstants.RECORDSTATUS_ACTIVE)
//            {
//                response.Data = true;
//                return response;
//            }

//            if (!CheckIfSameAMPCodeInDifferentTreeSelected(ampsInRequest, response))
//                return response;

//            if (!CheckIfSameParentCodeButInDifferentParentFormularyidsIsSelected(ampsInRequest, response))
//                return response;//no change in status

//            if (!await CheckIfAllAMPsUnderAParentIsSetActive(ampsInRequest, response))
//                return response;

//            response.Data = true;
//            return response;
//        }

//        private bool CheckIfSameAMPCodeInDifferentTreeSelected(IEnumerable<dynamic> ampsInRequest, ValidateAMPStatusChangeDTO response)
//        {
//            /*
//             vtm01 -v1
//                -vmp01
//                --amp01 -active (not allowed)
//                --amp02 -draft 
//                -vmp01
//                --amp01 -active (not allowed)
//                --amp02 -draft

//                vtm01 -v2
//                -vmp01
//                --amp01 -active
//                --amp02 -active
//             */

//            //check if same amp belongs to different tree. (should not allow same amp code in different tree to set to active together
//            var ampInRequestCode = new HashSet<string>();
//            var isValid = true;

//            foreach (var ampInRequest in ampsInRequest)
//            {
//                if (ampInRequestCode.Contains(ampInRequest.Code))
//                {
//                    response.Status.ErrorMessages.Add($"{ampInRequest.Code}-{ampInRequest.Name}. Cannot set multiple AMPs of different versions but with same DM+D code as 'active'");
//                    response.Data = false;//cannot set multiple amps (with different FId but same code as 'active')
//                    isValid = false;//no change in status
//                }
//                ampInRequestCode.Add(ampInRequest.Code);
//            }

//            return isValid;
//        }

//        //need refactoring
//        private async Task<bool> CheckIfAllAMPsUnderAParentIsSetActive(IEnumerable<dynamic> ampsInRequest, ValidateAMPStatusChangeDTO response)
//        {
//            #region summary
//            /*
//             vtm01 -v1
//                -vmp01
//                --amp01 -draft
//                --amp02 -draft
//                -vmp01
//                --amp01 -active (not allowed)
//                --amp02 -draft

//                vtm01 -v2
//                -vmp01
//                --amp01 -active
//                --amp02 -active
//             */

//            //Problem: Check whether input AMPs is replacing all current 'Active' AMPs in the 'Active' tree (Current Active tree of the same root 'code' should be different that the 'input' code tree.
//            //got to the root of these input formularyids that are to be made active
//            //and get the active tree of that root if exists
//            //get all active amps in that active root and check whether the all those exists in the current input
//            #endregion

//            //(icode, ifid, irootfid, iactiverootfid irootcodeActiveAMPs)

//            List<(string InputAMPCode, string InputAMPFormularyId, string RootFIdOfInputAMP, string ActiveRootFIdOfInputAMP, List<string> ExistingActiveAMPsForRootCode)> x = new();

//            var inputFormularyIds = new List<string>();
//            var inputCodes = new List<string>();

//            ampsInRequest.Each(rec =>
//            {
//                inputCodes.Add(rec.Code);
//                inputFormularyIds.Add(rec.FormularyId);
//            });

//            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

//            var formularyIdWithRootFormularyIdLkp = GetRootFormularyIdForInputAMPFormularyId(formularyRepo, inputFormularyIds);

//            //if roots are only amps no need to test
//            if (!formularyIdWithRootFormularyIdLkp.IsCollectionValid())
//            {
//                response.Data = true;
//                return true;
//            }

//            //to get active root id
//            //rootfid-rootcode
//            //rootcode-activerootfid

//            var rootFIds = formularyIdWithRootFormularyIdLkp.Values.ToList();

//            var rootFormularyIdAndRootCodeLkp = formularyRepo.ItemsAsReadOnly.Where(rec=> rootFIds.Contains(rec.FormularyId))
//                ?.Select(rec => new { rec.Code, rec.FormularyId })
//                .Distinct(rec=> rec.FormularyId)
//                .ToDictionary(k=> k.FormularyId, v=> v.Code);

//            var rootCodes = rootFormularyIdAndRootCodeLkp.Values.ToList();

//            var rootCodeWithActiveFIdMapping = await rootCodes.GetActiveFormularyIdForCode(_provider);
//            if(!rootCodeWithActiveFIdMapping.IsCollectionValid())
//            {
//                response.Data = true;
//                return true;
//            }
//            var rootFIdWithActiveRootFidMapping = new Dictionary<string, string>();
//            foreach (var key in rootFormularyIdAndRootCodeLkp.Keys)
//            {
//                var rootCode = rootFormularyIdAndRootCodeLkp[key];
//                if (!rootCodeWithActiveFIdMapping.ContainsKey(rootCode)) continue;
//                rootFIdWithActiveRootFidMapping[key] = rootCodeWithActiveFIdMapping[rootCode];
//            }
//            if(!rootFIdWithActiveRootFidMapping.IsCollectionValid())
//            {
//                response.Data = true;
//                return true;
//            }

//            var activeRootFIds = rootFIdWithActiveRootFidMapping.Values.ToList();
//            var xx = GetActiveAMPsOfActiveRootFormularyIdAsLKP(formularyRepo, activeRootFIds);

//            var msgHeader = "Should set all AMPs of these parent codes as 'Active'.";
//            var headerAdded = true;
//            var isValid = true;

//            foreach (var ampInRequest in ampsInRequest)
//            {
//                string ampInRequestFormularyId = (ampInRequest.FormularyId).ToString();
//                var rootFId = formularyIdWithRootFormularyIdLkp.ContainsKey(ampInRequest.FormularyId) ? (formularyIdWithRootFormularyIdLkp[ampInRequestFormularyId]?.ToString()) : null;

//                if (rootFId == null) continue;

//                var activeRootFId = !rootFIdWithActiveRootFidMapping.ContainsKey(rootFId) ? null : rootFIdWithActiveRootFidMapping[rootFId];
//                //if no active root or if active root and input code's root are same, then no need to validate
//                if (!(activeRootFId != null && activeRootFId != rootFId)) continue;

//                var activeAMPsForActiveRoot = xx.ContainsKey(activeRootFId) ? xx[activeRootFId] : null;

//                if (!activeAMPsForActiveRoot.IsCollectionValid()) continue;

//                var matchedOutput = inputCodes.Where(rec => activeAMPsForActiveRoot.Contains(rec))?.ToList();

//                isValid = !matchedOutput.IsCollectionValid() || (matchedOutput.IsCollectionValid() && matchedOutput.Count == activeAMPsForActiveRoot.Count);

//                if (isValid) continue;

//                if (headerAdded)
//                {
//                    headerAdded = false;
//                    response.Status.ErrorMessages.Add(msgHeader);
//                }
//                response.Status.ErrorMessages.Add(rootFormularyIdAndRootCodeLkp[rootFId]);

//                //x.Add((ampInRequest.Code, ampInRequest.FormularyId, rootFId, activeRootFId, null));
//            }


//            //==================================
//            //inputcode - rootcode
//            var inputCodeWithRootCodeAsLkp = new Dictionary<string, string>();
//            //rootcode - rootfid
//            Dictionary<string, string> rootCodesForFormularyIdsInInputAsLkp;
//            //rootcode-activerootfid
//            Dictionary<string, string> rootCodeWithActiveFormularyIdsLkp;
//            //activerootfid - active amps
//            var activeRootfidWithActiveAMPsOnlyLkp = new Dictionary<string, List<string>>();

//            var inputFormularyIds = new List<string>();
//            var inputCodes = new List<string>();

//            ampsInRequest.Each(rec =>
//            {
//                inputCodes.Add(rec.Code);
//                inputFormularyIds.Add(rec.FormularyId);
//            });

//            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

//            var formularyIdWithRootFormularyIdLkp = GetRootFormularyIdForInputAMPFormularyId(formularyRepo, inputFormularyIds); 
//            //if roots are only amps no need to test
//            if (!formularyIdWithRootFormularyIdLkp.IsCollectionValid()) 
//            {
//                response.Data = true;
//                return true;
//            }

//            (inputCodeWithRootCodeAsLkp, rootCodesForFormularyIdsInInputAsLkp) = GetRootCodeForInputCodeAndRootCodeForInputFormularyId(formularyRepo, formularyIdWithRootFormularyIdLkp, ampsInRequest);

//            var rootCodesForFormularyIdsInInput = rootCodesForFormularyIdsInInputAsLkp.Keys.ToList();

//            rootCodeWithActiveFormularyIdsLkp =  await rootCodesForFormularyIdsInInput.GetActiveFormularyIdForCode(_provider);

//            if (!rootCodeWithActiveFormularyIdsLkp.IsCollectionValid())
//            {
//                response.Data = true;
//                return true;
//            }

//            var activeRootFIds = rootCodeWithActiveFormularyIdsLkp.Values.ToList();
//            activeRootfidWithActiveAMPsOnlyLkp = GetActiveAMPsOfActiveRootFormularyIdAsLKP(formularyRepo, activeRootFIds);

//            if (!activeRootfidWithActiveAMPsOnlyLkp.IsCollectionValid())
//            {
//                response.Data = true;
//                return true;
//            }

//            //filter those for which the input formularyid and active formularyid are same. (no need to validate for those codes)
//            var validatableCodes = new List<string>();

//            foreach (var inputCode in inputCodes)
//            {
//                var rootCodeOfValidatableCode = inputCodeWithRootCodeAsLkp[inputCode];
//                if (rootCodeOfValidatableCode == null) continue;
//                //if current selected input tree is different from active tree
//                if (rootCodeWithActiveFormularyIdsLkp.ContainsKey(rootCodeOfValidatableCode) && rootCodesForFormularyIdsInInputAsLkp[rootCodeOfValidatableCode] != rootCodeWithActiveFormularyIdsLkp[rootCodeOfValidatableCode])
//                {
//                    validatableCodes.Add(inputCode);
//                }
//            }

//            var msgHeader = "Should set all AMPs of these parent codes as 'Active'.";
//            var headerAdded = true;
//            var isValid = true;

//            //for each of the input amp code (only validatable codes), capture the list of active amps that should be there in the list
//            foreach (var validatableCode in validatableCodes)
//            {
//                if (!inputCodeWithRootCodeAsLkp.IsCollectionValid() || !inputCodeWithRootCodeAsLkp.ContainsKey(validatableCode)) continue;

//                var rootCodeOfValidatableCode = inputCodeWithRootCodeAsLkp[validatableCode];
                
//                if(!rootCodesForFormularyIdsInInputAsLkp.IsCollectionValid() || !rootCodesForFormularyIdsInInputAsLkp.ContainsKey(rootCodeOfValidatableCode)) continue;

//                //var rootFId = rootCodesForFormularyIdsInInputAsLkp[rootCodeOfValidatableCode];

//                if (!rootCodeWithActiveFormularyIdsLkp.IsCollectionValid() || !rootCodeWithActiveFormularyIdsLkp.ContainsKey(rootCodeOfValidatableCode)) continue;

//                var activeRootFID = rootCodeWithActiveFormularyIdsLkp[rootCodeOfValidatableCode];

//                if(!activeRootfidWithActiveAMPsOnlyLkp.IsCollectionValid() || !activeRootfidWithActiveAMPsOnlyLkp.ContainsKey(activeRootFID)) continue;

//                var activeAMPsFromRoot = activeRootfidWithActiveAMPsOnlyLkp[activeRootFID];

//                if(!activeAMPsFromRoot.IsCollectionValid()) continue;

//                var matchedOutput = inputCodes.Where(rec=> activeAMPsFromRoot.Contains(rec))?.ToList();

//                isValid = !matchedOutput.IsCollectionValid() || (matchedOutput.IsCollectionValid() && matchedOutput.Count == activeAMPsFromRoot.Count);

//                if(isValid) continue;
//                if (headerAdded)
//                {
//                    headerAdded = false;
//                    response.Status.ErrorMessages.Add(msgHeader);
//                }
//                response.Status.ErrorMessages.Add(rootCodeOfValidatableCode);
//            }

//            response.Data = isValid;
//            return isValid;

//            #region old code ref only
//            /*
//            var ampsCodesInRequestInputWithParentFIdAndCodesAsLkp = GetAMPsCodesInRequestInputWithParentFIdAndCodesAsLkp(ampsInRequest);
//            var ampsCodesInActiveModeWithParentFIdAndCodesAsLkp = GetAMPsCodesInActiveModeWithParentFIdAndCodesAsLkp(ampsInRequest);

//            if (!ampsCodesInActiveModeWithParentFIdAndCodesAsLkp.IsCollectionValid())
//            {
//                response.Data = true;
//                return true;
//            }

//            var msgHeader = "Should set all AMPs of these parent codes as 'Active'.";
//            var headerAdded = true;
//            var isValid = true;

//            foreach (var parentCodeOfActiveAMPs in ampsCodesInActiveModeWithParentFIdAndCodesAsLkp.Keys)
//            {
//                var allActiveAMPsForParentCode = ampsCodesInActiveModeWithParentFIdAndCodesAsLkp[parentCodeOfActiveAMPs];

//                if (ampsCodesInRequestInputWithParentFIdAndCodesAsLkp.ContainsKey(parentCodeOfActiveAMPs))
//                {
//                    var allInRequestInputAMPsForParentCode = ampsCodesInRequestInputWithParentFIdAndCodesAsLkp[parentCodeOfActiveAMPs];
//                    var missingAMPsInRequestButPresentInActiveParent = allActiveAMPsForParentCode.Except(allInRequestInputAMPsForParentCode).ToList();

//                    if (missingAMPsInRequestButPresentInActiveParent.IsCollectionValid())
//                    {
//                        if (headerAdded)
//                        {
//                            headerAdded = false;
//                            response.Status.ErrorMessages.Add(msgHeader);
//                        }
//                        var parentDetails = ampsInRequest.Where(rec => parentCodeOfActiveAMPs == rec.ParentCode)
//                            .Select(rec=> $"{rec.ParentCode}")
//                            .ToList();
//                        //not all amps of parent is set to active
//                        parentDetails.Each(rec => response.Status.ErrorMessages.Add(rec));
//                        //response.Data = false;
//                        //return false;//no change in status
//                        isValid = false;
//                    }
//                }
//            }

//            response.Data = isValid;
//            return isValid;
//            */
//            #endregion
//        }

//        private Dictionary<string, List<string>>? GetActiveAMPsOfActiveRootFormularyIdAsLKP(IFormularyRepository<FormularyHeader> formularyRepo, List<string> activeFormularyIds)
//        {
//            var alldescendentsFormularyIdsLkp = formularyRepo.GetDescendentFormularyIdsForFormularyIdsAsFlattenedLookup(formularyIds: activeFormularyIds, true);

//            if (!alldescendentsFormularyIdsLkp.IsCollectionValid()) return null;

//            var descendentFIds = new List<string>();

//            alldescendentsFormularyIdsLkp.Values.Each(rec => descendentFIds.AddRange(rec));

//            var activeAMPs = formularyRepo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && rec.ProductType == "AMP" && descendentFIds.Contains(rec.FormularyId))
//                ?.Select(rec => new { rec.FormularyId, rec.Code })
//                .Distinct(rec => rec.FormularyId)
//                .ToList();

//            if (!activeAMPs.IsCollectionValid()) return null;

//            var activeRootfidWithActiveAMPsOnlyLkp = new Dictionary<string, List<string>>();

//            //filter and capture only active amps
//            foreach (var rootKey in alldescendentsFormularyIdsLkp.Keys)
//            {
//                var rootDescendents = alldescendentsFormularyIdsLkp[rootKey];
//                if (!rootDescendents.IsCollectionValid()) continue;

//                activeRootfidWithActiveAMPsOnlyLkp[rootKey] = activeAMPs.Where(rec => rootDescendents.Contains(rec.FormularyId))
//                    ?.Select(rec => rec.Code)
//                    .ToList();
//            }

//            return activeRootfidWithActiveAMPsOnlyLkp;
//        }

//        private (Dictionary<string, string> inputCodeWithRootCodeAsLkp, Dictionary<string, string> rootCodesForFormularyIdsInInputAsLkp) GetRootCodeForInputCodeAndRootCodeForInputFormularyId(IFormularyRepository<FormularyHeader> formularyRepo, Dictionary<string, string> formularyIdWithRootFormularyIdLkp, IEnumerable<dynamic> ampsInRequest)
//        {
//            var rootFormularyIds = new List<string>();

//            formularyIdWithRootFormularyIdLkp.Values.Each(rec => rootFormularyIds.Add(rec));

//            //for these root formularyids bring their corresponding active
//            var rootCodesForFormularyIdsInInputs = formularyRepo.ItemsAsReadOnly.Where(rec => rootFormularyIds.Contains(rec.FormularyId))?.
//               Select(rec => new { rec.Code, rec.FormularyId })
//               .Distinct(rec => rec.Code).ToList();

//            var rootCodesForFormularyIdsInInputAsLkp = rootCodesForFormularyIdsInInputs.ToDictionary(k => k.Code, v => v.FormularyId);

//            var rootFormularyIdsWithCodesForInInputAsLkp = rootCodesForFormularyIdsInInputs.Distinct(rec => rec.FormularyId).ToDictionary(k => k.FormularyId, v => v.Code);

//            var inputCodeWithRootCodeAsLkp = new Dictionary<string, string>();

//            foreach (var ampInRequest in ampsInRequest)
//            {
//                if (formularyIdWithRootFormularyIdLkp.ContainsKey(ampInRequest.FormularyId))
//                {
//                    var rootFormularyIdForInputCode = formularyIdWithRootFormularyIdLkp[ampInRequest.FormularyId];
//                    if (rootFormularyIdsWithCodesForInInputAsLkp.ContainsKey(rootFormularyIdForInputCode))
//                        inputCodeWithRootCodeAsLkp[ampInRequest.Code] = rootFormularyIdsWithCodesForInInputAsLkp[rootFormularyIdForInputCode];
//                }
//            }

//            return (inputCodeWithRootCodeAsLkp, rootCodesForFormularyIdsInInputAsLkp);
//        }

//        private Dictionary<string, string> GetRootFormularyIdForInputAMPFormularyId(IFormularyRepository<FormularyHeader> formularyRepo, List<string> inputFormularyIds)
//        {
//            return formularyRepo.GetFormularyAncestorRootForFormularyIdsAsLookup(inputFormularyIds);
//        }

//        private Dictionary<string, List<string>> GetAMPsCodesInActiveModeWithParentFIdAndCodesAsLkp(IEnumerable<dynamic> ampsInRequest)
//        {
//            #region e.g.
//            /*
//             vtm01 -v1
//                -vmp01
//                --amp01 -active (this method should return this)
//                --amp02 -active (this method should return this)
//                -vmp01
//                --amp01 -draft (in request to 'active') 
//                --amp02 -draft

//                vtm01 -v2
//                -vmp01
//                --amp01 -active
//                --amp02 -active
//             */
//            #endregion
//            var ampsCodesInActiveModeWithParentFIdAndCodesAsLkp = new Dictionary<string, List<string>>();

//            var ampsParentCodeInRequest = ampsInRequest.Where(rec=> rec.ParentCode != null)?.Select(rec => rec.ParentCode).Distinct().ToList();

//            if (!ampsParentCodeInRequest.IsCollectionValid()) return ampsCodesInActiveModeWithParentFIdAndCodesAsLkp;

//            var formularyRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

//            var ampsCodesInActiveMode = formularyRepo.ItemsAsReadOnly.Where(rec => rec.ParentCode != null && ampsParentCodeInRequest.Contains(rec.ParentCode) && rec.IsLatest == true && rec.RecStatusCode == "003")
//                ?.Select(rec => new { rec.Code, rec.FormularyId, rec.FormularyVersionId, rec.ParentFormularyId, rec.ParentCode })
//                .ToList();

//            if (!ampsCodesInActiveMode.IsCollectionValid()) return ampsCodesInActiveModeWithParentFIdAndCodesAsLkp;

//            //to filter the records that may be of same tree as that of input (ie. same parent formularyid)
//            //should get active 'AMPs' with same parent code but with different 'parent formulary id'
//            var ampsParentCodeWithParentFormularyIdsInRequest = ampsInRequest.Select(rec => new { rec.Code, rec.ParentFormularyId }).ToList();

//            foreach(var rec in ampsCodesInActiveMode)
//            {
//                if (rec.ParentCode == null) continue;
//                //tree of the current incoming code and the active tree should be different. Should consider the active amps of other tree with same code.
//                var inComingParentFormularyIds = ampsParentCodeWithParentFormularyIdsInRequest.Where(inComingAMP=> rec.Code == inComingAMP.Code)?.Select(incomingAMP=> incomingAMP.ParentFormularyId).ToList();

//                //check whether they belong to same tree, then ignore this as active
//                if (inComingParentFormularyIds.IsCollectionValid() && inComingParentFormularyIds.Contains(rec.ParentFormularyId)) continue;

//                if (!ampsCodesInActiveModeWithParentFIdAndCodesAsLkp.ContainsKey(rec.ParentCode))
//                    ampsCodesInActiveModeWithParentFIdAndCodesAsLkp[rec.ParentCode] = new List<string>();

//                ampsCodesInActiveModeWithParentFIdAndCodesAsLkp[rec.ParentCode].Add(rec.Code);
//            }

//            return ampsCodesInActiveModeWithParentFIdAndCodesAsLkp;
//        }

//        private Dictionary<string, List<string>> GetAMPsCodesInRequestInputWithParentFIdAndCodesAsLkp(IEnumerable<dynamic> ampsInRequest)
//        {
//            //get all codes in amps in request
//            var ampsCodeInRequest = ampsInRequest.Select(rec => rec.Code)?.Distinct().ToList();

//            var ampsCodesInRequestInputWithParentFIdAndCodesAsLkp = new Dictionary<string, List<string>>();

//            //check if selection is from same parent code and same parentformularyid
//            foreach(var rec in ampsInRequest)
//            {
//                if (rec.ParentCode == null) continue;

//                if (!ampsCodesInRequestInputWithParentFIdAndCodesAsLkp.ContainsKey(rec.ParentCode))
//                    ampsCodesInRequestInputWithParentFIdAndCodesAsLkp[rec.ParentCode] = new List<string>();
//                ampsCodesInRequestInputWithParentFIdAndCodesAsLkp[rec.ParentCode].Add(rec.Code);
//            }

//            return ampsCodesInRequestInputWithParentFIdAndCodesAsLkp;
//        }

//        private bool CheckIfSameParentCodeButInDifferentParentFormularyidsIsSelected(IEnumerable<dynamic> ampsInRequest, ValidateAMPStatusChangeDTO response)
//        {
//            /*
//             vtm01 -v1
//                -vmp01
//                --amp01 -draft
//                --amp02 -active (not allowed)
//                -vmp01
//                --amp01 -active (not allowed)
//                --amp02 -draft

//                vtm01 -v2
//                -vmp01
//                --amp01 -active
//                --amp02 -active
//             */
//            //get all codes in amps in request
//            var ampsCodeInRequest = ampsInRequest.Select(rec => rec.Code)?.Distinct().ToList();

//            //check if selection is from same parent code and same parentformularyid
//            var ampParentCodeWithParentFormularyId = new Dictionary<string, string>();
//            var msgHeader = "Cannot select different AMPs of same Parent Code of different versions as 'Active'.";
//            var headerAdded = true;

//            var isValid = true;
//            foreach (var rec in ampsInRequest)
//            {
//                if (rec.ParentCode == null) continue;

//                if (ampParentCodeWithParentFormularyId.ContainsKey(rec.ParentCode) && ampParentCodeWithParentFormularyId[rec.ParentCode] != rec.ParentFormularyId)
//                {
//                    if (headerAdded)
//                    {
//                        headerAdded = false;
//                        response.Status.ErrorMessages.Add(msgHeader);
//                    }
//                    response.Status.ErrorMessages.Add($"{rec.ParentCode} - {rec.Name}");
//                    //response.Data = false;//cannot set multiple amps (with different FId but same code as 'active')
//                    //return false;
//                    isValid = false;
//                }

//                ampParentCodeWithParentFormularyId[rec.ParentCode] = rec.ParentFormularyId;
//            }
//            response.Data = isValid;
//            return isValid;
//        }
//    }
//}
