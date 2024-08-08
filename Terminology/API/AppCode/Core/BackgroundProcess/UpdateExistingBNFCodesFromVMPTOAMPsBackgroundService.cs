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
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Core.BackgroundProcess
{
    public class UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private IMapper _mapper;
        private ILogger<UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService> _logger;
        private IServiceProvider _serviceProvider;

        public UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService(IServiceScopeFactory serviceScopeFactory, IMapper mapper, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _mapper = mapper;
            _logger = loggerFactory.CreateLogger<UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService>();
            _serviceProvider = serviceProvider;
        }

        public void UpdateExistingBNFCodesFromVMPTOAMPs(string messageId, IServiceScope scope, IRepository<FormularyHeader> repo, IRepository<FormularyAdditionalCode> additionalCodeRepo)
        {
            //Invoke and forget but check whether accepted

            //_ = Task.Run(async () =>
            //{
            try
            {
                _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingBNFCodesFromVMPTOAMPs API - invocation Started."); //only error severity is configured for logging.
                //From VMP, get map of Code and its BNFCode Additional codes
                //From AMP, get parentcode and the FormularyVersionIds 
                //If the AMP has missing BNF the the BNF code in the VMP (only first 7 chars) will be copied to the child AMP

                //using (var scope = _serviceScopeFactory.CreateScope())
                //{
                //    var serviceProvider = scope.ServiceProvider;

                //    var repo = serviceProvider.GetRequiredService(typeof(IRepository<FormularyHeader>)) as IRepository<FormularyHeader>;

                //    var additionalCodeRepo = serviceProvider.GetRequiredService(typeof(IRepository<FormularyAdditionalCode>)) as IRepository<FormularyAdditionalCode>;

                //var formularyRepo = serviceProvider.GetRequiredService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

                //From VMP, get map of Code and its BNFCode Additional codes
                var vmpCodeWithItsBNFsLKP = GetVMPCodeWithItsAdditionalBNFsLkp(repo, additionalCodeRepo);

                if (!vmpCodeWithItsBNFsLKP.IsCollectionValid())
                {
                    _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingBNFCodesFromVMPTOAMPs API - Stopped: Could not find any VMPs With BNF codes.");
                    return;// Task.CompletedTask;
                }

                //From AMP, get parentcode and the FormularyVersionIds 
                var ampParentCodeWithItsFVIdsLkp = GetAMPCodeWithItsFVIdsAndBNFsLkp(repo, additionalCodeRepo);

                if (!ampParentCodeWithItsFVIdsLkp.IsCollectionValid())
                {
                    _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingBNFCodesFromVMPTOAMPs API - Stopped: Could not find any AMPs in the system.");
                    return;// Task.CompletedTask;
                }

                _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingBNFCodesFromVMPTOAMPs API - Updating the records.");

                foreach (var parentVMPCode in vmpCodeWithItsBNFsLKP.Keys)
                {
                    if (!ampParentCodeWithItsFVIdsLkp.ContainsKey(parentVMPCode)) continue;

                    //Need to add the BNF for all these amps from the VMP
                    var ampFVIds = ampParentCodeWithItsFVIdsLkp[parentVMPCode];

                    if (!ampFVIds.IsCollectionValid()) continue;

                    foreach (var ampfvIdWithBNFs in ampFVIds)
                    {
                        //If this AMP FVId already has any BNF code then no need to copy from VMP
                        if (ampfvIdWithBNFs.ampBNFs.IsCollectionValid()) continue;

                        //for each of bnfs in VMP, add to the amp
                        var vmpsBNFs = vmpCodeWithItsBNFsLKP[parentVMPCode];

                        if (!vmpsBNFs.IsCollectionValid()) continue;

                        foreach (var bnfInVMP in vmpsBNFs)
                        {
                            if (bnfInVMP.AdditionalCode.IsEmpty()) continue;

                            var bnfCode = bnfInVMP.AdditionalCode.Length > 7 ? bnfInVMP.AdditionalCode.Substring(0, 7): bnfInVMP.AdditionalCode;

                            /*Not just this bnf code in vmp, any bnf code in AMP then ignore the copy - hence commenting
                            //check if this BNF already exists in AMP if then ignore that
                            if (ampfvIdWithBNFs.ampBNFs.IsCollectionValid() && ampfvIdWithBNFs.ampBNFs.Contains(bnfCode))
                                continue;
                            */

                            //var newAdditionalGen = new FormularyAdditionalCode();
                            var newAdditional = new FormularyAdditionalCode();// _mapper.Map<FormularyAdditionalCode>(bnfInVMP);//clone it

                            //newAdditional.RowId = null;//to be auto generated
                            newAdditional.FormularyVersionId = ampfvIdWithBNFs.fvId;
                            newAdditional.AdditionalCode = bnfInVMP.AdditionalCode.IsNotEmpty() ? ((bnfInVMP.AdditionalCode.Length > 7) ? bnfInVMP.AdditionalCode.Substring(0, 7): bnfInVMP.AdditionalCode) : null;
                            newAdditional.AdditionalCodeDesc = bnfInVMP.AdditionalCodeDesc;
                            newAdditional.AdditionalCodeSystem = "BNF";
                            newAdditional.MetaJson = bnfInVMP.MetaJson;
                            newAdditional.Source = bnfInVMP.Source;
                            newAdditional.Attr1 = bnfInVMP.Attr1;
                            newAdditional.CodeType = bnfInVMP.CodeType;
                            newAdditional.FormularyVersion = bnfInVMP.FormularyVersion;
                            newAdditional.Tenant = bnfInVMP.Tenant;
                            newAdditional.Updateddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
                            newAdditional.Updatedtimestamp = DateTime.UtcNow;
                            newAdditional.Updatedby = "System";
                            newAdditional.Createddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
                            newAdditional.Createdtimestamp = DateTime.UtcNow;
                            newAdditional.Createdby = "System";

                            //_logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingBNFCodesFromVMPTOAMPs API invocation. Adding BNF Code for: {ampfvIdWithBNFs.fvId} - {newAdditional.AdditionalCode} - {newAdditionalGen.AdditionalCode} - {bnfInVMP.AdditionalCodeDesc}");

                            additionalCodeRepo.Add(newAdditional);
                        }
                    }
                }
                additionalCodeRepo.SaveChanges();
                //}

                _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingBNFCodesFromVMPTOAMPs API invocation successfully Completed.");

                return;// Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - Exception invoking UpdateExistingBNFCodesFromVMPTOAMPs API:" + ex.ToString());
                _logger.LogError(ex, ex.ToString());
            }
            /*

            var latestVMPFormulariesFromHeader = repo.ItemsAsReadOnly.Where(rec => rec.ProductType == "VMP" && rec.IsLatest == true).ToList();
            var formularyVerIds = latestVMPFormulariesFromHeader?.Select(rec => rec.FormularyVersionId).ToList();

            if (!formularyVerIds.IsCollectionValid()) return NoContent();

            var codesOfVMPFormulariesFromHeader = latestVMPFormulariesFromHeader?.Select(rec => rec.Code)?.Distinct().ToList();

            var latestAMPFormulariesFromHeader = repo.ItemsAsReadOnly
                ?.Where(rec => rec.ProductType == "AMP" && rec.IsLatest == true && codesOfVMPFormulariesFromHeader.Contains(rec.ParentCode))
                ?.ToList();

            //var latestVMPsBNFAddnlCodes = additionalCodeRepo.ItemsAsReadOnly
            //  .Where(rec => rec.AdditionalCodeSystem == "BNF" && formularyVerIds.Contains(rec.FormularyVersionId))
            //  .ToList();

            var parentcodeFVIdsLkp = new Dictionary<string, List<string>>();

            foreach (var item in latestAMPFormulariesFromHeader)
            {
                if (!parentcodeFVIdsLkp.ContainsKey(item.ParentCode))
                {
                    parentcodeFVIdsLkp[item.ParentCode] = new List<string>() { item.FormularyVersionId };
                }
                else
                {
                    parentcodeFVIdsLkp[item.ParentCode].Add(item.FormularyVersionId);
                }
            }

            var vmpAllFormularies = formularyRepo.GetFormularyBasicDetailListForIds(formularyVerIds, true).ToList();

            if (!vmpAllFormularies.IsCollectionValid()) return NoContent();

            var vmpLatestFormularies = vmpAllFormularies.Where(rec => rec.IsLatest == true).ToList();

            if (!vmpLatestFormularies.IsCollectionValid()) return NoContent();

            foreach (var vmpFormulary in vmpLatestFormularies)
            {
                if (vmpFormulary == null || !vmpFormulary.FormularyAdditionalCode.IsCollectionValid()) continue;

                var formularyAdditinalsFromDB = _mapper.Map<List<FormularyAdditionalCode>>(vmpFormulary.FormularyAdditionalCode);//clone it

                var bnfRecordsFromDB = formularyAdditinalsFromDB.Where(rec => string.Compare(rec.AdditionalCodeSystem, "bnf", true) == 0)?.ToList();

                //var bnfRecords = _mapper
                if (!bnfRecordsFromDB.IsCollectionValid()) continue;

                //var ampsFVIds = parentcodeFVIdsLkp.ContainsKey(vmpFormulary.Code) ? parentcodeFVIdsLkp[vmpFormulary.Code] : null;

                //if (!ampsFVIds.IsCollectionValid()) continue;

                //Get Formulary additional codes for the AMPs and use its FVId to build new Additional codes for BNF from its VMP

                if (!parentcodeFVIdsLkp.ContainsKey(vmpFormulary.Code)) continue;

                var fvIdsOfChildAMPs = parentcodeFVIdsLkp[vmpFormulary.Code];

                foreach (var parentBNF in bnfRecordsFromDB)
                {
                    foreach (var fvId in fvIdsOfChildAMPs)
                    {
                        var newAdditionalGen = new FormularyAdditionalCode();
                        var newAdditional = _mapper.Map(parentBNF, newAdditionalGen);//clone it
                        newAdditional.RowId = null;//to be auto generated
                        newAdditional.FormularyVersionId = fvId;

                        additionalCodeRepo.Add(newAdditional);
                    }
                }

                additionalCodeRepo.SaveChanges();
            }
            */
            return;// Task.CompletedTask;
            //});
        }

        private Dictionary<string, List<(string fvId, List<string> ampBNFs)>> GetAMPCodeWithItsFVIdsAndBNFsLkp(IRepository<FormularyHeader> repo, IRepository<FormularyAdditionalCode> additionalCodeRepo)
        {
            var latestAMPFormulariesFromHeader = repo.ItemsAsReadOnly
                   ?.Where(rec => rec.ProductType == "AMP" && rec.IsLatest == true)
                   ?.ToList();


            if (!latestAMPFormulariesFromHeader.IsCollectionValid()) return null;

            var uniqueFVIds = latestAMPFormulariesFromHeader.Select(rec => rec.FormularyVersionId).Distinct().ToList();

            var latestAMPFormularyAddnlCodesForFVIds = additionalCodeRepo.ItemsAsReadOnly
                   ?.Where(rec => uniqueFVIds.Contains(rec.FormularyVersionId) && rec.CodeType == TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE && rec.AdditionalCodeSystem == "BNF")
                   ?.ToList();

            var fvIdBNFsLkp = new Dictionary<string, List<string>>();



            if (latestAMPFormularyAddnlCodesForFVIds.IsCollectionValid())
            {
                foreach (var item in latestAMPFormularyAddnlCodesForFVIds)
                {
                    var val = item.AdditionalCode != null? (item.AdditionalCode.Length > 7 ? item.AdditionalCode.Substring(0, 7): item.AdditionalCode) : null;

                    if (val.IsEmpty()) continue;

                    if (!fvIdBNFsLkp.ContainsKey(item.FormularyVersionId))
                    {
                        fvIdBNFsLkp[item.FormularyVersionId] = new List<string> { val };
                    }
                    else
                    {
                        if (item.AdditionalCode.IsNotEmpty())
                        {
                            if (!fvIdBNFsLkp[item.FormularyVersionId].Any(rec => (rec == val)))
                                fvIdBNFsLkp[item.FormularyVersionId].Add(val);
                        }
                    }
                }
            }

            //Group by its parentcode and take all FVIds for that parentcode
            var parentcodeWithItsFVIdsLkp = new Dictionary<string, List<(string fvId, List<string> ampBNFs)>>();

            foreach (var ampHeader in latestAMPFormulariesFromHeader)
            {
                var ampBNFs = fvIdBNFsLkp.ContainsKey(ampHeader.FormularyVersionId) ? fvIdBNFsLkp[ampHeader.FormularyVersionId] : null;

                if (!parentcodeWithItsFVIdsLkp.ContainsKey(ampHeader.ParentCode))
                {
                    parentcodeWithItsFVIdsLkp[ampHeader.ParentCode] = new List<(string fvId, List<string> ampBNFs)> { (ampHeader.FormularyVersionId, ampBNFs) };
                }
                else
                {
                    parentcodeWithItsFVIdsLkp[ampHeader.ParentCode].Add((ampHeader.FormularyVersionId, ampBNFs));
                }
            }
            return parentcodeWithItsFVIdsLkp;
        }

        private Dictionary<string, List<FormularyAdditionalCode>> GetVMPCodeWithItsAdditionalBNFsLkp(IRepository<FormularyHeader> repo, IRepository<FormularyAdditionalCode> additionalCodeRepo)
        {
            var latestVMPFormulariesFromHeader = repo.ItemsAsReadOnly.Where(rec => rec.ProductType == "VMP" && rec.IsLatest == true).ToList();

            var formularyVerIds = latestVMPFormulariesFromHeader?.Select(rec => rec.FormularyVersionId).ToList();

            if (!formularyVerIds.IsCollectionValid()) return null;

            var bnfsForVMP = additionalCodeRepo.ItemsAsReadOnly?.Where(rec => rec.CodeType == TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE &&
            rec.AdditionalCodeSystem == "BNF" && formularyVerIds.Contains(rec.FormularyVersionId))?.ToList();

            if (!bnfsForVMP.IsCollectionValid()) return null;

            var codeAdditionalsLkp = new Dictionary<string, List<FormularyAdditionalCode>>();

            foreach (var header in latestVMPFormulariesFromHeader)
            {
                var bnfAdditionalsForHeader = bnfsForVMP.Where(rec => rec.FormularyVersionId == header.FormularyVersionId)?.Select(rec =>
                {
                    var clonedRec = _mapper.Map<FormularyAdditionalCode>(rec);
                    //clonedRec.AdditionalCode = clonedRec.AdditionalCode.Substring(0, 7);//NOt required can be considered while reading
                    return clonedRec;
                })?.Distinct(rec => rec.AdditionalCode)?.ToList();

                if (!bnfAdditionalsForHeader.IsCollectionValid()) continue;

                if (!codeAdditionalsLkp.ContainsKey(header.Code))
                {
                    codeAdditionalsLkp[header.Code] = bnfAdditionalsForHeader;
                }
                else
                {
                    codeAdditionalsLkp[header.Code].AddRange(bnfAdditionalsForHeader);
                }
            }

            return codeAdditionalsLkp;
        }
    }
}
