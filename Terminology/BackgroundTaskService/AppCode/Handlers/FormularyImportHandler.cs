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
using Interneuron.FDBAPI.Client;
using Interneuron.FDBAPI.Client.DataModels;
using Interneuron.Terminology.BackgroundTaskService.API.AppCode.Commands.ImportMergeHandlers;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Extensions;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers.ImportRules;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers.Util;
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;
using Interneuron.Terminology.BackgroundTaskService.Repository;
using System.Collections.Concurrent;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers
{
    public class FormularyImportHandlerResponse
    {
        public List<string> Errors { get; set; }

        public short Status { get; set; }
    }

    public class FormularyImportHandler : FormularyImportBaseHandler
    {
        private string _defaultFormularyStatusCode;
        private string _defaultRecordStatusCode;
        private IConfiguration _configuration;
        private ILogger<FormularyImportHandler> _logger;
        private TerminologyAPIService _terminologyAPIService;
        private IMapper _mapper;
        private ILoggerFactory _loggerFactory;
        private IUnitOfWork _unitOfWork;
        private IServiceProvider _serviceProvider;
        private FormularyUtil _formularyUtil;
        private ConcurrentDictionary<string, List<FormularyExcipient>> _dmdAMPExcipientsForCodes;
        private ConcurrentDictionary<string, List<DmdAmpDrugrouteDTO>> _dmdAMPRouteMappings;
        private ConcurrentDictionary<string, List<DmdVmpDrugrouteDTO>> _dmdVMPRouteMappings;
        private ConcurrentDictionary<string, List<FormularyAdditionalCode>> _dmdAdditionalCodesForCodes;

        private ConcurrentDictionary<string, SnomedModifiedReleaseDTO> _snomedModifiedReleaseMappings;
        private ConcurrentDictionary<string, SnomedTradeFamiliesDTO> _snomedTradeFamilyMappings;
        private Dictionary<string, List<string>> _cautionsForCodes;
        private Dictionary<string, List<string>> _sideEffectsForCodes;
        private Dictionary<string, List<string>> _safetyMessagesForCodes;
        private Dictionary<string, List<FDBIdText>> _contraIndicationsForCodes;
        private Dictionary<string, List<FDBIdText>> _licensedUsesForCodes;
        private Dictionary<string, bool> _blackTriangleFlagForCodes;
        private Dictionary<string, List<FDBIdText>> _unLicensedUsesForCodes;
        private Dictionary<string, bool?> _highRiskFlagForCodes;
        private Dictionary<string, List<(string, string)>>? _therapeuticClassForCodes;

        public FormularyImportHandler(IMapper mapper, IConfiguration configuration, ILogger<FormularyImportHandler> logger, TerminologyAPIService terminologyAPIService, IUnitOfWork unitOfWork, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, FormularyUtil formularyUtil)
        {
            _configuration = configuration;
            _logger = logger;
            _terminologyAPIService = terminologyAPIService;
            _mapper = mapper;
            _loggerFactory = loggerFactory;
            _unitOfWork = unitOfWork;
            _serviceProvider = serviceProvider;
            _formularyUtil = formularyUtil;
        }

        public async Task<FormularyImportHandlerResponse> ImportByHistoricCodes(List<string> dmdCodes, string defaultFormularyStatusCode = TerminologyConstants.FORMULARYSTATUS_NONFORMULARY, string defaultRecordStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT)
        {
            //Historic codes to be handled in chronological order and in batches here
            return null;
        }

        public async Task<FormularyImportHandlerResponse> ImportByCodes(List<string> dmdCodes, string defaultFormularyStatusCode = TerminologyConstants.FORMULARYSTATUS_NONFORMULARY, string defaultRecordStatusCode = TerminologyConstants.RECORDSTATUS_DRAFT)
        {
            FormularyImportHandlerResponse response = new() { Status = 0, Errors = new List<string>() };

            _defaultFormularyStatusCode = defaultFormularyStatusCode;
            _defaultRecordStatusCode = defaultRecordStatusCode;

            var dmdResponse = await _terminologyAPIService.GetDMDFullDataForCodes(dmdCodes);

            if (dmdResponse == null || dmdResponse.StatusCode == DataService.StatusCode.Fail || !dmdResponse.Data.IsCollectionValid())
            {
                if (dmdResponse?.StatusCode == DataService.StatusCode.Success)
                    return response;

                var errorsFromAPI = dmdResponse.ErrorMessages.IsCollectionValid() ? string.Join(",", dmdResponse.ErrorMessages) : String.Empty;
                response.Errors.Add($"No details in the DM+D Formulary for the DMD Codes. Errors: {errorsFromAPI}");
                return response;
            }
            await PrefillDataForImport(dmdResponse.Data);

            HandleImportSave(dmdResponse.Data);

            ResetLocals();

            return response;
        }

        private void ResetLocals()
        {
            if (_dmdAMPExcipientsForCodes.IsCollectionValid())
                _dmdAMPExcipientsForCodes.Clear();
            if (_dmdAMPRouteMappings.IsCollectionValid())
                _dmdAMPRouteMappings.Clear();
            if (_dmdVMPRouteMappings.IsCollectionValid())
                _dmdVMPRouteMappings.Clear();
            if (_dmdAdditionalCodesForCodes.IsCollectionValid())
                _dmdAdditionalCodesForCodes.Clear();
            if (_snomedModifiedReleaseMappings.IsCollectionValid())
                _snomedModifiedReleaseMappings.Clear();

            if (_snomedTradeFamilyMappings.IsCollectionValid())
                _snomedTradeFamilyMappings.Clear();
            if (_cautionsForCodes.IsCollectionValid())
                _cautionsForCodes.Clear();
            if (_sideEffectsForCodes.IsCollectionValid())
                _sideEffectsForCodes.Clear();
            if (_safetyMessagesForCodes.IsCollectionValid())
                _safetyMessagesForCodes.Clear();
            if (_contraIndicationsForCodes.IsCollectionValid())
                _contraIndicationsForCodes.Clear();
            if (_licensedUsesForCodes.IsCollectionValid())
                _licensedUsesForCodes.Clear();
            if (_blackTriangleFlagForCodes.IsCollectionValid())
                _blackTriangleFlagForCodes.Clear();
            if (_unLicensedUsesForCodes.IsCollectionValid())
                _unLicensedUsesForCodes.Clear();
            if (_highRiskFlagForCodes.IsCollectionValid())
                _highRiskFlagForCodes.Clear();
            if (_therapeuticClassForCodes.IsCollectionValid())
                _therapeuticClassForCodes.Clear();
        }

        #region Data Collector
        private async Task PrefillDataForImport(List<DMDDetailResultDTO> dmdResults)
        {
            //MMC-477-Moved to base class
            //await PrefillDMDLookup();

            await AssignExcipients(dmdResults);

            PrefillAdditionalCodesForDMDCodes(dmdResults);

            await PrefillTradeFamiliesFromSNOMED(dmdResults);

            await PrefillModifiedReleaseFromSNOMED(dmdResults);

            await PrefillAllVMPMappedRoutesFromDMD(dmdResults);

            await PrefillAllAMPMappedRoutesFromDMD(dmdResults);

            await PreFillFDBRecords(dmdResults);
        }


        private async Task AssignExcipients(List<DMDDetailResultDTO> dmdResults)
        {
            var dmdCodes = dmdResults.Where(rec => rec.LogicalLevel == 3)?.Select(rec => rec.Code).Distinct().ToList();//Only for AMPs

            _dmdAMPExcipientsForCodes = new ConcurrentDictionary<string, List<FormularyExcipient>>();

            if (!dmdCodes.IsCollectionValid()) return;

            var dmdAMPExcipientsResp = await _terminologyAPIService.GetAMPExcipientsForCodes(dmdCodes);

            if (dmdAMPExcipientsResp == null || dmdAMPExcipientsResp.Data == null || !dmdAMPExcipientsResp.Data.IsCollectionValid()) return;

            dmdAMPExcipientsResp.Data.AsParallel().Each(rec =>
            {
                var formularyExcipient = _mapper.Map<FormularyExcipient>(rec);

                if (_dmdAMPExcipientsForCodes.ContainsKey(rec.Apid))
                    _dmdAMPExcipientsForCodes[rec.Apid].Add(formularyExcipient);
                else
                    _dmdAMPExcipientsForCodes[rec.Apid] = new List<FormularyExcipient> { formularyExcipient };
            });
        }

        private void PrefillAdditionalCodesForDMDCodes(List<DMDDetailResultDTO> dmdResults)
        {
            _dmdAdditionalCodesForCodes = new ConcurrentDictionary<string, List<FormularyAdditionalCode>>();

            var dmdCodes = dmdResults?.Select(rec => rec.Code).Distinct().ToList();

            if (!dmdCodes.IsCollectionValid()) return;
            var dmdLookupProvider = getDMDLookupProvider();//let it throw error if not provided 
            var bnfLkps = dmdLookupProvider._bnfsLkp;

            dmdCodes.AsParallel().Each(rec =>
            {
                var atcForDMD = dmdLookupProvider._dmdATCCodes?.Where(atc => atc.DmdCd == rec && atc.Cd.IsNotEmpty()).ToList();

                var bnfsForDMD = dmdLookupProvider._dmdBNFCodes?
                .Where(bnf => bnf.Cd.IsNotEmpty() && bnf.DmdCd == rec)?
                .Select(rec => new DmdBNFCodeDTO { Cd = rec.Cd != null ? (rec.Cd.Length > 7 ? rec.Cd.Substring(0, 7): rec.Cd) : "", DmdCd = rec.DmdCd, Desc = rec.Desc })?
                //.OrderByDescending(rec => rec.Cd)?
                .Distinct(rec => rec.Cd)?
                .ToList();

                _dmdAdditionalCodesForCodes[rec] = new List<FormularyAdditionalCode>();

                if (atcForDMD.IsCollectionValid())
                {
                    var formularyAddnls = _mapper.Map<List<FormularyAdditionalCode>>(atcForDMD);
                    _dmdAdditionalCodesForCodes[rec].AddRange(formularyAddnls);
                }

                if (bnfsForDMD.IsCollectionValid() && bnfLkps.IsCollectionValid())
                {
                    var bnfsToAdd = new List<DmdBNFCodeDTO>();
                    
                    bnfsForDMD.Each(bnfAddnlCode =>
                    {
                        var codesToBeAdded = new List<string>();
                        if (bnfAddnlCode.Cd.Length >= 2)
                            codesToBeAdded.Add(bnfAddnlCode.Cd.Substring(0, 2));
                        if (bnfAddnlCode.Cd.Length >= 4)
                            codesToBeAdded.Add(bnfAddnlCode.Cd.Substring(0, 4));
                        if (bnfAddnlCode.Cd.Length >= 6)
                            codesToBeAdded.Add(bnfAddnlCode.Cd.Substring(0, 6));
                        if (bnfAddnlCode.Cd.Length >= 7)
                            codesToBeAdded.Add(bnfAddnlCode.Cd.Substring(0, 7));

                        codesToBeAdded = codesToBeAdded.Distinct().ToList();

                        foreach (var code in codesToBeAdded)
                        {
                            if (!bnfLkps.ContainsKey(code)) continue;

                            var dmdBnf = new DmdBNFCodeDTO()
                            {
                                DmdCd = bnfAddnlCode.DmdCd,
                                Cd = code,
                                Desc = bnfLkps[code]
                            };

                            bnfsToAdd.Add(dmdBnf);
                        }
                    });

                    var formularyAddnls = _mapper.Map<List<FormularyAdditionalCode>>(bnfsToAdd);// (bnfsForDMD);
                    _dmdAdditionalCodesForCodes[rec].AddRange(formularyAddnls);
                }
            });
        }

        private async Task PrefillTradeFamiliesFromSNOMED(List<DMDDetailResultDTO> dmdResults)
        {
            _snomedTradeFamilyMappings = new ConcurrentDictionary<string, SnomedTradeFamiliesDTO>();

            var dmdCodes = dmdResults?.Select(rec => rec.Code).Distinct().ToList();

            if (!dmdCodes.IsCollectionValid()) return;

            var tradeFamiliiesResp = await _terminologyAPIService.GetTradeFamilyForConceptIds(dmdCodes, true);

            if (tradeFamiliiesResp == null || tradeFamiliiesResp.Data == null || !tradeFamiliiesResp.Data.IsCollectionValid()) return;

            dmdCodes.AsParallel().Each(rec =>
            {
                var tradeFamilyForCode = tradeFamiliiesResp.Data.FirstOrDefault(tf => tf.BrandedDrugId == rec);

                if (tradeFamilyForCode != null)
                {
                    _snomedTradeFamilyMappings[rec] = tradeFamilyForCode;
                }
            });
        }

        private async Task PrefillModifiedReleaseFromSNOMED(List<DMDDetailResultDTO> dmdResults)
        {
            _snomedModifiedReleaseMappings = new ConcurrentDictionary<string, SnomedModifiedReleaseDTO>();
            var dmdCodes = dmdResults?.Select(rec => rec.Code).Distinct().ToList();

            if (!dmdCodes.IsCollectionValid()) return;

            var modifiedreleaseResp = await _terminologyAPIService.GetModifiedReleaseForConceptIds(dmdCodes, true);

            if (modifiedreleaseResp == null || modifiedreleaseResp.Data == null || !modifiedreleaseResp.Data.IsCollectionValid()) return;

            dmdCodes.AsParallel().Each(rec =>
            {
                var modifiedReleaseCode = modifiedreleaseResp.Data.FirstOrDefault(tf => tf.DrugId == rec);

                if (modifiedReleaseCode != null)
                {
                    _snomedModifiedReleaseMappings[rec] = modifiedReleaseCode;
                }
            });
        }

        private async Task PrefillAllAMPMappedRoutesFromDMD(List<DMDDetailResultDTO> dmdResults)
        {
            var dmdCodes = dmdResults.Where(rec => rec.LogicalLevel == 3)?.Select(rec => rec.Code).Distinct().ToList();//Only for AMPs

            _dmdAMPRouteMappings = new ConcurrentDictionary<string, List<DmdAmpDrugrouteDTO>>();

            if (!dmdCodes.IsCollectionValid()) return;


            var dmdAMPDrugRoutesResp = await _terminologyAPIService.GetAMPDrugRoutesForCodes(dmdCodes);

            if (dmdAMPDrugRoutesResp == null || dmdAMPDrugRoutesResp.Data == null || !dmdAMPDrugRoutesResp.Data.IsCollectionValid()) return;

            dmdAMPDrugRoutesResp.Data.AsParallel().Each(rec =>
            {
                if (_dmdAMPRouteMappings.ContainsKey(rec.Apid))
                    _dmdAMPRouteMappings[rec.Apid].Add(rec);
                else
                    _dmdAMPRouteMappings[rec.Apid] = new List<DmdAmpDrugrouteDTO> { rec };
            });
        }

        private async Task PrefillAllVMPMappedRoutesFromDMD(List<DMDDetailResultDTO> dmdResults)
        {
            var dmdCodes = dmdResults.Where(rec => rec.LogicalLevel == 2)?.Select(rec => rec.Code).Distinct().ToList();//Only for VMPs

            _dmdVMPRouteMappings = new ConcurrentDictionary<string, List<DmdVmpDrugrouteDTO>>();

            if (!dmdCodes.IsCollectionValid()) return;

            var dmdVMPDrugRoutesResp = await _terminologyAPIService.GetVMPDrugRoutesForCodes(dmdCodes);

            if (dmdVMPDrugRoutesResp == null || dmdVMPDrugRoutesResp.Data == null || !dmdVMPDrugRoutesResp.Data.IsCollectionValid()) return;

            dmdVMPDrugRoutesResp.Data.AsParallel().Each(rec =>
            {
                if (_dmdVMPRouteMappings.ContainsKey(rec.Vpid))
                    _dmdVMPRouteMappings[rec.Vpid].Add(rec);
                else
                    _dmdVMPRouteMappings[rec.Vpid] = new List<DmdVmpDrugrouteDTO> { rec };
            });
        }

        private async Task<bool> PreFillFDBRecords(List<DMDDetailResultDTO> dmdResults)
        {
            if (!dmdResults.IsCollectionValid()) return false;

            if (_configuration?["TerminologyBackgroundTaskConfig:ImportFDBData"] != null &&
                string.Compare(_configuration["TerminologyBackgroundTaskConfig:ImportFDBData"], "true", true) != 0)
                return true;

            var codesAndProductTypes = dmdResults.Select(res => new FDBDataRequest()
            {
                ProductType = res.LogicalLevel.GetDMDLevelCodeByLogicalLevel(),
                ProductCode = res.Code
            }).ToList();

            if (!codesAndProductTypes.IsCollectionValid()) return false;

            var ampOnlyCodes = codesAndProductTypes.Where(rec => string.Compare(rec.ProductType, "amp", true) == 0).ToList();

            if (!ampOnlyCodes.IsCollectionValid()) return true;

            var baseFDBUrl = _configuration.GetSection("FDB").GetValue<string>("BaseURL");

            baseFDBUrl = baseFDBUrl.EndsWith("/") ? baseFDBUrl.TrimEnd('/') : baseFDBUrl;

            var token = await _terminologyAPIService.GetAccessToken();

            var fdbClient = new FDBAPIClient(baseFDBUrl, _loggerFactory);

            var cautionsTask = await fdbClient.GetCautionsByCodes(ampOnlyCodes, token);
            var sideEffectsTask = await fdbClient.GetSideEffectsByCodes(ampOnlyCodes, token);
            var safetyMessagesTask = await fdbClient.GetSafetyMessagesByCodes(ampOnlyCodes, token);
            var contraIndicationsTask = await fdbClient.GetContraIndicationsByCodes(ampOnlyCodes, token);
            var licensedUsesTask = await fdbClient.GetLicensedUseByCodes(ampOnlyCodes, token);
            var unLicensedUsesTask = await fdbClient.GetUnLicensedUseByCodes(ampOnlyCodes, token);
            var blackTriangleFlagTask = await fdbClient.GetAdverseEffectsFlagByCodes(ampOnlyCodes, token);
            var highRiskFlagTask = await fdbClient.GetHighRiskFlagByCodes(ampOnlyCodes, token);
            //var theraupeuticClassTask = await fdbClient.GetTherapeuticClassificationGroupsByCodes(ampOnlyCodes, token);
            var theraupeuticClassTask = await fdbClient.GetAllTherapeuticClassificationGroupsByCodes(ampOnlyCodes, token);

            var cautionsResult = cautionsTask; var sideEffectsResult = sideEffectsTask; var safetyMessagesResult = safetyMessagesTask; var contraIndicationsResult = contraIndicationsTask; var licensedUsesResult = licensedUsesTask; var unLicensedUsesResult = unLicensedUsesTask;
            var highRiskFlag = highRiskFlagTask;
            var theraupeuticClass = theraupeuticClassTask;

            if (cautionsResult.Data.IsCollectionValid())
            {
                _cautionsForCodes = cautionsResult.Data;
            }
            if (sideEffectsResult.Data.IsCollectionValid())
            {
                _sideEffectsForCodes = sideEffectsResult.Data;
            }
            if (safetyMessagesResult.Data.IsCollectionValid())
            {
                _safetyMessagesForCodes = safetyMessagesResult.Data;
            }

            if (contraIndicationsResult.Data.IsCollectionValid())
            {
                _contraIndicationsForCodes = contraIndicationsResult.Data;
            }

            if (licensedUsesResult.Data.IsCollectionValid())
            {
                _licensedUsesForCodes = licensedUsesResult.Data;
            }
            if (unLicensedUsesResult.Data.IsCollectionValid())
            {
                _unLicensedUsesForCodes = unLicensedUsesResult.Data;
            }

            var blackTriangleFlag = blackTriangleFlagTask;

            if (blackTriangleFlag.Data.IsCollectionValid())
            {
                _blackTriangleFlagForCodes = blackTriangleFlag.Data;
            }

            if (highRiskFlag.Data.IsCollectionValid())
            {
                _highRiskFlagForCodes = highRiskFlag.Data;
            }

            _therapeuticClassForCodes = theraupeuticClass?.Data;

            return true;

            /*
            var cautionsTask = await fdbClient.GetCautionsByCodes(ampOnlyCodes, token);
            var sideEffectsTask = await fdbClient.GetSideEffectsByCodes(ampOnlyCodes, token);
            var safetyMessagesTask = await fdbClient.GetSafetyMessagesByCodes(ampOnlyCodes, token);
            var contraIndicationsTask = await fdbClient.GetContraIndicationsByCodes(ampOnlyCodes, token);
            var licensedUsesTask = await fdbClient.GetLicensedUseByCodes(ampOnlyCodes, token);
            var unLicensedUsesTask = await fdbClient.GetUnLicensedUseByCodes(ampOnlyCodes, token);
            var blackTriangleFlagTask = await fdbClient.GetAdverseEffectsFlagByCodes(ampOnlyCodes, token);
            var highRiskFlagTask = await fdbClient.GetHighRiskFlagByCodes(ampOnlyCodes, token);
            //var theraupeuticClassTask = await fdbClient.GetTherapeuticClassificationGroupsByCodes(ampOnlyCodes, token);
            var theraupeuticClassTask = await fdbClient.GetAllTherapeuticClassificationGroupsByCodes(ampOnlyCodes, token);

            var cautionsResult = cautionsTask; var sideEffectsResult = sideEffectsTask; var safetyMessagesResult = safetyMessagesTask; var contraIndicationsResult = contraIndicationsTask; var licensedUsesResult = licensedUsesTask; var unLicensedUsesResult = unLicensedUsesTask;
            var highRiskFlag = highRiskFlagTask;
            var theraupeuticClass = theraupeuticClassTask;


            /*
            var cautionsTask = fdbClient.GetCautionsByCodes(ampOnlyCodes, token);
            var sideEffectsTask = fdbClient.GetSideEffectsByCodes(ampOnlyCodes, token);
            var safetyMessagesTask = fdbClient.GetSafetyMessagesByCodes(ampOnlyCodes, token);
            var contraIndicationsTask = fdbClient.GetContraIndicationsByCodes(ampOnlyCodes, token);
            var licensedUsesTask = fdbClient.GetLicensedUseByCodes(ampOnlyCodes, token);
            var unLicensedUsesTask = fdbClient.GetUnLicensedUseByCodes(ampOnlyCodes, token);
            var blackTriangleFlagTask = fdbClient.GetAdverseEffectsFlagByCodes(ampOnlyCodes, token);
            var highRiskFlagTask = fdbClient.GetHighRiskFlagByCodes(ampOnlyCodes, token);
            //var theraupeuticClassTask = await fdbClient.GetTherapeuticClassificationGroupsByCodes(ampOnlyCodes, token);
            var theraupeuticClassTask = fdbClient.GetAllTherapeuticClassificationGroupsByCodes(ampOnlyCodes, token);

            await Task.WhenAll(cautionsTask, sideEffectsTask, safetyMessagesTask, contraIndicationsTask, licensedUsesTask, unLicensedUsesTask, blackTriangleFlagTask, highRiskFlagTask, theraupeuticClassTask);

             var cautionsResult = await cautionsTask; 
            var sideEffectsResult = await sideEffectsTask; 
            var safetyMessagesResult = await safetyMessagesTask; 
            var contraIndicationsResult = await contraIndicationsTask; 
            var licensedUsesResult = await licensedUsesTask;
            var unLicensedUsesResult = await unLicensedUsesTask;
            var highRiskFlag = await highRiskFlagTask;
            var theraupeuticClass = await theraupeuticClassTask;
            var blackTriangleFlag = await blackTriangleFlagTask;
            */

            /*
            var cautionsResult =  cautionsTask; 
            var sideEffectsResult =  sideEffectsTask; 
            var safetyMessagesResult =  safetyMessagesTask; 
            var contraIndicationsResult = await contraIndicationsTask; 
            var licensedUsesResult = await licensedUsesTask;
            var unLicensedUsesResult = await unLicensedUsesTask;
            var highRiskFlag = await highRiskFlagTask;
            var theraupeuticClass = await theraupeuticClassTask;
            var blackTriangleFlag = await blackTriangleFlagTask;

            if (cautionsResult.Data.IsCollectionValid())
            {
                _cautionsForCodes = cautionsResult.Data;
            }
            if (sideEffectsResult.Data.IsCollectionValid())
            {
                _sideEffectsForCodes = sideEffectsResult.Data;
            }
            if (safetyMessagesResult.Data.IsCollectionValid())
            {
                _safetyMessagesForCodes = safetyMessagesResult.Data;
            }

            if (contraIndicationsResult.Data.IsCollectionValid())
            {
                _contraIndicationsForCodes = contraIndicationsResult.Data;
            }

            if (licensedUsesResult.Data.IsCollectionValid())
            {
                _licensedUsesForCodes = licensedUsesResult.Data;
            }
            if (unLicensedUsesResult.Data.IsCollectionValid())
            {
                _unLicensedUsesForCodes = unLicensedUsesResult.Data;
            }

            if (blackTriangleFlag.Data.IsCollectionValid())
            {
                _blackTriangleFlagForCodes = blackTriangleFlag.Data;
            }

            if (highRiskFlag.Data.IsCollectionValid())
            {
                _highRiskFlagForCodes = highRiskFlag.Data;
            }

            _therapeuticClassForCodes = theraupeuticClass?.Data;

            return true;
            */
        }
        #endregion Data Collector

        private void HandleImportSave(List<DMDDetailResultDTO> dmdResults)
        {
            var formulariesToSave = PopulateFormulariesForImport(dmdResults);

            if (formulariesToSave.IsCollectionValid())
            {
                formulariesToSave.Each(saveFormulary =>
                {
                    _unitOfWork.FormularyHeaderFormularyRepository.Add(saveFormulary);
                });

                _unitOfWork.FormularyHeaderFormularyRepository.SaveChanges();

                formulariesToSave.Clear();
                formulariesToSave = null;
            }

            if (_unitOfWork != null)
                _unitOfWork.Dispose();
        }

        private List<FormularyHeader> PopulateFormulariesForImport(List<DMDDetailResultDTO> dmdResults)
        {
            var formulariesToSave = new List<FormularyHeader>();

            //Note: This returns trackable formularies - handle with care
            //Causing memory issue - better to fetch individual when required
            var existingLatestFormulariesForCodes = GetExistingLatestFormulariesByCodes(dmdResults);
            var existingLatestFormulariesUpdatables = GetExistingLatestFormulariesUpdatables(dmdResults);

            foreach (var res in dmdResults)
            {
                var formularyHeader = CreateHeaderForImport(res);

                //check for duplicates
                //var checkIfCodeExistsInDB = existingFormulariesForCodes.Where(x => (x.Code == res.Code || x.Code == res.PrevCode) && x.IsLatest == true && string.Compare(x.ProductType, res.LogicalLevel.GetDMDLevelCodeByLogicalLevel(), true) == 0);

                string productType = res.LogicalLevel.GetDMDLevelCodeByLogicalLevel().ToLower();

                switch (productType)
                {
                    case "amp":
                        //MMC-477 - should copy if AMP is changed
                        //if (checkIfCodeExistsInDB.Count() == 0 || (checkIfCodeExistsInDB.Count() > 0 && checkIfCodeExistsInDB.FirstOrDefault().RecStatusCode != "001"))
                        {
                            PopulateFormularyDetailForImport(res, formularyHeader);

                            PopulateFormularyIngredientsForImport(res, formularyHeader);

                            PopulateFormularyExcipientsForImport(res, formularyHeader);

                            PopulateFormularyRouteDetailsForImport(res, formularyHeader);

                            PopulateFormularyAdditionalCodesForImport(res, formularyHeader);

                            ApplyRules(res, formularyHeader);

                            OverwriteFromExisting(res, formularyHeader, existingLatestFormulariesForCodes, existingLatestFormulariesUpdatables);
                            //OverwriteFromExisting(res, formularyHeader);
                            //if (doesExistingHasLatestActive == true)
                            //    CopyDataFromActiveRecord(res, formularyHeader);

                            formulariesToSave.Add(formularyHeader);
                        }
                        break;
                    case "vmp":
                        //MMC-477 - should copy if VMP is changed
                        //if (checkIfCodeExistsInDB.Count() == 0)
                        //{
                        PopulateFormularyDetailForImport(res, formularyHeader);

                        PopulateFormularyIngredientsForImport(res, formularyHeader);

                        PopulateFormularyExcipientsForImport(res, formularyHeader);

                        PopulateFormularyRouteDetailsForVMPs(res, formularyHeader);

                        PopulateFormularyRouteDetailsForImport(res, formularyHeader);

                        PopulateFormularyAdditionalCodesForImport(res, formularyHeader);

                        ApplyRules(res, formularyHeader);

                        OverwriteFromExisting(res, formularyHeader, existingLatestFormulariesForCodes, existingLatestFormulariesUpdatables);
                        //OverwriteFromExisting(res, formularyHeader);

                        formulariesToSave.Add(formularyHeader);
                        //}
                        break;
                    case "vtm":
                        //MMC-477 - should copy if VTM is changed
                        //if (checkIfCodeExistsInDB.Count() == 0)
                        //{
                        PopulateFormularyDetailForImport(res, formularyHeader);

                        PopulateFormularyIngredientsForImport(res, formularyHeader);

                        PopulateFormularyExcipientsForImport(res, formularyHeader);

                        PopulateFormularyRouteDetailsForImport(res, formularyHeader);

                        PopulateFormularyAdditionalCodesForImport(res, formularyHeader);

                        ApplyRules(res, formularyHeader);

                        OverwriteFromExisting(res, formularyHeader, existingLatestFormulariesForCodes, existingLatestFormulariesUpdatables);
                        //OverwriteFromExisting(res, formularyHeader);

                        formulariesToSave.Add(formularyHeader);
                        //}
                        break;
                    default:
                        break;
                }
            }

            return formulariesToSave;
        }

        private Dictionary<string, FormularyHeader>? GetExistingLatestFormulariesUpdatables(List<DMDDetailResultDTO> dmdResults)
        {
            if (!dmdResults.IsCollectionValid()) return null;

            //Get Both current DMD codes and also previous DMD codes
            var dmdCodes = dmdResults.Select(rec => rec.Code).ToList();
            var prevCodes = dmdResults.Where(rec => rec.PrevCode.IsNotEmpty()).Select(rec => rec.PrevCode).ToList();

            if (prevCodes.IsCollectionValid())
                dmdCodes.AddRange(prevCodes);

            if (prevCodes.IsCollectionValid())
                dmdCodes.AddRange(prevCodes);

            var batchsize = 10;

            var batchedRequests = new List<List<string>>();
            for (var reqIndex = 0; reqIndex < dmdCodes.Count; reqIndex += batchsize)
            {
                var batches = dmdCodes.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToList());
            }
            var existingFormularies = new Dictionary<string, FormularyHeader>();

            foreach (var batch in batchedRequests)
            {
                var formularies = _unitOfWork.FormularyHeaderFormularyRepository.Items.Where(rec => batch.Contains(rec.Code) && rec.IsLatest == true)?
                    .Distinct(rec => rec.FormularyVersionId)
                    .ToList();

                if (formularies.IsCollectionValid())
                    formularies.Each(rec => existingFormularies[rec.FormularyVersionId] = rec);
            }

            return existingFormularies;
        }

        private List<FormularyHeader> GetExistingLatestFormulariesByCodes(List<DMDDetailResultDTO> dmdResults)
        {
            //Get Both current DMD codes and also previous DMD codes
            var dmdCodes = dmdResults.Select(rec => rec.Code).ToList();
            var prevCodes = dmdResults.Where(rec => rec.PrevCode.IsNotEmpty()).Select(rec => rec.PrevCode).ToList();

            if (prevCodes.IsCollectionValid())
                dmdCodes.AddRange(prevCodes);

            var batchsize = 10;

            var batchedRequests = new List<List<string>>();
            for (var reqIndex = 0; reqIndex < dmdCodes.Count; reqIndex += batchsize)
            {
                var batches = dmdCodes.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToList());
            }
            var existingFormularies = new List<FormularyHeader>();

            foreach (var batch in batchedRequests)
            {
                var scope = _serviceProvider.CreateScope();
                var svp = scope.ServiceProvider;
                var uow = svp.GetService<IUnitOfWork>();

                //var existingFormularies = _unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesByCodes(dmdCodes.ToArray()).ToList();
                var existingFormulariesInBatch = uow.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking(true).Where(rec => rec.IsLatest == true && batch.Contains(rec.Code)).ToList();

                //existingFormularies = existingFormularies?.Where(rec => rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_ARCHIVED && rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED).ToList();
                //MMC-477 - Modified since we need to remove the islatest flag for the existing archived records
                var nonDeletedExistingFormularies = existingFormulariesInBatch?.Where(rec => rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED).ToList();

                if (nonDeletedExistingFormularies.IsCollectionValid())
                    existingFormularies.AddRange(nonDeletedExistingFormularies);

                if (uow != null) uow.Dispose();
                if (scope != null) scope.Dispose();
            }

            return existingFormularies;
        }

        private FormularyHeader CreateHeaderForImport(DMDDetailResultDTO res)
        {
            var formularyHeader = new FormularyHeader();

            formularyHeader.FormularyId = Guid.NewGuid().ToString();
            formularyHeader.VersionId = 1;
            formularyHeader.FormularyVersionId = Guid.NewGuid().ToString();
            formularyHeader.IsLatest = true;
            formularyHeader.IsDuplicate = false;//Need to check

            formularyHeader.Code = res.Code;
            formularyHeader.Prevcode = res.PrevCode;
            formularyHeader.CodeSystem = TerminologyConstants.DEFAULT_IDENTIFICATION_CODE_SYSTEM;

            formularyHeader.Name = res.Name;
            formularyHeader.ParentCode = res.ParentCode;
            formularyHeader.ParentName = null;
            formularyHeader.ParentProductType = res.LogicalLevel.GetDMDParentLevelCodeByLogicalLevel();
            formularyHeader.ProductType = res.LogicalLevel.GetDMDLevelCodeByLogicalLevel();

            formularyHeader.RecSource = TerminologyConstants.RECORD_SOURCE_IMPORT;// "Import";

            //formularyHeader.RecStatusCode = _defaultRecordStatusCode; //(string.Compare(formularyHeader.ProductType, "amp", true) == 0) ? _defaultRecordStatusCode : null;// TerminologyConstants.RECORDSTATUS_DRAFT;//Draft

            //vtm and vmps will always be active
            formularyHeader.RecStatusCode = (string.Compare(formularyHeader.ProductType, "amp", true) == 0) ? TerminologyConstants.RECORDSTATUS_DRAFT : TerminologyConstants.RECORDSTATUS_ACTIVE;

            formularyHeader.RecStatuschangeDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            formularyHeader.RecStatuschangeTs = DateTime.UtcNow;

            formularyHeader.VtmId = (string.Compare(formularyHeader.ParentProductType, "vtm", true) == 0) ? formularyHeader.ParentCode : null;

            formularyHeader.VmpId = (string.Compare(formularyHeader.ParentProductType, "vmp", true) == 0) ? formularyHeader.ParentCode : null;

            formularyHeader.IsDmdInvalid = (res.Invalid == 1);

            return formularyHeader;
        }

        private void PopulateFormularyRouteDetailsForVMPs(DMDDetailResultDTO res, FormularyHeader formularyHeader)
        {
            if (!_dmdVMPRouteMappings.IsCollectionValid() || !_dmdVMPRouteMappings.ContainsKey(formularyHeader.Code)) return;

            formularyHeader.FormularyRouteDetail = formularyHeader.FormularyRouteDetail ?? new List<FormularyRouteDetail>();

            var dmdRouteForVMPs = _dmdVMPRouteMappings[formularyHeader.Code];

            var vmpRoutes = _mapper.Map<List<FormularyRouteDetail>>(dmdRouteForVMPs);

            if (!vmpRoutes.IsCollectionValid()) return;

            vmpRoutes.Each(routeDetail =>
            {
                routeDetail.FormularyVersionId = formularyHeader.FormularyVersionId;
                routeDetail.RouteFieldTypeCd = TerminologyConstants.ROUTEFIELDTYPE_ADDITONAL; //Additional
                routeDetail.Source = TerminologyConstants.DMD_DATA_SRC;

                UpdateFormularyRoutesDMDLookup(routeDetail);

                formularyHeader.FormularyRouteDetail.Add(routeDetail);
            });
        }

        private void PopulateFormularyIngredientsForImport(DMDDetailResultDTO res, FormularyHeader formularyHeader)
        {
            if (res.VMPIngredients.IsCollectionValid())
            {
                formularyHeader.FormularyIngredient = new List<FormularyIngredient>();

                res.VMPIngredients.Each(ing =>
                {
                    var ingredient = new FormularyIngredient
                    {
                        FormularyVersionId = formularyHeader.FormularyVersionId,

                        BasisOfPharmaceuticalStrengthCd = ing.BasisStrntcd?.ToString(),
                        IngredientCd = ing.Isid?.ToString(),
                        StrengthValueNumerator = ing.StrntNmrtrVal?.ToString(),
                        StrengthValueNumeratorUnitCd = ing.StrntNmrtrUomcd?.ToString(),
                        StrengthValueDenominator = ing.StrntDnmtrVal?.ToString(),
                        StrengthValueDenominatorUnitCd = ing.StrntDnmtrUomcd?.ToString(),
                    };

                    UpdateFormularyIngredientsDMDLookup(ingredient);

                    formularyHeader.FormularyIngredient.Add(ingredient);
                });
            }
        }

        private void PopulateFormularyExcipientsForImport(DMDDetailResultDTO res, FormularyHeader formularyHeader)
        {
            if (!_dmdAMPExcipientsForCodes.IsCollectionValid() || !_dmdAMPExcipientsForCodes.ContainsKey(formularyHeader.Code)) return;

            var excipients = _dmdAMPExcipientsForCodes[formularyHeader.Code];

            if (!excipients.IsCollectionValid()) return;



            excipients.Each(rec =>
            {
                UpdateFormularyExcipientDMDLookup(rec);

                rec.FormularyVersionId = formularyHeader.FormularyVersionId;
            });

            formularyHeader.FormularyExcipient = excipients;
        }


        private void PopulateFormularyRouteDetailsForImport(DMDDetailResultDTO res, FormularyHeader formularyHeader)
        {
            PopulateFormularyRouteDetailsForAMPs(res, formularyHeader);
        }

        private void PopulateFormularyRouteDetailsForAMPs(DMDDetailResultDTO res, FormularyHeader formularyHeader)
        {
            if (!_dmdAMPRouteMappings.IsCollectionValid() || !_dmdAMPRouteMappings.ContainsKey(formularyHeader.Code)) return;

            formularyHeader.FormularyRouteDetail = formularyHeader.FormularyRouteDetail ?? new List<FormularyRouteDetail>();

            var dmdRouteForAMPs = _dmdAMPRouteMappings[formularyHeader.Code];

            var ampRoutes = _mapper.Map<List<FormularyRouteDetail>>(dmdRouteForAMPs);

            if (!ampRoutes.IsCollectionValid()) return;

            ampRoutes.Each(routeDetail =>
            {
                routeDetail.FormularyVersionId = formularyHeader.FormularyVersionId;
                routeDetail.RouteFieldTypeCd = TerminologyConstants.ROUTEFIELDTYPE_NORMAL; //Normal
                routeDetail.Source = TerminologyConstants.DMD_DATA_SRC;

                UpdateFormularyRoutesDMDLookup(routeDetail);

                formularyHeader.FormularyRouteDetail.Add(routeDetail);
            });
        }

        private void PopulateFormularyAdditionalCodesForImport(DMDDetailResultDTO res, FormularyHeader formularyHeader)
        {
            if ((!_dmdAdditionalCodesForCodes.IsCollectionValid() || !_dmdAdditionalCodesForCodes.ContainsKey(formularyHeader.Code)) &&
                (!_therapeuticClassForCodes.IsCollectionValid() || !_therapeuticClassForCodes.ContainsKey(formularyHeader.Code))) return;

            var addnlCodes = new List<FormularyAdditionalCode>();

            if (_dmdAdditionalCodesForCodes.IsCollectionValid() && _dmdAdditionalCodesForCodes.ContainsKey(formularyHeader.Code))
            {
                var dmdAddnlCodes = _dmdAdditionalCodesForCodes[formularyHeader.Code];

                dmdAddnlCodes = dmdAddnlCodes?.Where(rec => rec.AdditionalCode.IsNotEmpty()).ToList();

                dmdAddnlCodes?.Each(rec => addnlCodes.Add(rec));
            }

            if (_therapeuticClassForCodes.IsCollectionValid() && _therapeuticClassForCodes.ContainsKey(formularyHeader.Code))
            {
                var theraupeticClassificationCodes = _therapeuticClassForCodes[formularyHeader.Code];
                foreach (var theraupeticClassificationCode in theraupeticClassificationCodes)
                {
                    if (theraupeticClassificationCode.Item1.IsNotEmpty())
                    {
                        addnlCodes.Add(new FormularyAdditionalCode
                        {
                            CodeType = TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE,
                            AdditionalCode = theraupeticClassificationCode.Item1,
                            AdditionalCodeDesc = theraupeticClassificationCode.Item2,
                            AdditionalCodeSystem = TerminologyConstants.FDB_DATA_SRC,
                            Source = TerminologyConstants.FDB_DATA_SRC
                        });
                    }
                }
            }

            addnlCodes.Each(rec =>
            {
                rec.FormularyVersionId = formularyHeader.FormularyVersionId;
            });

            formularyHeader.FormularyAdditionalCode = addnlCodes;
        }

        private void PopulateFormularyDetailForImport(DMDDetailResultDTO res, FormularyHeader formularyHeader)
        {
            formularyHeader.FormularyDetail = new List<FormularyDetail>();

            var formularyDetail = new FormularyDetail();

            formularyDetail.FormularyVersionId = formularyHeader.FormularyVersionId;
            formularyDetail.RnohFormularyStatuscd = _defaultFormularyStatusCode ?? TerminologyConstants.FORMULARYSTATUS_NONFORMULARY;

            if (res.BasisOfName != null)
                formularyDetail.BasisOfPreferredNameCd = res.BasisOfName.Cd?.ToString();

            formularyDetail.CfcFree = res.CfcF;
            formularyDetail.GlutenFree = res.GluF;
            formularyDetail.PreservativeFree = res.PresF;
            formularyDetail.SugarFree = res.SugF;
            formularyDetail.UnitDoseFormSize = res.Udfs;

            if (res.UnitDoseFormSizeUOM != null)
                formularyDetail.UnitDoseFormUnits = res.UnitDoseFormSizeUOM.Cd?.ToString();

            if (res.UnitDoseUOM != null)
                formularyDetail.UnitDoseUnitOfMeasureCd = res.UnitDoseUOM.Cd?.ToString();

            if (res.DoseForm != null)
                formularyDetail.DoseFormCd = res.DoseForm.Cd?.ToString();

            formularyDetail.EmaAdditionalMonitoring = res.Ema;

            if (res.LicensingAuthority != null)
            {
                var licensingAuth = res.LicensingAuthority.Cd?.ToString();
                formularyDetail.CurrentLicensingAuthorityCd = licensingAuth;
                formularyDetail.UnlicensedMedicationCd = licensingAuth.IsNotEmpty() && licensingAuth == "0" ? TerminologyConstants.STRINGIFIED_BOOL_TRUE : TerminologyConstants.STRINGIFIED_BOOL_FALSE;

                //For all the devices(2), the record status should be set to draft initially
                formularyHeader.RecStatusCode = licensingAuth.IsNotEmpty() && licensingAuth == "2" ? TerminologyConstants.RECORDSTATUS_DRAFT : formularyHeader.RecStatusCode;
            }

            formularyDetail.ParallelImport = res.ParallelImport;

            if (res.AvailableRestriction != null)
                formularyDetail.RestrictionsOnAvailabilityCd = res.AvailableRestriction.Cd?.ToString();

            if (res.ControlDrugCategory != null)
            {
                formularyDetail.ControlledDrugCategoryCd = res.ControlDrugCategory.Cd?.ToString();
                formularyDetail.ControlledDrugCategorySource = TerminologyConstants.DMD_DATA_SRC;
            }

            if (res.PrescribingStatus != null)
                formularyDetail.PrescribingStatusCd = res.PrescribingStatus.Cd?.ToString();

            if (res.SupplierCode != null)
            {
                formularyDetail.SupplierCd = res.SupplierCode?.ToString();
                //formularyDetail.SupplierName = _supplierCodeNames.ContainsKey(formularyDetail.SupplierCd) ? _supplierCodeNames[formularyDetail.SupplierCd] : null;
            }

            if (res.Form != null)
                formularyDetail.FormCd = res.Form.Cd?.ToString();

            if (formularyHeader.Code.IsNotEmpty() && _snomedTradeFamilyMappings.ContainsKey(formularyHeader.Code))
            {
                var tfDTO = _snomedTradeFamilyMappings[formularyHeader.Code];
                formularyDetail.TradeFamilyCd = tfDTO.TradeFamilyId;
                formularyDetail.TradeFamilyName = tfDTO.TradeFamilyTerm;
            }

            if (formularyHeader.Code.IsNotEmpty() && _snomedModifiedReleaseMappings.ContainsKey(formularyHeader.Code))
            {
                var tfDTO = _snomedModifiedReleaseMappings[formularyHeader.Code];
                formularyDetail.ModifiedReleaseCd = tfDTO.MrCd;
            }

            //This will be overridden by the rules later
            formularyDetail.Prescribable = true;
            formularyDetail.PrescribableSource = TerminologyConstants.DMD_DATA_SRC;

            UpdateFormularyDetailDMDLookup(formularyDetail);

            formularyHeader.FormularyDetail.Add(formularyDetail);

            //await AddFDBDetailsForProductTypeAndCode(formularyHeader);
            AddFDBDetailsForProductTypeAndCode(formularyHeader);
        }

        public void AddFDBDetailsForProductTypeAndCode(FormularyHeader recordToImport)
        {
            if (!recordToImport.FormularyDetail.IsCollectionValid()) return;

            recordToImport.FormularyDetail.Each(detail =>
            {
                detail.Caution = recordToImport.Code.SafeGetStringifiedCodeDescListForCode(_cautionsForCodes, TerminologyConstants.FDB_DATA_SRC);
                detail.SideEffect = recordToImport.Code.SafeGetStringifiedCodeDescListForCode(_sideEffectsForCodes, TerminologyConstants.FDB_DATA_SRC);
                detail.SafetyMessage = recordToImport.Code.SafeGetStringifiedCodeDescListForCode(_safetyMessagesForCodes, TerminologyConstants.FDB_DATA_SRC);
                detail.ContraIndication = recordToImport.Code.SafeGetStringifiedCodeDescListForCode(_contraIndicationsForCodes, TerminologyConstants.FDB_DATA_SRC);
                detail.LicensedUse = recordToImport.Code.SafeGetStringifiedCodeDescListForCode(_licensedUsesForCodes, TerminologyConstants.FDB_DATA_SRC);
                detail.UnlicensedUse = recordToImport.Code.SafeGetStringifiedCodeDescListForCode(_unLicensedUsesForCodes, TerminologyConstants.FDB_DATA_SRC);

                if (_highRiskFlagForCodes.IsCollectionValid() && _highRiskFlagForCodes.ContainsKey(recordToImport.Code))
                {
                    detail.HighAlertMedication = (_highRiskFlagForCodes[recordToImport.Code].HasValue && _highRiskFlagForCodes[recordToImport.Code].Value) ? "1" : null;
                    detail.HighAlertMedicationSource = (detail.HighAlertMedication == "1") ? TerminologyConstants.FDB_DATA_SRC : null;
                }

                if (_blackTriangleFlagForCodes.IsCollectionValid() && _blackTriangleFlagForCodes.ContainsKey(recordToImport.Code))
                {
                    detail.BlackTriangle = _blackTriangleFlagForCodes[recordToImport.Code] ? "1" : null;
                    detail.BlackTriangleSource = (detail.BlackTriangle == "1") ? TerminologyConstants.FDB_DATA_SRC : null;
                }
            });
        }

        private void ApplyRules(DMDDetailResultDTO dMDDetailResultDTO, FormularyHeader formularyHeader)
        {
            //Identity the product type and apply the rules
            IImportRule importRule = null;
            if (string.Compare(formularyHeader.ProductType, "vtm", true) == 0)
                importRule = new VTMImportRule(dMDDetailResultDTO, formularyHeader);
            else if (string.Compare(formularyHeader.ProductType, "vmp", true) == 0)
                importRule = new VMPImportRule(dMDDetailResultDTO, formularyHeader);
            else if (string.Compare(formularyHeader.ProductType, "amp", true) == 0)
                importRule = new AMPImportRule(dMDDetailResultDTO, formularyHeader);
            else
                importRule = new NullImportRule(dMDDetailResultDTO, formularyHeader);

            importRule.MutateByRules();
        }

        private void OverwriteFromExisting(DMDDetailResultDTO resDTO, FormularyHeader formularyHeader, List<FormularyHeader> existingLatestFormulariesForCodes, Dictionary<string, FormularyHeader>? existingLatestFormulariesUpdatables)
        //private void OverwriteFromExisting(DMDDetailResultDTO resDTO, FormularyHeader formularyHeader)
        {
            //Check if there is exisitng record
            //if an 'active' record for AMP - overwrite the custom properties from existing 'active' to the newly imported 'draft'
            //if already a 'draft' record exists - mark the old 'draft' as 'Archieved' and save the new imported as 'Draft'

            //MMC-477 - Not efficient but causing memory issue otherwise
            //var existingLatestFormulariesForCodes = GetExistingLatestFormulariesByCodes(new List<DMDDetailResultDTO>() { resDTO });

            if (!existingLatestFormulariesForCodes.IsCollectionValid()) return;

            //already verfied for latest
            var existingFormularies = existingLatestFormulariesForCodes.Where(rec => ((string.Compare(rec.ProductType, resDTO.LogicalLevel.GetDMDLevelCodeByLogicalLevel(), true) == 0) && (string.Compare(rec.Code, resDTO.Code, true) == 0 || string.Compare(rec.Code, resDTO.PrevCode, true) == 0)))?.ToList();
            //.FirstOrDefault();

            if (!existingFormularies.IsCollectionValid()) return;

            var existingLatestDraft = existingFormularies.FirstOrDefault(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT);
            var existingLatestActive = existingFormularies.FirstOrDefault(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE);
            var existingLatestArchive = existingFormularies.FirstOrDefault(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED);

            //But can be applied to AMP also - temp change - not to break any - need refactoring
            if ((string.Compare(formularyHeader.ProductType, "vtm", true) == 0) || (string.Compare(formularyHeader.ProductType, "vmp", true) == 0))
            {
                var existingLatestDrafts = existingFormularies.Where(rec => rec.Createdtimestamp != null && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?.OrderByDescending(rec => rec.Createdtimestamp).ToList();
                var existingLatestActives = existingFormularies.Where(rec => rec.Createdtimestamp != null && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)?.OrderByDescending(rec => rec.Createdtimestamp).ToList();
                var existingLatestArchives = existingFormularies.Where(rec => rec.Createdtimestamp != null && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ARCHIVED)?.OrderByDescending(rec => rec.Createdtimestamp).ToList();

                if (existingLatestDrafts.IsCollectionValid())
                    existingLatestDraft = existingLatestDrafts.FirstOrDefault();
                if (existingLatestActives.IsCollectionValid())
                    existingLatestActive = existingLatestActives.FirstOrDefault();
                if (existingLatestArchives.IsCollectionValid())
                    existingLatestArchive = existingLatestArchives.FirstOrDefault();
            }

            //merge from existing
            if (existingLatestActive != null)
            {
                ImportMergeHandler mergeHandler = null;
                if (string.Compare(formularyHeader.ProductType, "vtm", true) == 0)
                    mergeHandler = new VTMImportMergeHandler(_mapper, resDTO, formularyHeader, existingLatestActive);
                else if (string.Compare(formularyHeader.ProductType, "vmp", true) == 0)
                    mergeHandler = new VMPImportMergeHandler(_mapper, resDTO, formularyHeader, existingLatestActive);
                else if (string.Compare(formularyHeader.ProductType, "amp", true) == 0)
                    mergeHandler = new AMPImportMergeHandler(_mapper, resDTO, formularyHeader, existingLatestActive);
                else
                    mergeHandler = new NullImportMergeHandler(_mapper, resDTO, formularyHeader, existingLatestActive);

                mergeHandler.DMDLookupProvider = getDMDLookupProvider();
                mergeHandler.MergeFromExisting();

                //set the exising vtm and vmp active islatest to false
                if (string.Compare(formularyHeader.ProductType, "vmp", true) == 0 || string.Compare(formularyHeader.ProductType, "vtm", true) == 0)
                {
                    //existingLatestActive.IsLatest = false;
                    //_unitOfWork.FormularyHeaderFormularyRepository.Update(existingLatestActive);
                    //var recToUpdate = _unitOfWork.FormularyHeaderFormularyRepository.Items.Where(rec => rec.FormularyVersionId == existingLatestActive.FormularyVersionId).FirstOrDefault();

                    var recToUpdate = existingLatestFormulariesUpdatables != null && existingLatestFormulariesUpdatables.ContainsKey(existingLatestActive.FormularyVersionId) ? existingLatestFormulariesUpdatables[existingLatestActive.FormularyVersionId] : null;

                    //mmc-477 - formularyid fix - if code has changed - keep both as latest
                    //if (recToUpdate != null)
                    //{
                    //    recToUpdate.IsLatest = false;
                    //    _unitOfWork.FormularyHeaderFormularyRepository.Update(recToUpdate);
                    //}
                }
            }

            //mark previous as 'archived' --and check if it has any previous archived and if exists mark it as 'not latest' (change: Archived always latest)
            if (existingLatestDraft != null)
            {
                //var recToUpdate = _unitOfWork.FormularyHeaderFormularyRepository.Items.Where(rec => rec.FormularyVersionId == existingLatestDraft.FormularyVersionId).FirstOrDefault();

                #region MMC-477 - FormularyId changes

                //var recToUpdate = existingLatestFormulariesUpdatables != null && existingLatestFormulariesUpdatables.ContainsKey(existingLatestDraft.FormularyVersionId) ? existingLatestFormulariesUpdatables[existingLatestDraft.FormularyVersionId] : null;

                //if (recToUpdate != null)
                //{
                //    /*
                //    //the versionid of new draft should be taken from previous draft
                //    formularyHeader.VersionId = existingLatestDraft.VersionId == null ? 1 : existingLatestDraft.VersionId + 1;

                //    recToUpdate.IsLatest = true;
                //    recToUpdate.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;

                //    //the versionid of existing draft should be taken from previous archived if exists
                //    recToUpdate.VersionId = (existingLatestArchive == null || existingLatestArchive.VersionId == null) ? 1 : existingLatestArchive.VersionId + 1;
                //    */

                //    recToUpdate.IsLatest = false;
                //    _unitOfWork.FormularyHeaderFormularyRepository.Update(recToUpdate);

                //    //clone and create an archive of the old draft
                //    var tobeCloned = _unitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking(true).Where(rec => rec.FormularyVersionId == recToUpdate.FormularyVersionId).FirstOrDefault();

                //    if (tobeCloned != null)
                //    {
                //        //this increments the existing version during clone
                //        var cloned = _formularyUtil.CloneFormulary(tobeCloned);
                //        cloned.IsLatest = true;//archived always true
                //        cloned.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;
                //        _unitOfWork.FormularyHeaderFormularyRepository.Add(cloned);
                //    }
                //}
                #endregion MMC-477 - FormularyId changes

                /*
            //the versionid of new draft should be taken from previous draft
            formularyHeader.VersionId = existingLatestDraft.VersionId == null ? 1 : existingLatestDraft.VersionId + 1;

            existingLatestDraft.IsLatest = true;
            existingLatestDraft.RecStatusCode = TerminologyConstants.RECORDSTATUS_ARCHIVED;

            //the versionid of existing draft should be taken from previous archived if exists
            existingLatestDraft.VersionId = (existingLatestArchive == null || existingLatestArchive.VersionId == null) ? 1 : existingLatestArchive.VersionId + 1;

            _unitOfWork.FormularyHeaderFormularyRepository.Update(existingLatestDraft);

            //remove the latest flag for the previous archived records -- not required as archive can be many latest
            //if (existingLatestArchive != null)
            //{
            //    existingLatestArchive.IsLatest = false;
            //    _unitOfWork.FormularyHeaderFormularyRepository.Update(existingLatestArchive);
            //}
                */

            }
        }

        /*
        private void CopyDataFromActiveRecord(DMDDetailResultDTO res, FormularyHeader formularyHeader)
        {
            var formularyForCode = GetExistingFormulariesByCodes(new List<DMDDetailResultDTO>() { res });

            if (formularyForCode.Count == 0) return;

            var activeFormulary = formularyForCode.Where(x => x.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE).FirstOrDefault();

            if (activeFormulary.IsNotNull())
            {
                var activeFormularyDetail = activeFormulary.FormularyDetail.FirstOrDefault();

                var activeFormularyLocalRoutes = activeFormulary.FormularyLocalRouteDetail;

                var formularyDetail = formularyHeader.FormularyDetail.FirstOrDefault();

                formularyHeader.FormularyLocalRouteDetail = formularyHeader.FormularyLocalRouteDetail ?? new List<FormularyLocalRouteDetail>();
                formularyHeader.MetaInfoJson = activeFormulary.MetaInfoJson;

                //var formularyDetail = new FormularyDetail();

                formularyDetail.RnohFormularyStatuscd = activeFormularyDetail.RnohFormularyStatuscd;
                formularyDetail.LocalLicensedUse = activeFormularyDetail.LocalLicensedUse;
                formularyDetail.LocalUnlicensedUse = activeFormularyDetail.LocalUnlicensedUse;
                formularyDetail.RoundingFactorCd = activeFormularyDetail.RoundingFactorCd;
                formularyDetail.CustomWarning = activeFormularyDetail.CustomWarning;
                formularyDetail.Reminder = activeFormularyDetail.Reminder;
                formularyDetail.Endorsement = activeFormularyDetail.Endorsement;
                formularyDetail.MedusaPreparationInstructions = activeFormularyDetail.MedusaPreparationInstructions;
                formularyDetail.TitrationTypeCd = activeFormularyDetail.TitrationTypeCd;
                formularyDetail.Diluent = activeFormularyDetail.Diluent;
                formularyDetail.ClinicalTrialMedication = activeFormularyDetail.ClinicalTrialMedication;
                formularyDetail.CriticalDrug = activeFormularyDetail.CriticalDrug;
                formularyDetail.IsGastroResistant = activeFormularyDetail.IsGastroResistant;
                formularyDetail.IsModifiedRelease = activeFormularyDetail.IsModifiedRelease;
                formularyDetail.ExpensiveMedication = activeFormularyDetail.ExpensiveMedication;
                formularyDetail.HighAlertMedication = activeFormularyDetail.HighAlertMedication;
                formularyDetail.HighAlertMedicationSource = activeFormularyDetail.HighAlertMedicationSource;
                formularyDetail.IvToOral = activeFormularyDetail.IvToOral;
                formularyDetail.NotForPrn = activeFormularyDetail.NotForPrn;
                formularyDetail.IsBloodProduct = activeFormularyDetail.IsBloodProduct;
                formularyDetail.IsDiluent = activeFormularyDetail.IsDiluent;
                formularyDetail.Prescribable = activeFormularyDetail.Prescribable == null ? false : activeFormularyDetail.Prescribable;
                formularyDetail.PrescribableSource = activeFormularyDetail.PrescribableSource;
                formularyDetail.OutpatientMedicationCd = activeFormularyDetail.OutpatientMedicationCd;
                formularyDetail.IgnoreDuplicateWarnings = activeFormularyDetail.IgnoreDuplicateWarnings;
                formularyDetail.IsCustomControlledDrug = activeFormularyDetail.IsCustomControlledDrug;
                formularyDetail.IsPrescriptionPrintingRequired = activeFormularyDetail.IsPrescriptionPrintingRequired;
                formularyDetail.IsIndicationMandatory = activeFormularyDetail.IsIndicationMandatory;
                formularyDetail.WitnessingRequired = activeFormularyDetail.WitnessingRequired;

                //formularyHeader.FormularyDetail.Add(formularyDetail);

                activeFormularyLocalRoutes.Each(res =>
                {
                    var localRoute = new FormularyLocalRouteDetail();

                    localRoute.Createdby = res.Createdby;
                    localRoute.Createddate = DateTime.Now.ToUniversalTime();
                    localRoute.Createdtimestamp = DateTime.Now;
                    localRoute.FormularyVersionId = formularyDetail.FormularyVersionId;
                    localRoute.RouteCd = res.RouteCd;
                    localRoute.RouteDesc = res.RouteDesc;
                    localRoute.RouteFieldTypeCd = res.RouteFieldTypeCd;
                    localRoute.RowId = Guid.NewGuid().ToString();
                    localRoute.Source = res.Source;
                    localRoute.Updatedby = res.Updatedby;
                    localRoute.Updateddate = DateTime.Now.ToUniversalTime();
                    localRoute.Updatedtimestamp = DateTime.Now;

                    formularyHeader.FormularyLocalRouteDetail.Add(localRoute);
                });
            }
            else
            {
                return;
            }
        }
        */
    }
}
