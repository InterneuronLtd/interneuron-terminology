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
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Model;
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;
using Interneuron.Terminology.BackgroundTaskService.Repository;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers
{
    /// <summary>
    /// This class identifies the delta between existing and new DMD and persists the differences
    /// </summary>
    public class DeltaIdentificationHandler
    {
        private IServiceProvider _serviceProvider;
        private readonly IMapper _mapper;
        private Dictionary<string, string> _propertyCategoryLkp;
        private ComparisonConfig _productPropertiesRootConfig;
        private List<string> _testCodes;
        private Dictionary<string, string> _propertyNameLkp;

        public DeltaIdentificationHandler(IServiceProvider serviceProvider, IMapper mapper)
        {
            _serviceProvider = serviceProvider;
            _mapper = mapper;
            PreparePopertyNameLkp();
            PreparePopertyCategoryLkp();
            PrepareConfiguration();
        }

        private void PrepareConfiguration()
        {
            _productPropertiesRootConfig = GetHeaderConfig();
        }

        private void PreparePopertyCategoryLkp()
        {
            _propertyCategoryLkp = new Dictionary<string, string>();
            _propertyCategoryLkp.Add("DETAIL", "Detail");
            _propertyCategoryLkp.Add("ADDITIONALCODES", "Detail");

            _propertyCategoryLkp.Add("INGREDIENTS", "Posology");
            _propertyCategoryLkp.Add("EXCIPIENTS", "Posology");
            _propertyCategoryLkp.Add("ROUTES", "Posology");
            _propertyCategoryLkp.Add("LOCALROUTES", "Posology");
            _propertyCategoryLkp.Add("FORMULARYADDITIONALCODE", "Detail");
            _propertyCategoryLkp.Add("FORMULARYINGREDIENT", "Posology");
            _propertyCategoryLkp.Add("FORMULARYROUTEDETAIL", "Posology");
            _propertyCategoryLkp.Add("FORMULARYLOCALROUTEDETAIL", "Posology");
            _propertyCategoryLkp.Add("FORMULARYEXCIPIENT", "Posology");

            _propertyCategoryLkp.Add("NAME", "Detail");
            _propertyCategoryLkp.Add("PARENTCODE", "Detail");
            _propertyCategoryLkp.Add("PREVCODE", "Detail");
            _propertyCategoryLkp.Add("VTMID", "Detail");
            _propertyCategoryLkp.Add("VMPID", "Detail");
            _propertyCategoryLkp.Add("ISDMDINVALID", "Detail");
            _propertyCategoryLkp.Add("ISDMDDELETED", "Detail");
            _propertyCategoryLkp.Add("BASISOFPREFERREDNAMECD", "Detail");
            _propertyCategoryLkp.Add("BASISOFPREFERREDNAMEDESC", "Detail");
            _propertyCategoryLkp.Add("CURRENTLICENSINGAUTHORITYCD", "Detail");
            _propertyCategoryLkp.Add("CURRENTLICENSINGAUTHORITYDESC", "Detail");
            _propertyCategoryLkp.Add("LOCALLICENSEDUSES", "Detail");
            _propertyCategoryLkp.Add("LOCALUNLICENSEDUSES", "Detail");
            _propertyCategoryLkp.Add("SUPPLIERCD", "Detail");
            _propertyCategoryLkp.Add("SUPPLIERNAME", "Detail");
            _propertyCategoryLkp.Add("DOSEFORMCD", "Posology");
            _propertyCategoryLkp.Add("DOSEFORMDESC", "Posology");
            _propertyCategoryLkp.Add("FORMCD", "Posology");
            _propertyCategoryLkp.Add("FORMDESC", "Posology");
            _propertyCategoryLkp.Add("UNITDOSEFORMSIZE", "Posology");
            _propertyCategoryLkp.Add("UNITDOSEFORMUNITS", "Posology");
            _propertyCategoryLkp.Add("UNITDOSEFORMUNITSDESC", "Posology");
            _propertyCategoryLkp.Add("UNITDOSEUNITOFMEASURECD", "Posology");
            _propertyCategoryLkp.Add("UNITDOSEUNITOFMEASUREDESC", "Posology");
            _propertyCategoryLkp.Add("CONTROLLEDDRUGCATEGORYCD", "Guidance");
            _propertyCategoryLkp.Add("CONTROLLEDDRUGCATEGORYDESC", "Guidance");
            _propertyCategoryLkp.Add("RESTRICTIONSONAVAILABILITYCD", "Guidance");
            _propertyCategoryLkp.Add("RESTRICTIONSONAVAILABILITYDESC", "Guidance");
            _propertyCategoryLkp.Add("PRESCRIBINGSTATUSCD", "Guidance");
            _propertyCategoryLkp.Add("PRESCRIBINGSTATUSDESC", "Guidance");
            _propertyCategoryLkp.Add("EMAADDITIONALMONITORING", "Flags");
            _propertyCategoryLkp.Add("PRESCRIBABLE", "Flags");
            _propertyCategoryLkp.Add("SUGARFREE", "Flags");
            _propertyCategoryLkp.Add("GLUTENFREE", "Flags");
            _propertyCategoryLkp.Add("PRESERVATIVEFREE", "Flags");
            _propertyCategoryLkp.Add("CFCFREE", "Flags");
            _propertyCategoryLkp.Add("UNLICENSEDMEDICATIONCD", "Flags");
            _propertyCategoryLkp.Add("PARALLELIMPORT", "Flags");
        }

        private void PreparePopertyNameLkp()
        {
            _propertyNameLkp = new Dictionary<string, string>();
            _propertyNameLkp.Add("ADDITIONALCODES", "Detail");

            _propertyNameLkp.Add("INGREDIENTS", "INGREDIENT");
            _propertyNameLkp.Add("EXCIPIENTS", "EXCIPIENT");
            _propertyNameLkp.Add("ROUTES", "ROUTE");
            _propertyNameLkp.Add("LOCALROUTES", "LOCAL ROUTE");
            _propertyNameLkp.Add("FORMULARYADDITIONALCODE", "ADDITIONAL CODE");
            _propertyNameLkp.Add("FORMULARYINGREDIENT", "INGREDIENT");
            _propertyNameLkp.Add("FORMULARYROUTEDETAIL", "ROUTE");
            _propertyNameLkp.Add("FORMULARYLOCALROUTEDETAIL", "LOCAL ROUTE");
            _propertyNameLkp.Add("FORMULARYEXCIPIENT", "EXCIPIENT");

            _propertyNameLkp.Add("NAME", "NAME");
            _propertyNameLkp.Add("PARENTCODE", "PARENT CODE");
            _propertyNameLkp.Add("PREVCODE", "PREV CODE");
            _propertyNameLkp.Add("VTMID", "VTM ID");
            _propertyNameLkp.Add("VMPID", "VMP ID");
            _propertyNameLkp.Add("ISDMDINVALID", "INVALID");
            _propertyNameLkp.Add("ISDMDDELETED", "DELETED");
            _propertyNameLkp.Add("BASISOFPREFERREDNAMECD", "BASIS OF PREFERRED NAME CD");
            _propertyNameLkp.Add("BASISOFPREFERREDNAMEDESC", "BASIS OF PREFERRED NAME");
            _propertyNameLkp.Add("CURRENTLICENSINGAUTHORITYCD", "CURRENT LICENSING AUTHORITY CD");
            _propertyNameLkp.Add("CURRENTLICENSINGAUTHORITYDESC", "CURRENT LICENSING AUTHORITY");
            _propertyNameLkp.Add("LOCALLICENSEDUSES", "LOCAL LICENSED USES");
            _propertyNameLkp.Add("LOCALUNLICENSEDUSES", "LOCAL UNLICENSED USES");
            _propertyNameLkp.Add("SUPPLIERCD", "SUPPLIER CD");
            _propertyNameLkp.Add("SUPPLIERNAME", "SUPPLIER");
            _propertyNameLkp.Add("DOSEFORMCD", "DOSE FORM CD");
            _propertyNameLkp.Add("DOSEFORMDESC", "DOSE FORM");
            _propertyNameLkp.Add("FORMCD", "FORM CD");
            _propertyNameLkp.Add("FORMDESC", "FORM");
            _propertyNameLkp.Add("UNITDOSEFORMSIZE", "UNIT DOSE FORM SIZE");
            _propertyNameLkp.Add("UNITDOSEFORMUNITS", "UNIT DOSE FORM UNITS");
            _propertyNameLkp.Add("UNITDOSEFORMUNITSDESC", "UNIT DOSE FORM UNITS");
            _propertyNameLkp.Add("UNITDOSEUNITOFMEASURECD", "UNIT DOSEUNIT OF MEASURE CD");
            _propertyNameLkp.Add("UNITDOSEUNITOFMEASUREDESC", "UNIT DOSE UNIT OF MEASURE");
            _propertyNameLkp.Add("CONTROLLEDDRUGCATEGORYCD", "CONTROLLED DRUG CATEGORY CD");
            _propertyNameLkp.Add("CONTROLLEDDRUGCATEGORYDESC", "CONTROLLED DRUG CATEGORY");
            _propertyNameLkp.Add("RESTRICTIONSONAVAILABILITYCD", "RESTRICTIONS ON AVAILABILITY CD");
            _propertyNameLkp.Add("RESTRICTIONSONAVAILABILITYDESC", "RESTRICTIONS ON AVAILABILITY");
            _propertyNameLkp.Add("PRESCRIBINGSTATUSCD", "PRESCRIBING STATUS CD");
            _propertyNameLkp.Add("PRESCRIBINGSTATUSDESC", "PRESCRIBING STATUS");
            _propertyNameLkp.Add("EMAADDITIONALMONITORING", "EMA ADDITIONAL MONITORING");
            _propertyNameLkp.Add("PRESCRIBABLE", "PRESCRIBABLE");
            _propertyNameLkp.Add("SUGARFREE", "SUGAR FREE");
            _propertyNameLkp.Add("GLUTENFREE", "GLUTEN FREE");
            _propertyNameLkp.Add("PRESERVATIVEFREE", "PRESERVATIVE FREE");
            _propertyNameLkp.Add("CFCFREE", "CFC FREE");
            _propertyNameLkp.Add("UNLICENSEDMEDICATIONCD", "UNLICENSEDMEDICATION CD");
            _propertyNameLkp.Add("PARALLELIMPORT", "PARALLEL IMPORT");
        }

        private string GetDisplayableName(string? name)
        {
            if (name.IsEmpty()) return string.Empty;
            if (!_propertyNameLkp.ContainsKey(name.ToUpper())) return name;
            return _propertyNameLkp[name.ToUpper()];
        }

        public async Task PersistDeltas(List<string> testCodes = null)
        {
            _testCodes = testCodes;
            //Pull all the DMD records that are both in 'Active' and 'Draft', compare it and persist the differences in DM+D properties

            var (scopeA, unitOfWorkA) = GetUoWInNewScope();// _serviceProvider.GetService<IUnitOfWork>();

            var repo = unitOfWorkA.FormularyHeaderFormularyRepository;

            await repo.TruncateFormularyChangeLog();

            var ampCodes = _testCodes?.ToArray();

            if (!_testCodes.IsCollectionValid())
            {
                var ampCodesWithStatus = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && rec.ProductType == "AMP" && (rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT || rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE))?
                    .Select(c => new { c.Code, c.RecStatusCode })
                    .AsEnumerable()
                    .Select(rec => (rec.Code, rec.RecStatusCode))
                    .ToList();

                var ampCodesAsList = GetRecordsHavingActiveAndDraft(ampCodesWithStatus);
                ampCodes = ampCodesAsList?.ToArray();
            }

            DisposeUoWWithScope(scopeA, unitOfWorkA);

            if (!ampCodes.IsCollectionValid()) return;

            var batchsize = 50;

            var batchedRequests = new List<string[]>();

            for (var reqIndex = 0; reqIndex < ampCodes.Length; reqIndex += batchsize)
            {
                //var batches = ampCodes.AsSpan().Slice(reqIndex, batchsize);
                var batches = ampCodes.Skip(reqIndex).Take(batchsize);
                batchedRequests.Add(batches.ToArray());
            }

            await ProcessDelta(batchedRequests);

            var (scopeB, unitOfWorkB) = GetUoWInNewScope();// _serviceProvider.GetService<IUnitOfWork>();

            await unitOfWorkB.FormularyChangeLogFormularyRepository.RefreshFormularyChangeLogMaterializedView();

            DisposeUoWWithScope(scopeB, unitOfWorkB);
        }

        private async Task ProcessDelta(List<string[]> batchedRequests)
        {
            if (!batchedRequests.IsCollectionValid()) return;

            //Parallel.ForEach(batchedRequests, new ParallelOptions() { MaxDegreeOfParallelism = 3 }, async (req) =>
            //{
            foreach (var req in batchedRequests)
            {
                var (scope, savingUnitOfWork) = GetUoWInNewScope();// _serviceProvider.GetService<IUnitOfWork>();

                //var ampsInDraftOrActive = savingUnitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesByCodes(req)?.ToList();
                var ampsInDraftOrActiveQry = savingUnitOfWork.FormularyHeaderFormularyRepository.GetLatestFormulariesAsQueryableWithNoTracking()
                    .Where(rec => rec.IsLatest == true && rec.ProductType == "AMP" && req.Contains(rec.Code) && (rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT || rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE));


                var ampsInDraftOrActive = ampsInDraftOrActiveQry.ToList();

                DisposeUoWWithScope(scope, savingUnitOfWork);

                if (!ampsInDraftOrActive.IsCollectionValid()) continue;

                var (hasBothDraftAndActive, drafts, actives) = GetRecordsHavingActiveAndDraft(ampsInDraftOrActive);

                if (!hasBothDraftAndActive.IsCollectionValid()) continue;

                var list = GetRecordsToCompareAsLkp();

                List<(DMDComparableProductDetail Active, DMDComparableProductDetail Draft)> GetRecordsToCompareAsLkp()
                {
                    var coll = new List<(DMDComparableProductDetail Active, DMDComparableProductDetail Draft)>();

                    foreach (var rec in CollectionsMarshal.AsSpan(hasBothDraftAndActive.Distinct().ToList()))
                    {
                        var headerDrafts = drafts[rec];
                        var headerActive = actives[rec];

                        foreach (var headerDraft in headerDrafts)
                        {
                            var draftComparableDetail = BuildComparableDMD(headerDraft);
                            var activeComparableDetail = BuildComparableDMD(headerActive);
                            coll.Add((Active: activeComparableDetail, Draft: draftComparableDetail));
                        }
                    }

                    return coll;
                }

                var deltas = new ConcurrentBag<FormularyChangeLogDTO>();

                //dict.Keys.AsParallel().Each(k =>
                //foreach (var k in dict.Keys)
                foreach (var rec in list)
                {
                    //var delta = AssignDelta(dict[k]);
                    var delta = AssignDelta(rec);
                    if (delta != null)
                        deltas.Add(delta);
                }
                //});

                if (!deltas.IsCollectionValid()) continue;

                var deltaLogs = _mapper.Map<FormularyChangeLog[]>(deltas.ToArray());

                var (scopeA, savingUnitOfWorkA) = GetUoWInNewScope();// _serviceProvider.GetService<IUnitOfWork>();

                savingUnitOfWorkA.FormularyChangeLogFormularyRepository.AddRange(deltaLogs);

                await savingUnitOfWorkA.SaveAsync();

                DisposeUoWWithScope(scopeA, savingUnitOfWorkA);

                if (deltaLogs != null) deltaLogs = null;
                //});
            }
        }

        private DMDComparableProductDetail? BuildComparableDMD(FormularyHeader headerDraft)
        {
            if (headerDraft == null) return null;
            var comparableDetail = _mapper.Map<DMDComparableProductDetail>(headerDraft.FormularyDetail.First());
            comparableDetail.FormularyAdditionalCode = _mapper.Map<List<DMDFormularyAdditionalCode>>(headerDraft.FormularyAdditionalCode);
            comparableDetail.FormularyIngredient = _mapper.Map<List<DMDFormularyIngredient>>(headerDraft.FormularyIngredient);
            comparableDetail.FormularyRouteDetail = _mapper.Map<List<DMDFormularyRouteDetail>>(headerDraft.FormularyRouteDetail);
            comparableDetail.FormularyLocalRouteDetail = _mapper.Map<List<DMDFormularyRouteDetail>>(headerDraft.FormularyLocalRouteDetail);
            comparableDetail.FormularyExcipient = _mapper.Map<List<DMDFormularyExcipient>>(headerDraft.FormularyExcipient);

            comparableDetail.ParentCode = headerDraft.ParentCode;
            comparableDetail.Name = headerDraft.Name;
            comparableDetail.Code = headerDraft.Code;
            comparableDetail.FormularyId = headerDraft.FormularyId;
            comparableDetail.ProductType = headerDraft.ProductType;
            comparableDetail.VmpId = headerDraft.VmpId;
            comparableDetail.VtmId = headerDraft.VtmId;
            comparableDetail.IsDmdInvalid = headerDraft.IsDmdInvalid;
            comparableDetail.IsDmdDeleted = headerDraft.IsDmdDeleted;

            return comparableDetail;
        }

        private FormularyChangeLogDTO? AssignDelta((DMDComparableProductDetail Active, DMDComparableProductDetail Draft) currentRecord)
        {
            var delta = new FormularyChangeLogDTO
            {
                Code = currentRecord.Draft.Code,
                Name = currentRecord.Draft.Name,
                FormularyId = currentRecord.Draft.FormularyId,
                ParentCode = currentRecord.Draft.ParentCode,
                ProductType = currentRecord.Draft.ProductType,
                EntitiesCompared = JsonConvert.SerializeObject(new { Active = currentRecord.Active, Draft = currentRecord.Draft }, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
            };

            var prodPropertiesComparer = new CompareLogic(_productPropertiesRootConfig);
            var result = prodPropertiesComparer.Compare(currentRecord.Active, currentRecord.Draft);
            var diffs = result.Differences;

            if (!diffs.IsCollectionValid()) return null;

            CaptureDelta(delta, diffs);

            return delta;
        }

        private void CaptureDelta(FormularyChangeLogDTO delta, List<Difference> diffs)
        {
            if (!diffs.IsCollectionValid()) return;

            var draftChanges = new Dictionary<string, Dictionary<string, dynamic>>();
            var activeChanges = new Dictionary<string, Dictionary<string, dynamic>>();

            var draftAdditionalCodeChanges = new List<DMDFormularyAdditionalCode>();
            var activeAdditionalCodeChanges = new List<DMDFormularyAdditionalCode>();

            var draftLocallicensedUseChanges = new List<DMDFormularyLookupItem>();
            var activeLocallicensedUseChanges = new List<DMDFormularyLookupItem>();

            var draftLocalUnlicensedUseChanges = new List<DMDFormularyLookupItem>();
            var activeLocalUnlicensedUseChanges = new List<DMDFormularyLookupItem>();

            var draftIngredientChanges = new List<DMDFormularyIngredient>();
            var activeIngredientChanges = new List<DMDFormularyIngredient>();
            var draftRouteDetailChanges = new List<DMDFormularyRouteDetail>();
            var activeRouteDetailChanges = new List<DMDFormularyRouteDetail>();
            var draftLocalRouteDetailChanges = new List<DMDFormularyRouteDetail>();
            var activeLocalRouteDetailChanges = new List<DMDFormularyRouteDetail>();
            var draftExcipientChanges = new List<DMDFormularyExcipient>();
            var activeExcipientChanges = new List<DMDFormularyExcipient>();

            foreach (var diff in diffs)
            {
                if (string.Compare(diff.ChildPropertyName, "count", true) == 0) continue;
                var propNameInLkp = _propertyCategoryLkp.ContainsKey(diff.PropertyName.ToUpper());//individual fields
                var propParentNameInLkp = _propertyCategoryLkp.ContainsKey(diff.ParentPropertyName.ToUpper());//lists
                var categoryName = propNameInLkp ? _propertyCategoryLkp[diff.PropertyName.ToUpper()] : (propParentNameInLkp ? _propertyCategoryLkp[diff.ParentPropertyName.ToUpper()] : null);

                var matchedPropName = propNameInLkp ? diff.PropertyName : (propParentNameInLkp ? diff.ParentPropertyName : null);

                if (categoryName == null)
                    continue;

                if (!draftChanges.ContainsKey(categoryName))
                    draftChanges[categoryName] = new Dictionary<string, dynamic>();
                if (!activeChanges.ContainsKey(categoryName))
                    activeChanges[categoryName] = new Dictionary<string, dynamic>();

                var activeVal = propNameInLkp ? diff.Object1Value : (propParentNameInLkp ? diff.Object1 : null);

                var draftVal = propNameInLkp ? diff.Object2Value : (propParentNameInLkp ? diff.Object2 : null);

                switch (categoryName.ToLower())
                {
                    case "detail":
                        HandleDetailCategoryChanges(delta, categoryName, draftChanges, activeChanges, matchedPropName, activeVal, draftVal, draftAdditionalCodeChanges, activeAdditionalCodeChanges, draftLocallicensedUseChanges, activeLocallicensedUseChanges, draftLocalUnlicensedUseChanges, activeLocalUnlicensedUseChanges);
                        break;
                    case "posology":
                        HandlePosologyCategoryChanges(delta, categoryName, draftChanges, activeChanges, matchedPropName, activeVal, draftVal, draftIngredientChanges, activeIngredientChanges, draftRouteDetailChanges, activeRouteDetailChanges, draftLocalRouteDetailChanges, activeLocalRouteDetailChanges, draftExcipientChanges, activeExcipientChanges);
                        break;
                    case "guidance":
                        HandleGuidanceCategoryChanges(delta, categoryName, draftChanges, activeChanges, matchedPropName, activeVal, draftVal);
                        break;
                    case "flags":
                        HandleFlagsCategoryChanges(delta, categoryName, draftChanges, activeChanges, matchedPropName, activeVal, draftVal);
                        break;
                    default:
                        break;
                }
            }

            if (draftAdditionalCodeChanges.IsCollectionValid())
                draftChanges["Detail"]["Additional Code"] = draftAdditionalCodeChanges;

            if (activeAdditionalCodeChanges.IsCollectionValid())
                activeChanges["Detail"]["Additional Code"] = activeAdditionalCodeChanges;

            if (draftLocallicensedUseChanges.IsCollectionValid())
                draftChanges["Detail"]["Local Licensed Use"] = draftLocallicensedUseChanges;

            if (activeLocallicensedUseChanges.IsCollectionValid())
                activeChanges["Detail"]["Local Licensed Use"] = activeLocallicensedUseChanges;

            if (draftLocalUnlicensedUseChanges.IsCollectionValid())
                draftChanges["Detail"]["Local Unlicensed Use"] = draftLocalUnlicensedUseChanges;

            if (activeLocalUnlicensedUseChanges.IsCollectionValid())
                activeChanges["Detail"]["Local Unlicensed Use"] = activeLocalUnlicensedUseChanges;

            if (draftIngredientChanges.IsCollectionValid())
                draftChanges["Posology"]["Ingredient"] = draftIngredientChanges;

            if (activeIngredientChanges.IsCollectionValid())
                activeChanges["Posology"]["Ingredient"] = activeIngredientChanges;

            if (draftRouteDetailChanges.IsCollectionValid())
                draftChanges["Posology"]["Route"] = draftRouteDetailChanges;

            if (activeRouteDetailChanges.IsCollectionValid())
                activeChanges["Posology"]["Route"] = activeRouteDetailChanges;

            if (draftLocalRouteDetailChanges.IsCollectionValid())
                draftChanges["Posology"]["Local Route"] = draftLocalRouteDetailChanges;

            if (activeLocalRouteDetailChanges.IsCollectionValid())
                activeChanges["Posology"]["Local Route"] = activeLocalRouteDetailChanges;

            if (draftExcipientChanges.IsCollectionValid())
                draftChanges["Posology"]["Excipient"] = draftExcipientChanges;

            if (activeExcipientChanges.IsCollectionValid())
                activeChanges["Posology"]["Excipient"] = activeExcipientChanges;

            delta.DeltaDetail = JsonConvert.SerializeObject(new { Active = activeChanges, Draft = draftChanges });
        }

        private void HandleGuidanceCategoryChanges(FormularyChangeLogDTO delta, string categoryName, Dictionary<string, Dictionary<string, dynamic>> draftChanges, Dictionary<string, Dictionary<string, dynamic>> activeChanges, string? matchedPropName, object? activeVal, object? draftVal)
        {
            delta.HasProductGuidanceChanged = true;

            var detailDraftLkp = draftChanges[categoryName];
            var detailActiveLkp = activeChanges[categoryName];
            var matchedDisplayName = GetDisplayableName(matchedPropName);

            detailDraftLkp[matchedDisplayName] = $"{draftVal}";
            detailActiveLkp[matchedDisplayName] = $"{activeVal}";
        }

        private void HandleFlagsCategoryChanges(FormularyChangeLogDTO delta, string categoryName, Dictionary<string, Dictionary<string, dynamic>> draftChanges, Dictionary<string, Dictionary<string, dynamic>> activeChanges, string? matchedPropName, object? activeVal, object? draftVal)
        {
            delta.HasProductFlagsChanged = true;

            var detailDraftLkp = draftChanges[categoryName];
            var detailActiveLkp = activeChanges[categoryName];
            var matchedDisplayName = GetDisplayableName(matchedPropName);

            detailDraftLkp[matchedDisplayName] = $"{draftVal}";
            detailActiveLkp[matchedDisplayName] = $"{activeVal}";
        }

        private void HandlePosologyCategoryChanges(FormularyChangeLogDTO delta, string categoryName, Dictionary<string, Dictionary<string, dynamic>> draftChanges, Dictionary<string, Dictionary<string, dynamic>> activeChanges, string? matchedPropName, object? activeVal, object? draftVal, List<DMDFormularyIngredient> draftIngredientChanges, List<DMDFormularyIngredient> activeIngredientChanges, List<DMDFormularyRouteDetail> draftRouteDetailChanges, List<DMDFormularyRouteDetail> activeRouteDetailChanges, List<DMDFormularyRouteDetail> draftLocalRouteDetailChanges, List<DMDFormularyRouteDetail> activeLocalRouteDetailChanges, List<DMDFormularyExcipient> draftExcipientChanges, List<DMDFormularyExcipient> activeExcipientChanges)
        {
            delta.HasProductPosologyChanged = true;

            var detailDraftLkp = draftChanges[categoryName];
            var detailActiveLkp = activeChanges[categoryName];
            var matchedDisplayName = GetDisplayableName(matchedPropName);

            if (string.Compare(matchedPropName, "FormularyIngredient", true) == 0)
            {
                if (draftVal != null)
                    draftIngredientChanges.Add(_mapper.Map<DMDFormularyIngredient>(draftVal));
                if (activeVal != null)
                    activeIngredientChanges.Add(_mapper.Map<DMDFormularyIngredient>(activeVal));
            }
            else if (string.Compare(matchedPropName, "FormularyRouteDetail", true) == 0)
            {
                if (draftVal != null)
                    draftRouteDetailChanges.Add(_mapper.Map<DMDFormularyRouteDetail>(draftVal));
                if (activeVal != null)
                    activeRouteDetailChanges.Add(_mapper.Map<DMDFormularyRouteDetail>(activeVal));

            }
            else if (string.Compare(matchedPropName, "FormularyLocalRouteDetail", true) == 0)
            {
                if (draftVal != null)
                    draftLocalRouteDetailChanges.Add(_mapper.Map<DMDFormularyRouteDetail>(draftVal));
                if (activeVal != null)
                    activeLocalRouteDetailChanges.Add(_mapper.Map<DMDFormularyRouteDetail>(activeVal));
            }
            else if (string.Compare(matchedPropName, "FormularyExcipient", true) == 0)
            {
                if (draftVal != null)
                    draftExcipientChanges.Add(_mapper.Map<DMDFormularyExcipient>(draftVal));
                if (activeVal != null)
                    activeExcipientChanges.Add(_mapper.Map<DMDFormularyExcipient>(activeVal));
            }
            else
            {
                detailDraftLkp[matchedDisplayName] = $"{draftVal}";
                detailActiveLkp[matchedDisplayName] = $"{activeVal}";
            }

        }

        private void HandleDetailCategoryChanges(FormularyChangeLogDTO delta, string categoryName, Dictionary<string, Dictionary<string, dynamic>> draftChanges, Dictionary<string, Dictionary<string, dynamic>> activeChanges, string? matchedPropName, object? activeVal, object? draftVal, List<DMDFormularyAdditionalCode> draftAdditionalCodeChanges, List<DMDFormularyAdditionalCode> activeAdditionalCodeChanges, List<DMDFormularyLookupItem> draftLocallicensedUseChanges, List<DMDFormularyLookupItem> activeLocallicensedUseChanges, List<DMDFormularyLookupItem> draftLocalUnlicensedUseChanges, List<DMDFormularyLookupItem> activeLocalUnlicensedUseChanges)
        {
            delta.HasProductDetailChanged = true;

            var detailDraftLkp = draftChanges[categoryName];
            var detailActiveLkp = activeChanges[categoryName];

            var matchedDisplayName = GetDisplayableName(matchedPropName);

            if (string.Compare(matchedPropName, "FormularyAdditionalCode", true) == 0)
            {
                if (draftVal != null)
                    draftAdditionalCodeChanges.Add(_mapper.Map<DMDFormularyAdditionalCode>(draftVal));
                if (activeVal != null)
                    activeAdditionalCodeChanges.Add(_mapper.Map<DMDFormularyAdditionalCode>(activeVal));
            }
            else if (string.Compare(matchedPropName, "LocalLicensedUses", true) == 0)
            {
                if (draftVal != null)
                    draftLocallicensedUseChanges.Add(_mapper.Map<DMDFormularyLookupItem>(draftVal));
                if (activeVal != null)
                    activeLocallicensedUseChanges.Add(_mapper.Map<DMDFormularyLookupItem>(activeVal));
            }
            else if (string.Compare(matchedPropName, "LocalUnLicensedUses", true) == 0)
            {
                if (draftVal != null)
                    draftLocalUnlicensedUseChanges.Add(_mapper.Map<DMDFormularyLookupItem>(draftVal));
                if (activeVal != null)
                    activeLocalUnlicensedUseChanges.Add(_mapper.Map<DMDFormularyLookupItem>(activeVal));
            }
            else if (string.Compare(matchedPropName, "isdmdinvalid", true) == 0)
            {
                delta.HasProductInvalidFlagChanged = true;
                detailDraftLkp[matchedDisplayName] = $"{draftVal}";
                detailActiveLkp[matchedDisplayName] = $"{activeVal}";

            }
            else if (string.Compare(matchedPropName, "isdmddeleted", true) == 0)
            {
                delta.HasProductDeletedChanged = true;
                detailDraftLkp[matchedDisplayName] = $"{draftVal}";
                detailActiveLkp[matchedDisplayName] = $"{activeVal}";
            }
            else
            {
                detailDraftLkp[matchedDisplayName] = $"{draftVal}";
                detailActiveLkp[matchedDisplayName] = $"{activeVal}";
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

        private ComparisonConfig GetHeaderConfig()
        {
            ComparisonConfig config = new()
            {
                MaxDifferences = 10000000,
                IgnoreCollectionOrder = true
            };
            config.CustomComparers.Add(new BooleanAndNullComparer(RootComparerFactory.GetRootComparer()));
            config.CustomComparers.Add(new StringFalseAndNullComparer(RootComparerFactory.GetRootComparer()));
            config.CustomComparers.Add(new CustomComparer<DMDFormularyAdditionalCode, DMDFormularyAdditionalCode>(
                (st1, st2) => !(st1.AdditionalCode != st2.AdditionalCode || st1.AdditionalCodeDesc != st2.AdditionalCodeDesc || st1.AdditionalCodeSystem != st2.AdditionalCodeSystem)
            ));
            //config.CustomComparers.Add(new CustomComparer<DMDFormularyIngredient, DMDFormularyIngredient>(
            //    (st1, st2) => !(st1.IngredientCd != st2.IngredientCd || st1.IngredientName != st2.IngredientName || st1.BasisOfPharmaceuticalStrengthCd != st2.BasisOfPharmaceuticalStrengthCd || st1.StrengthValueDenominator != st2.StrengthValueDenominator || st1.StrengthValueDenominatorUnitCd != st2.StrengthValueDenominatorUnitCd || st1.StrengthValueNumerator != st2.StrengthValueNumerator || st1.StrengthValueNumeratorUnitCd != st2.StrengthValueNumeratorUnitCd)
            //));
            //config.CustomComparers.Add(new CustomComparer<DMDFormularyExcipient, DMDFormularyExcipient>(
            //    (st1, st2) => !(st1.IngredientCd != st2.IngredientCd || st1.IngredientName != st2.IngredientName || st1.StrengthUnitCd != st2.StrengthUnitCd)
            //));
            //config.CustomComparers.Add(new CustomComparer<DMDFormularyRouteDetail, DMDFormularyRouteDetail>(
            //(st1, st2) => !(st1.RouteCd != st2.RouteCd || st1.RouteFieldType != st2.RouteFieldType)
            //));
            config.CustomComparers.Add(new CustomComparer<DMDFormularyIngredient, DMDFormularyIngredient>(
                (st1, st2) => !(st1.IngredientName != st2.IngredientName || st1.StrengthValueDenominator != st2.StrengthValueDenominator || st1.StrengthValueNumerator != st2.StrengthValueNumerator)));
            config.CustomComparers.Add(new CustomComparer<DMDFormularyExcipient, DMDFormularyExcipient>(
                (st1, st2) => !(st1.IngredientName != st2.IngredientName)
            ));
            config.CustomComparers.Add(new CustomComparer<DMDFormularyRouteDetail, DMDFormularyRouteDetail>(
            (st1, st2) => !(st1.RouteDesc != st2.RouteDesc || st1.RouteFieldType != st2.RouteFieldType)
            ));

            return config;
        }

        private List<string>? GetRecordsHavingActiveAndDraft(List<(string Code, string RecStatusCode)> ampsInDraftOrActive)
        {
            if (!ampsInDraftOrActive.IsCollectionValid()) return null;
            //select records that have both draft and active
            var drafts = ampsInDraftOrActive.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?
                .Select(rec => rec.Code)
                .Distinct(rec => rec)
                .ToList();

            var actives = ampsInDraftOrActive.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)?
                .Select(rec => rec.Code)
                .Distinct(rec => rec)
                .ToList();

            if (!drafts.IsCollectionValid() || !actives.IsCollectionValid()) return null;

            return drafts.Intersect(actives)?.ToList();
        }

        private (List<string?>? hasBoth, Dictionary<string, List<FormularyHeader>>? drafts, Dictionary<string, FormularyHeader>? actives) GetRecordsHavingActiveAndDraft(List<FormularyHeader>? ampsInDraftOrActive)
        {
            //select records that have both draft and active
            //var drafts = ampsInDraftOrActive.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?
            //    .Select(rec => new { Code = rec.Code, Data = rec })?
            //    .Distinct(rec => rec.Code)?
            //    .ToDictionary(k => k.Code, v => v.Data);

            //var actives = ampsInDraftOrActive.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)?
            //    .Select(rec => new { Code = rec.Code, Data = rec })?
            //    .Distinct(rec => rec.Code)?
            //    .ToDictionary(k => k.Code, v => v.Data);

            var drafts = new Dictionary<string, List<FormularyHeader>>();

            //Draft can be multiple
            ampsInDraftOrActive.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_DRAFT)?
                .Each(rec =>
                {
                    if (!drafts.ContainsKey(rec.Code))
                        drafts[rec.Code] = new List<FormularyHeader>();
                    drafts[rec.Code].Add(rec);
                });

            var actives = ampsInDraftOrActive.Where(rec => rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE)?
                .Select(rec => new { Code = rec.Code, Data = rec })?
                .Distinct(rec => rec.Code)?
                .ToDictionary(k => k.Code, v => v.Data);

            if (!drafts.IsCollectionValid() || !actives.IsCollectionValid()) return (null, drafts, actives);

            return (hasBoth: drafts.Keys.Intersect(actives.Keys)?.ToList(), drafts: drafts, actives: actives);
        }
    }

    /// <summary>
    /// This Comparer treats the 'false' and 'null' as 'false'
    /// FALSE : NULL - No Change
    /// NULL : FALSE - No Change
    /// FALSE : FALSE - No Change
    /// TRUE : TRUE - No Change
    /// TRUE : NULL - Has Change
    /// NULL : TRUE - Has Change
    /// </summary>
    public class BooleanAndNullComparer : BaseTypeComparer
    {
        public BooleanAndNullComparer(RootComparer rootComparer) : base(rootComparer)
        {
        }

        public override bool IsTypeMatch(Type type1, Type type2)
        {
            return (type1 == null && type2 == typeof(Boolean)) || (type1 == typeof(Boolean) && type2 == null);
        }

        public override void CompareType(CompareParms parms)
        {
            if (((parms.Object2 != null && (Boolean)parms.Object2 == true) && parms.Object1 == null) || ((parms.Object1 != null && (Boolean)parms.Object1 == true) && parms.Object2 == null))
            {
                AddDifference(parms);
            }
        }
    }

    public class StringFalseAndNullComparer : BaseTypeComparer
    {
        public StringFalseAndNullComparer(RootComparer rootComparer) : base(rootComparer)
        {
        }

        public override bool IsTypeMatch(Type type1, Type type2)
        {
            return (type1 == null && type2 == typeof(string)) || (type1 == typeof(string) && type2 == null);
        }

        public override void CompareType(CompareParms parms)
        {
            if (((parms.Object2 != null && string.Compare(parms.Object2.ToString(), "true", true) == 0) && parms.Object1 == null) || ((parms.Object1 != null && string.Compare(parms.Object1.ToString(), "true", true) == 0) && parms.Object2 == null))
            {
                AddDifference(parms);
            }
        }
    }

    public class DMDComparableProductDetail
    {
        public string? Code { get; set; }
        public string? FormularyId { get; set; }
        public string? Name { get; set; }
        public string? ProductType { get; set; }
        public string? ParentCode { get; set; }
        public string Prevcode { get; set; }
        public string? VtmId { get; set; }
        public string? VmpId { get; set; }
        public bool? IsDmdInvalid { get; set; }
        public bool? IsDmdDeleted { get; set; }
        //public string BasisOfPreferredNameCd { get; set; }
        public string BasisOfPreferredNameDesc { get; set; }
        //public string CurrentLicensingAuthorityCd { get; set; }
        public string CurrentLicensingAuthorityDesc { get; set; }
        //public string SupplierCd { get; set; }
        public string SupplierName { get; set; }
        //public string DoseFormCd { get; set; }
        public string DoseFormDesc { get; set; }
        //public string FormCd { get; set; }
        public string FormDesc { get; set; }
        public string UnitDoseFormSize { get; set; }
        public string UnitDoseFormUnits { get; set; }
        public string UnitDoseFormUnitsDesc { get; set; }
        //public string UnitDoseUnitOfMeasureCd { get; set; }
        public string UnitDoseUnitOfMeasureDesc { get; set; }
        //public string ControlledDrugCategoryCd { get; set; }
        public string ControlledDrugCategoryDesc { get; set; }
        //public string RestrictionsOnAvailabilityCd { get; set; }
        public string RestrictionsOnAvailabilityDesc { get; set; }
        //public string PrescribingStatusCd { get; set; }
        public string PrescribingStatusDesc { get; set; }
        public string EmaAdditionalMonitoring { get; set; }
        public string Prescribable { get; set; }
        public string SugarFree { get; set; }
        public string GlutenFree { get; set; }
        public string PreservativeFree { get; set; }
        public string CfcFree { get; set; }
        public string UnlicensedMedicationCd { get; set; }
        public string ParallelImport { get; set; }
        public List<DMDFormularyLookupItem> LocalLicensedUses { get; set; }
        public List<DMDFormularyLookupItem> LocalUnLicensedUses { get; set; }

        public List<DMDFormularyExcipient> FormularyExcipient { get; set; }
        public List<DMDFormularyAdditionalCode> FormularyAdditionalCode { get; set; }
        public List<DMDFormularyIngredient> FormularyIngredient { get; set; }
        public List<DMDFormularyRouteDetail> FormularyLocalRouteDetail { get; set; }
        public List<DMDFormularyRouteDetail> FormularyRouteDetail { get; set; }
    }

    public class DMDFormularyLookupItem
    {
        public string Cd { get; set; }
        public string Desc { get; set; }
        //public string Type { get; set; }
        //public bool? IsDefault { get; set; }
        //public short? Recordstatus { get; set; }
        //public string Source { get; set; }
        //public string AdditionalProperties { get; set; }
    }

    public partial class DMDFormularyExcipient
    {
        //public string? IngredientCd { get; set; }
        public string? Strength { get; set; }
        //public string? StrengthUnitCd { get; set; }
        public string? IngredientName { get; set; }
        public string? StrengthUnitDesc { get; set; }
    }

    public partial class DMDFormularyRouteDetail
    {
        //public string RouteCd { get; set; }
        public string RouteFieldType { get; set; }
        //public string Source { get; set; }
        public string RouteDesc { get; set; }
    }
    public partial class DMDFormularyIngredient
    {
        //public string IngredientCd { get; set; }
        //public string BasisOfPharmaceuticalStrengthCd { get; set; }
        public string StrengthValueNumerator { get; set; }
        //public string StrengthValueNumeratorUnitCd { get; set; }
        public string StrengthValueDenominator { get; set; }
        //public string StrengthValueDenominatorUnitCd { get; set; }
        public string IngredientName { get; set; }
        public string BasisOfPharmaceuticalStrengthDesc { get; set; }
        public string StrengthValueNumeratorUnitDesc { get; set; }
        public string StrengthValueDenominatorUnitDesc { get; set; }
    }
    public partial class DMDFormularyAdditionalCode
    {
        public string AdditionalCode { get; set; }
        public string AdditionalCodeSystem { get; set; }
        public string AdditionalCodeDesc { get; set; }
        //public string Attr1 { get; set; }
        //public string MetaJson { get; set; }
        public string Source { get; set; }
        public string CodeType { get; set; }
    }
}


