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
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.Extensions;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Model.Search;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Queries
{
    public abstract class RuleBoundBaseFormularyBuilder
    {
        protected FormularyDTO _formularyDTO = new FormularyDTO();
        protected FormularyHeader _formularyDAO;
        protected IServiceProvider _provider;
        protected IMapper _mapper;
        protected IConfiguration _configuration;


        public List<FormularyHeader> DescendentFormularies { get; set; }
        public List<FormularyHeader> AncestorFormularies { get; set; }

        public FormularyDTO FormularyDTO { get { return _formularyDTO; } }

        protected ActiveFormularyBasicDTO _activeFormularyBasicDTO;

        public ActiveFormularyBasicDTO ActiveFormularyBasicDTO { get { return _activeFormularyBasicDTO; } }

        public RuleBoundBaseFormularyBuilder(IServiceProvider serviceProvider)
        {
            _provider = serviceProvider;
            _mapper = this._provider.GetService(typeof(IMapper)) as IMapper;
            _configuration = this._provider.GetService(typeof(IConfiguration)) as IConfiguration;

        }

        public virtual async Task CreateBase(FormularyHeader formularyDAO)
        {
            _formularyDAO = formularyDAO;

            _formularyDTO = this._mapper.Map<FormularyDTO>(formularyDAO);
        }

        public virtual async Task CreateBasicActiveFormularyBase(FormularyHeader formularyDAO)
        {
            _formularyDAO = formularyDAO;

            _activeFormularyBasicDTO = this._mapper.Map<ActiveFormularyBasicDTO>(formularyDAO);
        }

        public virtual void CreateDetails()
        {
            var formularyDetailObj = _formularyDAO.FormularyDetail.FirstOrDefault();

            if (formularyDetailObj.IsNotNull())
            {
                _formularyDTO.Detail = this._mapper.Map<FormularyDetailDTO>(formularyDetailObj);
            }
        }

        public virtual void CreateBasicActiveFormularyDetails()
        {
            var formularyDetailObj = _formularyDAO.FormularyDetail.FirstOrDefault();

            if (formularyDetailObj.IsNotNull())
            {
                _activeFormularyBasicDTO.Detail = this._mapper.Map<ActiveFormularyBasicDetailDTO>(formularyDetailObj);
            }
        }

        public virtual async Task CreateAdditionalCodes(bool getAllAddnlCodes = false)
        {
            if (_formularyDAO.FormularyAdditionalCode.IsCollectionValid())
            {
                var addlCodes = this._mapper.Map<List<FormularyAdditionalCodeDTO>>(_formularyDAO.FormularyAdditionalCode);
                _formularyDTO.FormularyAdditionalCodes = addlCodes;
                //_formularyDTO.AllFormularyAdditionalCodes = addlCodes;
            }

            if (string.Compare(_formularyDTO.ProductType, "amp", true) == 0)
            {
                var missingAddnlCodes = await GetMissingClassificationCodeSystems(_formularyDTO.FormularyAdditionalCodes);

                if (!_formularyDTO.FormularyAdditionalCodes.IsCollectionValid())
                    _formularyDTO.FormularyAdditionalCodes = new List<FormularyAdditionalCodeDTO>();

                if (missingAddnlCodes.IsCollectionValid())
                    _formularyDTO.FormularyAdditionalCodes.AddRange(missingAddnlCodes);

                if (!_formularyDTO.FormularyAdditionalCodes.IsCollectionValid()) return;

                //MMC-451: No need to overwrite since the user has option to select the default now
                //OverrideClassificationCodesByConfigValuesForAMP();
            }
        }

        protected virtual void OverrideClassificationCodesByConfigValuesForVMP()
        {
            var classCodes = _configuration.GetSection("Formulary_Rules:OverridableClassificationCodes").Get<List<FormularyAdditionalCodeDTO>>();

            if (!classCodes.IsCollectionValid()) return;

            var classCodesForCurrentCode = classCodes.Where(rec => rec.DmdCode == _formularyDTO.Code);

            if (!classCodesForCurrentCode.IsCollectionValid()) return;

            foreach (var item in _formularyDTO.FormularyAdditionalCodes)
            {
                //get from config if exists and overwrite it from config
                var classCodeForSystem = classCodesForCurrentCode.Where(rec => rec.AdditionalCodeSystem == item.AdditionalCodeSystem).FirstOrDefault();

                if (classCodeForSystem != null)
                {
                    item.AdditionalCodeDesc = classCodeForSystem.AdditionalCodeDesc;
                    item.AdditionalCode = classCodeForSystem.AdditionalCode;
                }
            }
        }

        protected virtual void OverrideClassificationCodesByConfigValuesForAMP()
        {
            var classCodes = _configuration.GetSection("Formulary_Rules:OverridableClassificationCodes").Get<List<FormularyAdditionalCodeDTO>>();

            if (!classCodes.IsCollectionValid()) return;

            var classCodesForCurrentCode = classCodes.Where(rec => rec.DmdCode == _formularyDTO.ParentCode);//check for parent code

            if (!classCodesForCurrentCode.IsCollectionValid()) return;

            foreach (var item in _formularyDTO.FormularyAdditionalCodes)
            {
                //get from config if exists and overwrite it from config
                var classCodeForSystem = classCodesForCurrentCode.Where(rec => rec.AdditionalCodeSystem == item.AdditionalCodeSystem).FirstOrDefault();

                if (classCodeForSystem != null)
                {
                    item.AdditionalCodeDesc = classCodeForSystem.AdditionalCodeDesc;
                    item.AdditionalCode = classCodeForSystem.AdditionalCode;
                }
            }
        }

        public virtual void CreateIngredients()
        {
            if (_formularyDAO.FormularyIngredient.IsCollectionValid())
            {
                _formularyDTO.FormularyIngredients = new List<FormularyIngredientDTO>();

                _formularyDAO.FormularyIngredient.Each(fi =>
                {
                    _formularyDTO.FormularyIngredients.Add(_mapper.Map<FormularyIngredientDTO>(fi));
                });
            }
        }

        public virtual void CreateExcipients()
        {
            if (_formularyDAO.FormularyExcipient.IsCollectionValid())
            {
                _formularyDTO.FormularyExcipients = new List<FormularyExcipientDTO>();

                _formularyDAO.FormularyExcipient.Each(fi =>
                {
                    _formularyDTO.FormularyExcipients.Add(_mapper.Map<FormularyExcipientDTO>(fi));
                });
            }
        }

        public virtual void CreateRouteDetails()
        {
            if (_formularyDAO.FormularyRouteDetail.IsCollectionValid())
            {
                _formularyDTO.FormularyRouteDetails = new List<FormularyRouteDetailDTO>();

                _formularyDAO.FormularyRouteDetail.Each(route =>
                {
                    _formularyDTO.FormularyRouteDetails.Add(_mapper.Map<FormularyRouteDetailDTO>(route));
                });
            }
        }

        public virtual void CreateLocalRouteDetails()
        {
            if (_formularyDAO.FormularyLocalRouteDetail.IsCollectionValid())
            {
                _formularyDTO.FormularyLocalRouteDetails = new List<FormularyLocalRouteDetailDTO>();

                _formularyDAO.FormularyLocalRouteDetail.Each(route =>
                {
                    _formularyDTO.FormularyLocalRouteDetails.Add(_mapper.Map<FormularyLocalRouteDetailDTO>(route));
                });
            }
        }

        protected Dictionary<string, string> GetDefaultClassificationCodesFromDB()
        {
            var defaultClassifictionCodes = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(_formularyDAO.MetaInfoJson))
            {
                var meta = JObject.Parse(_formularyDAO.MetaInfoJson);
                if (meta != null)
                {
                    var defClassificationCode = meta[TerminologyConstants.DEF_CLASS_CODES];
                    if (defClassificationCode != null)
                    {
                        var bnfCode = defClassificationCode[TerminologyConstants.DEF_BNF_CLASS_CODE];
                        var atcCode = defClassificationCode[TerminologyConstants.DEF_ATC_CLASS_CODES];
                        var fdbCode = defClassificationCode[TerminologyConstants.DEF_FDB_CLASS_CODES];

                        if (bnfCode != null && !string.IsNullOrEmpty(bnfCode.ToString()))
                            defaultClassifictionCodes[TerminologyConstants.DEF_BNF_CLASS_CODE] = bnfCode.ToString();
                        if (atcCode != null && !string.IsNullOrEmpty(atcCode.ToString()))
                            defaultClassifictionCodes[TerminologyConstants.DEF_ATC_CLASS_CODES] = atcCode.ToString();
                        if (fdbCode != null && !string.IsNullOrEmpty(fdbCode.ToString()))
                            defaultClassifictionCodes[TerminologyConstants.DEF_FDB_CLASS_CODES] = fdbCode.ToString();
                    }
                }
            }

            return defaultClassifictionCodes;
        }

        protected virtual void ProjectClassificationCodesFromChildNodes<T>(IEnumerable<FormularyAdditionalCodeDTO> childAdditionalCodesDTO, bool getAllAdditionalCodes, Action<FormularyAdditionalCodeDTO> onBeforeClassCodeSelection, T formularyDTO) where T : IComposeAdditionalCodes
        {
            if (!childAdditionalCodesDTO.IsCollectionValid()) return;

            //Project the classification codes from AMP to VTM
            //Only if each classification type has only one record
            var codeSystemLkp = new ConcurrentDictionary<string, SortedDictionary<string, FormularyAdditionalCodeDTO>>();

            var classificationTypeCodes = childAdditionalCodesDTO
                   .Where(rec => string.Compare(rec.CodeType, TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE, true) == 0)?
                   .Distinct()?.ToList();

            //Check whether each type has only one record
            if (classificationTypeCodes.IsCollectionValid())
            {
                classificationTypeCodes.Each(rec =>
                {
                    var cloned = _mapper.Map<FormularyAdditionalCodeDTO>(rec);

                    //if BNF consider only first 7 characters - MMC-455
                    var additionalCode = cloned.AdditionalCode ?? "";

                    if (string.Compare(cloned.AdditionalCodeSystem, "bnf", true) == 0)
                    {
                        cloned.AdditionalCode = cloned.AdditionalCode.IsNotEmpty() ? (cloned.AdditionalCode.Length > 7 ? cloned.AdditionalCode.Substring(0, 7) : cloned.AdditionalCode) : "";
                        additionalCode = cloned.AdditionalCode;
                    }

                    if (codeSystemLkp.ContainsKey(cloned.AdditionalCodeSystem))
                    {
                        if (!codeSystemLkp[cloned.AdditionalCodeSystem].ContainsKey(additionalCode))
                        {
                            codeSystemLkp[cloned.AdditionalCodeSystem].Add(additionalCode, cloned);
                        }
                    }
                    else
                    {
                        codeSystemLkp[cloned.AdditionalCodeSystem] = new SortedDictionary<string, FormularyAdditionalCodeDTO> { { additionalCode, cloned } };
                    }
                });
            }
            FillAdditionalCodes(codeSystemLkp, getAllAdditionalCodes, onBeforeClassCodeSelection, formularyDTO);
        }

        private void FillAdditionalCodes<T>(ConcurrentDictionary<string, SortedDictionary<string, FormularyAdditionalCodeDTO>> codeSystemLkp, bool getAllAdditionalCodes, Action<FormularyAdditionalCodeDTO> onBeforeClassCodeSelection, T formularyDTO) where T : IComposeAdditionalCodes
        {
            if (!codeSystemLkp.IsCollectionValid()) return;

            var defaultClassifictionCodes = GetDefaultClassificationCodesFromDB();

            foreach (var csItem in codeSystemLkp)
            {
                //Consider that code system only if it doesn not has same code for that classification code system
                if (csItem.Value.IsCollectionValid())
                {
                    //AddAllClassificationCodes(csItem.Value, defaultClassifictionCodes);

                    var key = $"default_{csItem.Key.ToLowerInvariant()}_classification_code";

                    //for FDB take the last one
                    //var codeData = string.Compare(csItem.Key, "fdb", true) == 0 ? csItem.Value.Last().Value : csItem.Value.First().Value;
                    //take last one for all - Joel MMC 455
                    //MMC-539
                    var codeData = csItem.Value.Last().Value;
                    if (csItem.Value.Count > 1)
                        codeData = csItem.Value.ElementAt(1).Value;

                    if (defaultClassifictionCodes.ContainsKey(key))
                    {
                        var defAddlCode = defaultClassifictionCodes[key];
                        //consider the primary if else take the very first one or last one in case of 'fdb'
                        codeData = csItem.Value.ContainsKey(defAddlCode) ? csItem.Value[defAddlCode] : codeData;
                    }

                    if (codeData != null)
                    {
                        codeData.IsDefault = true;

                        if (!getAllAdditionalCodes)
                        {
                            var cloned = _mapper.Map<FormularyAdditionalCodeDTO>(codeData);

                            onBeforeClassCodeSelection?.Invoke(cloned);

                            formularyDTO.FormularyAdditionalCodes.Add(cloned);
                        }
                    }
                    if (getAllAdditionalCodes)
                    {
                        var uniqueForCodeType = new HashSet<string>();
                        foreach (var itemVal in csItem.Value.Values)
                        {
                            var cloned = _mapper.Map<FormularyAdditionalCodeDTO>(itemVal);

                            onBeforeClassCodeSelection?.Invoke(cloned);

                            if (uniqueForCodeType.Contains(itemVal.AdditionalCode)) continue;

                            uniqueForCodeType.Add(itemVal.AdditionalCode);

                            formularyDTO.FormularyAdditionalCodes.Add(cloned);
                        }
                        //formularyDTO.FormularyAdditionalCodes.AddRange(csItem.Value.Values);
                    }

                }
            }
        }

        //public virtual void CreateOntologyForms()
        //{
        //    if (_formularyDAO.FormularyOntologyForm.IsCollectionValid())
        //    {
        //        _formularyDTO.FormularyOntologyForms = new List<FormularyOntologyFormDTO>();

        //        _formularyDAO.FormularyOntologyForm.Each(rec =>
        //        {
        //            _formularyDTO.FormularyOntologyForms.Add(_mapper.Map<FormularyOntologyFormDTO>(rec));
        //        });
        //    }
        //}

        //public virtual void CreateIndications() 
        //{
        //    if (_formularyDAO.FormularyIndication.IsCollectionValid())
        //    {
        //        _formularyDTO.FormularyIndications = new List<FormularyIndicationDTO>();

        //        _formularyDAO.FormularyIndication.Each(rec => 
        //            {
        //                _formularyDTO.FormularyIndications.Add(_mapper.Map<FormularyIndicationDTO>(rec));
        //            }
        //        );
        //    }
        //}

        public virtual async Task<List<FormularyHeader>> GetDescendentsForCodeUtil(List<string> codes, bool includeCurrentCodeLevel = false, bool activeOnly = true)
        {
            var basicSearchRepo = this._provider.GetService(typeof(IFormularyRepository<FormularyBasicSearchResultModel>)) as IFormularyRepository<FormularyBasicSearchResultModel>;

            var descendentsResults = await basicSearchRepo.GetFormularyDescendentForCodes(codes.ToArray(), true);

            if (!descendentsResults.IsCollectionValid()) return null;

            var descendentsResultsAsList = descendentsResults.ToList();

            IEnumerable<FormularyBasicSearchResultModel> descendents = descendentsResultsAsList;

            if (!includeCurrentCodeLevel)
                descendents = descendents.Where(rec => codes.Contains(rec.ParentCode));

            if (activeOnly)
                descendents = descendents.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE);

            var uniqueIds = descendents.Select(rec => rec.FormularyVersionId).Distinct().ToList();

            var repo = this._provider.GetService(typeof(IFormularyRepository<FormularyHeader>)) as IFormularyRepository<FormularyHeader>;

            List<FormularyHeader> descendentsDetails = repo.GetFormularyBasicDetailListForIds(uniqueIds)?.ToList();

            return descendentsDetails;
        }



        public virtual async Task HydrateLookupDescriptions(bool getForOnlyBasic = false)
        {
            //var timerDb = new Stopwatch();
            //timerDb.Start();

            //Log.Logger.Error("Info: HydrateLookupDescriptions started");

            var formularyQueries = this._provider.GetService(typeof(IFormularyQueries)) as IFormularyQueries;

            /*MMC-477
            var uomsLkp = await formularyQueries.GetLookup<DmdLookupUomDTO, string, string>(LookupType.DMDUOM, (rec) => rec.Cd, rec => rec.Desc);
            var doseFormsLkp = await formularyQueries.GetLookup<DmdLookupDrugformindDTO, string, string>(LookupType.DMDDoseForm, rec => rec.Cd.ToString(), rec => rec.Desc);
            var formsLkp = await formularyQueries.GetLookup<DmdLookupFormDTO, string, string>(LookupType.DMDForm, rec => rec.Cd, rec => rec.Desc);
            var strengthsLkp = await formularyQueries.GetLookup<DmdLookupBasisofstrengthDTO, string, string>(LookupType.DMDPharamceuticalStrength, rec => rec.Cd.ToString(), rec => rec.Desc);
            var ingredientsLkp = await formularyQueries.GetLookup<DmdLookupIngredientDTO, string, string>(LookupType.DMDIngredient, rec => rec.Isid.ToString(), rec => rec.Nm);
            var routesLkp = await formularyQueries.GetLookup<DmdLookupRouteDTO, string, string>(LookupType.DMDRoute, rec => rec.Cd, rec => rec.Desc);
            //var routesLkp = await formularyQueries.GetLookup<DmdLookupRouteDTO, string, string>(LookupType.TitrationType, rec => rec.Cd, rec => rec.Desc);
            */

            var titrationTypesLkp = new Dictionary<string, object>();

            var data = await formularyQueries.GetLookup<FormularyLookupItemDTO>(LookupType.TitrationType);
            var lookupData = data.Where(d => d.Type == LookupType.TitrationType.GetTypeName()).ToList();

            if (lookupData.IsCollectionValid())
                lookupData.Each(d => titrationTypesLkp[d.Cd] = d);

            if (getForOnlyBasic)
            {
                /*MMC-477*/
                HydrateLookupsInDetailForBasic(titrationTypesLkp);
            }
            else
            {
                /*MMC-477 - DMD Lookp not required since we store the description
                HydrateLookupsInDetail(uomsLkp, doseFormsLkp, formsLkp, titrationTypesLkp);

                //These not needed for basic right now and hence not considering
                HydrateLookupsInIngredients(uomsLkp, strengthsLkp, ingredientsLkp);
                await HydrateLookupsInRoutes(formularyQueries, routesLkp);
                await HydrateLookupsInLocalRoutes(formularyQueries, routesLkp);
                */
                var roundingFactorsAskp = await GetRoundingFactorsLkp(formularyQueries);

                HydrateLookupsInDetail(titrationTypesLkp, roundingFactorsAskp);

                //These not needed for basic right now and hence not considering
                await HydrateLookupsInRoutes(formularyQueries);
                await HydrateLookupsInLocalRoutes(formularyQueries);
            }


            //timerDb.Stop();
            //TimeSpan timeTakenDbCall = timerDb.Elapsed;
            //var msgDb = $"Info: HydrateLookupDescriptions completed. Time taken: {timeTakenDbCall.ToString(@"hh\:mm\:ss\.fff")}";
            //Log.Logger.Error(msgDb);

        }

        private async Task<Dictionary<string, string>> GetRoundingFactorsLkp(IFormularyQueries formularyQueries)
        {
            var roundingFactorsAskp = new Dictionary<string, string>();

            var roundingFactorLkpVals = await formularyQueries.GetLookup<FormularyLookupItemDTO>(LookupType.RoundingFactor);
            var roundingFactorLkpData = roundingFactorLkpVals?.Where(d => d.Type == LookupType.RoundingFactor.GetTypeName()).ToList();
            if (roundingFactorLkpData.IsCollectionValid())
                roundingFactorLkpData.Where(d => d.Recordstatus == 1)?.Each(rec => roundingFactorsAskp[rec.Cd] = rec.Desc);
            return roundingFactorsAskp;
        }

        private async Task HydrateLookupsInRoutes(IFormularyQueries formularyQueries)
        {
            var formularyDTO = _formularyDTO;
            if (formularyDTO.FormularyRouteDetails.IsCollectionValid())
            {
                var routeTypes = await formularyQueries.GetLookup<FormularyLookupItemDTO>(LookupType.RouteFieldType);
                var routeTypesLkp = new Dictionary<string, string>();

                routeTypesLkp = routeTypes.Where(d => d.Type == LookupType.RouteFieldType.GetTypeName())?
                                    .Select(rec => new
                                    {
                                        cd = rec.Cd,
                                        desc = rec.Desc
                                    })
                                    .ToDictionary(k => k.cd, d => d.desc);

                formularyDTO.FormularyRouteDetails.Each(route =>
                {
                    if (route.RouteFieldTypeCd.IsNotEmpty() && routeTypesLkp.ContainsKey(route.RouteFieldTypeCd))
                    {
                        route.RouteFieldTypeDesc = routeTypesLkp[route.RouteFieldTypeCd];
                    }
                });
            }
        }

        private async Task HydrateLookupsInLocalRoutes(IFormularyQueries formularyQueries)
        {
            var formularyDTO = _formularyDTO;

            if (formularyDTO.FormularyLocalRouteDetails.IsCollectionValid())
            {
                var routeTypes = await formularyQueries.GetLookup<FormularyLookupItemDTO>(LookupType.RouteFieldType);
                var routeTypesLkp = new Dictionary<string, string>();

                routeTypesLkp = routeTypes.Where(d => d.Type == LookupType.RouteFieldType.GetTypeName())?
                                        .Select(rec => new
                                        {
                                            cd = rec.Cd,
                                            desc = rec.Desc
                                        })
                                        .ToDictionary(k => k.cd, d => d.desc);

                formularyDTO.FormularyLocalRouteDetails.Each(route =>
                {
                    if (route.RouteFieldTypeCd.IsNotEmpty() && routeTypesLkp.ContainsKey(route.RouteFieldTypeCd))
                    {
                        route.RouteFieldTypeDesc = routeTypesLkp[route.RouteFieldTypeCd];
                    }
                });
            }
        }

        //MMC-477-Commenting
        //private void HydrateLookupsInIngredients(Dictionary<string, string> uomsLkp, Dictionary<string, string> strengthsLkp, Dictionary<string, string> ingredientsLkp)
        //{
        //    var formularyDTO = _formularyDTO;
        //    if (formularyDTO.FormularyIngredients.IsCollectionValid())
        //    {
        //        formularyDTO.FormularyIngredients.Each(ing =>
        //        {
        //            if (ing.BasisOfPharmaceuticalStrengthCd.IsNotEmpty() && strengthsLkp.ContainsKey(ing.BasisOfPharmaceuticalStrengthCd))
        //            {
        //                ing.BasisOfPharmaceuticalStrengthDesc = strengthsLkp[ing.BasisOfPharmaceuticalStrengthCd];
        //            }

        //            if (ing.IngredientCd.IsNotEmpty() && ing.IngredientName.IsEmpty() && ingredientsLkp.ContainsKey(ing.IngredientCd))
        //            {
        //                ing.IngredientName = ingredientsLkp[ing.IngredientCd];
        //            }

        //            if (ing.StrengthValueDenominatorUnitCd.IsNotEmpty() && uomsLkp.ContainsKey(ing.StrengthValueDenominatorUnitCd))
        //            {
        //                ing.StrengthValueDenominatorUnitDesc = uomsLkp[ing.StrengthValueDenominatorUnitCd];
        //            }
        //            if (ing.StrengthValueNumeratorUnitCd.IsNotEmpty() && uomsLkp.ContainsKey(ing.StrengthValueNumeratorUnitCd))
        //            {
        //                ing.StrengthValueNumeratorUnitDesc = uomsLkp[ing.StrengthValueNumeratorUnitCd];
        //            }
        //        });
        //    }
        //}

        private void HydrateLookupsInDetailForBasic(Dictionary<string, object> titrationTypesLkp)
        {
            //Description needed only for titration types for not need to extend
            if (_activeFormularyBasicDTO.Detail != null)
            {
                _activeFormularyBasicDTO.Detail.TitrationTypes?.Each(tit =>
                {
                    if (tit.Cd != null && titrationTypesLkp.ContainsKey(tit.Cd))
                    {
                        tit.Desc = ((FormularyLookupItemDTO)titrationTypesLkp[tit.Cd]).Desc;
                        tit.AdditionalProperties = ((FormularyLookupItemDTO)titrationTypesLkp[tit.Cd]).AdditionalProperties;
                    }
                });
            }
        }

        /*MMC-477--Commenting*/
        //private void HydrateLookupsInDetail(Dictionary<string, string> uomsLkp, Dictionary<string, string> doseFormsLkp, Dictionary<string, string> formsLkp,
        //    Dictionary<string, object> titrationTypesLkp)
        //{

        //    var formularyDTO = _formularyDTO;
        //    if (formularyDTO.Detail != null)
        //    {
        //        if (formularyDTO.Detail.DoseFormCd.IsNotEmpty() && doseFormsLkp.ContainsKey(formularyDTO.Detail.DoseFormCd))
        //        {
        //            formularyDTO.Detail.DoseFormDesc = doseFormsLkp[formularyDTO.Detail.DoseFormCd];
        //        }

        //        if (formularyDTO.Detail.UnitDoseFormUnits.IsNotEmpty() && uomsLkp.ContainsKey(formularyDTO.Detail.UnitDoseFormUnits))
        //        {
        //            formularyDTO.Detail.UnitDoseFormUnitsDesc = uomsLkp[formularyDTO.Detail.UnitDoseFormUnits];
        //        }

        //        if (formularyDTO.Detail.UnitDoseUnitOfMeasureCd.IsNotEmpty() && uomsLkp.ContainsKey(formularyDTO.Detail.UnitDoseUnitOfMeasureCd))
        //        {
        //            formularyDTO.Detail.UnitDoseUnitOfMeasureDesc = uomsLkp[formularyDTO.Detail.UnitDoseUnitOfMeasureCd];
        //        }

        //        if (formularyDTO.Detail.FormCd.IsNotEmpty() && formsLkp.ContainsKey(formularyDTO.Detail.FormCd))
        //        {
        //            formularyDTO.Detail.FormDesc = formsLkp[formularyDTO.Detail.FormCd];
        //        }

        //        formularyDTO.Detail.ChildFormulations?.Each(rec =>
        //        {
        //            if (rec != null && rec.Cd.IsNotEmpty())
        //                rec.Desc = formsLkp[rec.Cd];
        //        });

        //        formularyDTO.Detail.TitrationTypes?.Each(tit =>
        //        {
        //            if (tit.Cd != null && titrationTypesLkp.ContainsKey(tit.Cd))
        //            {
        //                tit.Desc = ((FormularyLookupItemDTO)titrationTypesLkp[tit.Cd]).Desc;
        //                tit.AdditionalProperties = ((FormularyLookupItemDTO)titrationTypesLkp[tit.Cd]).AdditionalProperties;
        //            }
        //        });

        //    }
        //}
        private void HydrateLookupsInDetail(Dictionary<string, object> titrationTypesLkp, Dictionary<string, string> roundingFactorsAskp)
        {
            var formularyDTO = _formularyDTO;
            if (formularyDTO.Detail != null)
            {
                formularyDTO.Detail.TitrationTypes?.Each(tit =>
                {
                    if (tit.Cd != null && titrationTypesLkp.ContainsKey(tit.Cd))
                    {
                        tit.Desc = ((FormularyLookupItemDTO)titrationTypesLkp[tit.Cd]).Desc;
                        tit.AdditionalProperties = ((FormularyLookupItemDTO)titrationTypesLkp[tit.Cd]).AdditionalProperties;
                    }
                });

                /* MMC-621 - reverting for now - to be uncommented later
                var hasRoundingDesc = formularyDTO.Detail.RoundingFactorCd != null && roundingFactorsAskp.IsCollectionValid() && roundingFactorsAskp.ContainsKey(formularyDTO.Detail.RoundingFactorCd);
                formularyDTO.Detail.RoundingFactorDesc = hasRoundingDesc ? roundingFactorsAskp[formularyDTO.Detail.RoundingFactorCd] : null;
                */
            }
        }

        protected async Task<List<FormularyAdditionalCodeDTO>> GetMissingClassificationCodeSystems(List<FormularyAdditionalCodeDTO> formularyAddnlCodesDTOs)
        {
            var missingAddnlCodes = new List<FormularyAdditionalCodeDTO>();

            var missingClassificationTypes = CheckForMissingClassificationSystemCodes(formularyAddnlCodesDTOs);

            if (!missingClassificationTypes.IsCollectionValid()) return missingAddnlCodes;

            //Get data from sibling 
            var descendentsHeader = await GetDescendentsForCodeUtil(new List<string> { _formularyDAO.ParentCode });

            if (!descendentsHeader.IsCollectionValid()) return missingAddnlCodes;

            //take all additional codes from the decendents except from this code
            var otherUniqueSiblingAddnlCodes = descendentsHeader
                .Where(rec => rec.IsLatest == true && rec.Code != _formularyDAO.Code)
                ?.SelectMany(rec => rec.FormularyAdditionalCode)
                ?.Where(rec => string.Compare(rec.CodeType, TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE, true) == 0)
                ?.ToList();

            if (!otherUniqueSiblingAddnlCodes.IsCollectionValid()) return missingAddnlCodes;


            foreach (var missingType in missingClassificationTypes)
            {
                var siblingAddnlCodesOfMissingType = otherUniqueSiblingAddnlCodes
                    .Where(rec => string.Compare(rec.AdditionalCodeSystem, missingType, true) == 0)
                    ?.Distinct(rec => rec.AdditionalCode)
                    ?.OrderByDescending(rec => rec.AdditionalCode)
                    ?.ToList();

                if (!siblingAddnlCodesOfMissingType.IsCollectionValid()) continue;

                var dtos = _mapper.Map<List<FormularyAdditionalCodeDTO>>(siblingAddnlCodesOfMissingType);
                //formularyDTO.FormularyAdditionalCodes.Add(dtos.First());
                //missingAddnlCodes.Add(dtos.First());
                dtos.Each(d => d.IsFromSibling = true);//MMC-477 - to identify it is from sibling
                missingAddnlCodes.AddRange(dtos);//Take all codes for MMC-451
            }

            return missingAddnlCodes;
        }

        private HashSet<string> CheckForMissingClassificationSystemCodes(List<FormularyAdditionalCodeDTO> formularyAddnlCodesDTOs)
        {
            var intendedPresenceOfCodeSystems = new HashSet<string> { "atc", "fdb", "bnf" };

            if (!formularyAddnlCodesDTOs.IsCollectionValid()) return intendedPresenceOfCodeSystems;

            var uniqueClassCodeSystems = new HashSet<string>();

            uniqueClassCodeSystems = formularyAddnlCodesDTOs.Where(rec => string.Compare(rec.CodeType, TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE, true) == 0).Select(rec => rec.AdditionalCodeSystem)?
                            .Select(rec => rec.ToLower())?.Distinct()?.ToHashSet();

            if (!uniqueClassCodeSystems.IsCollectionValid()) return intendedPresenceOfCodeSystems;

            var exceptUniques = intendedPresenceOfCodeSystems.Except(uniqueClassCodeSystems)?.Distinct()?.ToHashSet();

            if (exceptUniques.IsCollectionValid())
            {
                exceptUniques.Remove("customgroup");
                return exceptUniques;
            }

            return null;
        }
    }
}
