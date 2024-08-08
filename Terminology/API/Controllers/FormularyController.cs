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
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interneuron.Common.Extensions;
using Interneuron.Terminology.API.AppCode.Commands;
using Interneuron.Terminology.API.AppCode.Core.BackgroundProcess;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary;
using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
using Interneuron.Terminology.API.AppCode.Queries;
using Interneuron.Terminology.API.AppCode.Validators;
using Interneuron.Terminology.Infrastructure.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Interneuron.Terminology.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    //[Route("api")]
    [ApiController]
    public partial class FormularyController : ControllerBase
    {
        private ILogger<FormularyController> _logger;
        private IServiceProvider _provider;
        private IFormularyQueries _formularyQueries;
        private IFormularyCommands _formularyCommand;
        private IDMDQueries _dmdQueries;
        private IDMDCommand _dmdCommand;
        private IConfiguration _configuration;

        public FormularyController(ILogger<FormularyController> logger, IServiceProvider provider, IFormularyQueries formularyQueries, IFormularyCommands formularyCommand, IDMDQueries dmdQueries, IDMDCommand dmdCommand, IConfiguration configuration)
        {
            _logger = logger;
            _provider = provider;
            _formularyQueries = formularyQueries;
            _formularyCommand = formularyCommand;
            _dmdQueries = dmdQueries;
            _dmdCommand = dmdCommand;
            _configuration = configuration;

        }

        [HttpPost, Route("searchformularies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormularySearchResultsWithHierarchyDTO>> SearchFormularies([FromBody] FormularySearchFilterRequest filterCriteria, [FromHeader] bool sortByStat = true, [FromHeader] bool considerOnlyActive = true)
        {
            if (filterCriteria == null) return BadRequest();

            var isCatergoryNotSelected = filterCriteria.CategoryDifference == null || (!filterCriteria.CategoryDifference.IsGuidanceChanged.GetValueOrDefault() || !filterCriteria.CategoryDifference.IsDeleted.GetValueOrDefault() || !filterCriteria.CategoryDifference.IsDetailChanged.GetValueOrDefault() || !filterCriteria.CategoryDifference.IsFlagsChanged.GetValueOrDefault() || !filterCriteria.CategoryDifference.IsPosologyChanged.GetValueOrDefault() || !filterCriteria.CategoryDifference.IsInvalid.GetValueOrDefault());

            if (considerOnlyActive)
            {
                if (!filterCriteria.RecStatusCds.IsCollectionValid())
                    filterCriteria.RecStatusCds = new List<string>();

                filterCriteria.RecStatusCds.Add(TerminologyConstants.RECORDSTATUS_ACTIVE);

                filterCriteria.RecStatusCds = filterCriteria.RecStatusCds.Distinct().ToList();
            }

            //hidearchived is true by default - and consider it as no filter
            //var hasNoFilters = filterCriteria.HideArchived == false && filterCriteria.SearchTerm.IsEmpty() && !filterCriteria.RecStatusCds.IsCollectionValid() && !filterCriteria.Flags.IsCollectionValid() && !filterCriteria.FormularyStatusCd.IsCollectionValid() && filterCriteria.ShowOnlyDuplicate == false && isCatergoryNotSelected && filterCriteria.ProductType.IsEmpty();
            var hasNoFilters = filterCriteria.HideArchived == true && filterCriteria.SearchTerm.IsEmpty() && !filterCriteria.RecStatusCds.IsCollectionValid() && !filterCriteria.Flags.IsCollectionValid() && !filterCriteria.FormularyStatusCd.IsCollectionValid() && filterCriteria.ShowOnlyDuplicate == false && isCatergoryNotSelected && filterCriteria.ProductType.IsEmpty();

            if (hasNoFilters) return BadRequest();

            if (!sortByStat)
                return await this._formularyQueries.GetFormularyHierarchyForSearchRequest(filterCriteria);

            var searchResultsTask = this._formularyQueries.GetFormularyHierarchyForSearchRequest(filterCriteria);

            var statFetchTimeframe = 60l;

            if (long.TryParse(_configuration["TerminologyConfig:StatFetchTimeframeInDays"], out long statFetchTimeframeInDays))
                statFetchTimeframe = statFetchTimeframeInDays;

            var filterMeta = new Dictionary<string, object>();
            filterMeta["statFetchTimeframeInDays"] = statFetchTimeframe;

            //Not required for now - but can be extendable
            if (filterCriteria.Source.IsNotEmpty())
                filterMeta["source"] = filterCriteria.Source;

            /*
            var statResultsTask = this._formularyQueries.GetFormulariesUsageStatByPrefix(filterCriteria.SearchTerm, filterMeta);

            await Task.WhenAll(searchResultsTask, statResultsTask);
            var searchResults = await searchResultsTask;
            var statResults = await statResultsTask;
            */
            var searchResults = await searchResultsTask;
            if (searchResults == null || !searchResults.Data.IsCollectionValid()) return searchResults;

            var codes = new List<string>();
            searchResults.Data.Each(rec =>
            {
                codes.Add(rec.Code);
                if (rec.Children.IsCollectionValid())
                    codes.AddRange(GetAllCodesOfChildren(rec.Children));
            });

            var statResultsTask = this._formularyQueries.GetFormulariesUsageStatByCodes(codes, filterMeta);
            var statResults = await statResultsTask;
            return GetSortedData(searchResults, statResults);
        }

        private IEnumerable<string> GetAllCodesOfChildren(List<FormularySearchResultWithTreeDTO> children)
        {
            if (!children.IsCollectionValid()) return null;
            var codes = new List<string>();
            children.Each(rec =>
            {
                codes.Add(rec.Code);
                if (rec.Children.IsCollectionValid())
                    codes.AddRange(GetAllCodesOfChildren(rec.Children));
            });
            return codes;
        }

        [HttpPost, Route("getformularychangelogforcodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyChangeLogDTO>>> GetFormularyChangeLogForCodes([FromBody] List<string> codes)
        {
            if (!codes.IsCollectionValid() || codes.Any(rec => rec == null || rec.Trim().IsEmpty())) return BadRequest();

            var changes = await this._formularyQueries.GetFormularyChangeLogForCodes(codes);
            if (!changes.IsCollectionValid()) return NoContent();

            return Ok(changes);
        }

        [HttpPost, Route("getformularychangelogforcodeswithchangedetailonly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Dictionary<string, string>>> GetFormularyChangeLogForCodesWithChangeDetailOnly([FromBody] List<string> codes)
        {
            if (!codes.IsCollectionValid() || codes.Any(rec => rec == null || rec.Trim().IsEmpty())) return BadRequest();

            var changes = await this._formularyQueries.GetFormularyChangeLogForCodes(codes);
            if (!changes.IsCollectionValid()) return NoContent();

            var changesLkp = changes
                .Select(rec => new { Code = rec.Code, Delta = rec.DeltaDetail })
                .Distinct(rec => rec.Code)
                .ToDictionary(k => k.Code, v => v.Delta);

            return Ok(changesLkp);
        }

        private FormularySearchResultsWithHierarchyDTO GetSortedData(FormularySearchResultsWithHierarchyDTO searchResults, List<FormularyUsageStatDTO> statResults)
        {
            if (!statResults.IsCollectionValid()) return searchResults;

            if (searchResults == null || !searchResults.Data.IsCollectionValid()) return searchResults;

            var statListWithPriority = statResults.Select(rec => new { Code = rec.Code, Priority = rec.UsageCount }).Distinct(rec => rec.Code).ToDictionary(k => k.Code, v => v.Priority);

            //EPMA-2891- Fix
            //var resultsByCode = searchResults.Data.Select(rec => new { Code = rec.Code, Rec = rec }).Distinct(rec => rec.Code).ToDictionary(k => k.Code, v => v.Rec);
            var resultsByCode = new List<(string Code, FormularySearchResultWithTreeDTO Value)>();
            searchResults.Data.Each(rec => resultsByCode.Add((rec.Code, rec)));

            //Build Priority
            var resultCodeWithPriority = BuildPriorityByStat(searchResults.Data, statListWithPriority);

            var queue = new PriorityQueue<FormularySearchResultWithTreeDTO, long>(resultsByCode.Count);

            //Heapify and get ordered by priority (max heap)
            //1. Add to queue
            //resultsByCode.Each(rec => queue.Enqueue(rec.Value, resultCodeWithPriority.ContainsKey(rec.Key) ? resultCodeWithPriority[rec.Key].Value : 0));
            resultsByCode.Each(rec => queue.Enqueue(rec.Value, resultCodeWithPriority.ContainsKey(rec.Code) ? resultCodeWithPriority[rec.Code].Value : 0));

            //2. Dequeue and put onto the stack - since it is minheap and not maxheap
            var stack = new Stack<FormularySearchResultWithTreeDTO>();

            while (queue.Count > 0)
            {
                var result = queue.Dequeue();
                result.Children = GetSortedChildren(result.Children, statListWithPriority);//now sort the children of each element
                stack.Push(result);
            }

            var orderedList = new List<FormularySearchResultWithTreeDTO>();

            //Take from stack - it will be of highest priority
            while (stack.Count > 0)
            {
                orderedList.Add(stack.Pop());
            }

            searchResults.Data.Clear();
            orderedList.ForEach(rec => searchResults.Data.Add(rec));

            return searchResults;
        }

        private Dictionary<string, long?> BuildPriorityByStat(List<FormularySearchResultWithTreeDTO> data, Dictionary<string, long?> statListWithPriority)
        {
            var resultCodeWithPriority = data
                .Select(rec => new { Code = rec.Code, Priority = statListWithPriority.ContainsKey(rec.Code) ? statListWithPriority[rec.Code] : 0 })?
                .Distinct()
                .ToDictionary(k => k.Code, v => v.Priority);

            return resultCodeWithPriority;
        }

        private List<FormularySearchResultWithTreeDTO> GetSortedChildren(List<FormularySearchResultWithTreeDTO> children, Dictionary<string, long?> statListWithPriority)
        {
            if (!children.IsCollectionValid()) return new List<FormularySearchResultWithTreeDTO>();

            //var resultsByCode = children.Select(rec => new { Code = rec.Code, Rec = rec }).Distinct(rec=> rec.Code).ToDictionary(k => k.Code, v => v.Rec);
            var resultsByCode = new List<(string Code, FormularySearchResultWithTreeDTO Value)>();
            children.Each(rec => resultsByCode.Add((rec.Code, rec)));

            //Build Priority
            var resultCodeWithPriority = BuildPriorityByStat(children, statListWithPriority);

            var queue = new PriorityQueue<FormularySearchResultWithTreeDTO, long>(resultsByCode.Count);

            //Heapify and get ordered by priority (max heap)
            //1. Add to queue
            //resultsByCode.Each(rec => queue.Enqueue(rec.Value, resultCodeWithPriority.ContainsKey(rec.Key) ? resultCodeWithPriority[rec.Key].Value : 0));
            resultsByCode.Each(rec => queue.Enqueue(rec.Value, resultCodeWithPriority.ContainsKey(rec.Code) ? resultCodeWithPriority[rec.Code].Value : 0));

            //2. Dequeue and put it onto the stack - since it is minheap and not maxheap
            var stack = new Stack<FormularySearchResultWithTreeDTO>();

            while (queue.Count > 0)
            {
                var result = queue.Dequeue();
                result.Children = GetSortedChildren(result.Children, statListWithPriority);//now sort the children of each element
                stack.Push(result);
            }

            var orderedList = new List<FormularySearchResultWithTreeDTO>();

            //Take from stack - it will be of highest priority
            while (stack.Count > 0)
            {
                orderedList.Add(stack.Pop());
            }

            return orderedList;
        }

        [HttpPost, Route("searchformulariesaslist")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<FormularySearchResultDTO>> SearchFormulariesAsList([FromBody] FormularySearchFilterRequest filterCriteria)
        {
            return await this._formularyQueries.GetFormularyAsFlatList(filterCriteria);
        }

        [HttpGet, Route("getlatestformulariesheaderonly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLatestFormulariesHeaderOnly()
        {
            var results = await new TaskFactory().StartNew(() => this._formularyQueries.GetLatestFormulariesBriefInfo());

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpGet, Route("getlatestformulariesheaderonlybynameorcode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLatestFormulariesHeaderOnlyByNameOrCode(string nameOrCode, string productType = null, bool isExactSearch = false)
        {
            if (nameOrCode.IsEmpty()) return BadRequest();

            var results = await new TaskFactory().StartNew(() => this._formularyQueries.GetLatestFormulariesBriefInfoByNameOrCode(nameOrCode, productType, isExactSearch));

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getlatesttoplevelformulariesbasicinfo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLatestTopLevelFormulariesBasicInfoByStatusCodes()
        {
            var results = await this._formularyQueries.GetLatestTopLevelFormulariesBasicInfo();

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpGet, Route("getformulariesasdiluents")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormularySearchResultDTO>> GetFormulariesAsDiluents()
        {
            var results = await _formularyQueries.GetFormulariesAsDiluents();

            return Ok(results);
        }

        [HttpPost, Route("getdescendentformulariesforcodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularySearchResultDTO>>> GetFormularyDescendentForCodes(GetFormularyDescendentForCodesRequest request)
        {
            if (request == null || !request.Codes.IsCollectionValid()) return BadRequest();

            var results = await this._formularyQueries.GetFormularyImmediateDescendentForCodes(request.Codes, request.OnlyNonDeleted);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }


        [HttpPost, Route("getimmediatedescendentformulariesforformularyversionids")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularySearchResultDTO>>> GetFormularyImmediateDescendentForFormularyVersionIds(GetFormularyDescendentForFormularyVersionIdsRequest request)
        {
            if (request == null || !request.FormularyVersionIds.IsCollectionValid()) return BadRequest();

            var results = await this._formularyQueries.GetFormularyImmediateDescendentForFormularyVersionIds(request.FormularyVersionIds, request.OnlyNonDeleted);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        //Deprecated
        //[HttpGet, Route("getformularydetail/{id}")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status410Gone)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> GetFormularyDetail(string id)
        //{
        //    if (id.IsEmpty()) return BadRequest();

        //    var result = await this._formularyQueries.GetFormularyDetail(id);

        //    if (result == null) return NoContent();

        //    return Ok(result);
        //}




        [HttpPut, Route("updatestatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UpdateFormularyRecordStatusDTO>> UpdateFormularyRecordStatus([FromBody] UpdateFormularyRecordStatusRequest request)
        {
            if (request == null || !request.RequestData.IsCollectionValid())
                return BadRequest();

            //Disabling cache for now
            //var result = await _formularyCommand.UpdateFormularyRecordStatus(request, updatableCodes =>
            //{
            //    if (updatableCodes == null) return;

            //    var cacheHandler = this._provider.GetService(typeof(CacheHandlerService)) as CacheHandlerService;

            //    if (updatableCodes != null && updatableCodes.Count > 0)
            //    {
            //        cacheHandler.CacheActiveFormularyDetailRuleBound(updatableCodes);
            //    }
            //});
            var result = await _formularyCommand.UpdateFormularyRecordStatus(request);

            if (result == null || result.Status == null || result.Status.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
                return BadRequest(result?.Status?.ErrorMessages);

            if (result.Status.StatusCode == TerminologyConstants.STATUS_FAIL) return StatusCode(500, result?.Status?.ErrorMessages);

            if (result.Status.ErrorMessages.IsCollectionValid())
                return StatusCode(207, result);

            //if (result.Status.StatusCode == TerminologyConstants.STATUS_SUCCESS)
            //{
            //    var vmpResult = await _formularyCommand.UpdateVMPFormularyRecordStatus(request);

            //    if (vmpResult.Status.StatusCode == TerminologyConstants.STATUS_SUCCESS)
            //    {
            //        var vtmResult = await _formularyCommand.UpdateVTMFormularyRecordStatus(request);
            //    }
            //}

            return Ok(result);
        }

        [HttpPut, Route("bulkupdatestatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UpdateFormularyRecordStatusDTO>> BulkUpdateFormularyRecordStatus([FromBody] UpdateFormularyRecordStatusRequest request)
        {
            if (request == null || !request.RequestData.IsCollectionValid())
                return BadRequest();

            var result = await _formularyCommand.BulkUpdateFormularyRecordStatus(request);

            if (result == null || result.Status == null || result.Status.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
                return BadRequest(result?.Status?.ErrorMessages);

            if (result.Status.StatusCode == TerminologyConstants.STATUS_FAIL) return StatusCode(500, result?.Status?.ErrorMessages);

            if (result.Status.ErrorMessages.IsCollectionValid())
                return StatusCode(207, result);

            return Ok(result);
        }

        [HttpPost, Route("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateEditFormularyDTO>> CreateFormulary([FromBody] CreateEditFormularyRequest request)
        {
            var validationResult = new CreateFormularyRequestValidator(request).Validate();

            if (!validationResult.IsValid)
                return BadRequest(validationResult.ValidationErrors);

            //No cache for now
            //var result = await _formularyCommand.CreateFormulary(request, codes =>
            //{
            //    if (!codes.IsCollectionValid())
            //    {
            //        return;
            //    }

            //    var cacheHandler = this._provider.GetService(typeof(CacheHandlerService)) as CacheHandlerService;

            //    cacheHandler.CacheActiveFormularyDetailRuleBound(codes);
            //});
            var result = await _formularyCommand.CreateFormulary(request);

            if (result == null || result.Status == null || result.Status.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
                return BadRequest(result?.Status?.ErrorMessages);

            if (result.Status.StatusCode == TerminologyConstants.STATUS_FAIL) return StatusCode(500, result?.Status?.ErrorMessages);

            if (result.Status.ErrorMessages.IsCollectionValid())
                return StatusCode(207, result);


            return Ok(result);
        }



        [HttpPut, Route("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateEditFormularyDTO>> UpdateFormulary([FromBody] CreateEditFormularyRequest request)
        {
            //To be changed for Edit functionality
            var validationResult = new EditFormularyRequestValidator(request).Validate();

            if (!validationResult.IsValid)
                return BadRequest(validationResult.ValidationErrors);

            //No cache for now
            //var result = await _formularyCommand.UpdateFormulary(request, codes =>
            //{
            //    if(!codes.IsCollectionValid())
            //    {
            //        return;
            //    }

            //    var cacheHandler = this._provider.GetService(typeof(CacheHandlerService)) as CacheHandlerService;

            //    cacheHandler.CacheActiveFormularyDetailRuleBound(codes);
            //});
            var result = await _formularyCommand.UpdateFormulary(request);

            if (result == null || result.Status == null || result.Status.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
                return BadRequest(result?.Status?.ErrorMessages);

            if (result.Status.StatusCode == TerminologyConstants.STATUS_FAIL) return StatusCode(500, result?.Status?.ErrorMessages);

            if (result.Status.ErrorMessages.IsCollectionValid())
                return StatusCode(207, result);

            return Ok(result);
        }


        [HttpPost, Route("fileimport")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(1073741824)]
        public async Task<ActionResult<CreateEditFormularyDTO>> FileImport([FromBody] CreateEditFormularyRequest request)
        {
            //No validation will be performed

            var result = await _formularyCommand.FileImport(request);

            if (result == null || result.Status == null || result.Status.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
                return BadRequest(result?.Status?.ErrorMessages);

            if (result.Status.StatusCode == TerminologyConstants.STATUS_FAIL) return StatusCode(500, result?.Status?.ErrorMessages);

            if (result.Status.ErrorMessages.IsCollectionValid())
                return StatusCode(207, result);


            return Ok(result);
        }

        //[HttpPost, Route("searchdmdandformularies")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status410Gone)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult<FormularySearchResultsWithHierarchyDTO>> SearchDMDAndFormularies(string searchTerm, string formularyStatusCode = null)
        //{
        //    if (searchTerm.IsEmpty()) return BadRequest();

        //    FormularySearchFilterRequest formularySearchFilter = new FormularySearchFilterRequest();

        //    formularySearchFilter.FormularyStatusCd = formularyStatusCode.IsNotEmpty() ? new List<string> { formularyStatusCode } : null;
        //    formularySearchFilter.SearchTerm = searchTerm;

        //    var result = await this._formularyQueries.GetDMDAndFormularyHierarchy(formularySearchFilter);

        //    return Ok(result);
        //}

        [HttpGet, Route("getformularydetailrulebound/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormularyDTO>> GetFormularyDetailRuleBound(string id, [FromQuery] bool getAllAdditionalCodes)
        {
            if (id.IsEmpty()) return BadRequest();

            var formularyDTO = await this._formularyQueries.GetFormularyDetailRuleBound(id, getAllAdditionalCodes);

            if (formularyDTO == null) return NotFound();

            return Ok(formularyDTO);
        }

        [HttpPost, Route("getformularydetailruleboundforids/{getAllAdditionalCodes}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyDTO>>> GetFormularyDetailRuleBoundForFVIds([FromBody] List<string> ids, [FromRoute] bool getAllAdditionalCodes = false)
        {
            if (!ids.IsCollectionValid()) return BadRequest();

            var formularyDTOs = await this._formularyQueries.GetFormularyDetailRuleBoundForFVIds(ids, getAllAdditionalCodes);

            if (!formularyDTOs.IsCollectionValid()) return NotFound();

            return Ok(formularyDTOs);
        }

        [HttpPost, Route("getformularyheaderonlyforfvids")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyDTO>>> GetFormularyHeaderOnlyForFVIds([FromBody] List<string> ids)
        {
            if (!ids.IsCollectionValid()) return BadRequest();

            var formularyDTOs = await this._formularyQueries.GetFormularyHeaderOnlyForFVIds(ids);

            if (!formularyDTOs.IsCollectionValid()) return NotFound();

            return Ok(formularyDTOs);
        }


        [HttpGet, Route("getformularydetailruleboundbycode/{code}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormularyDTO>> GetActiveFormularyDetailRuleBoundByCode(string code, [FromHeader] bool fromCache = true, [FromHeader] bool includeInvalid = false)
        {
            if (code.IsEmpty()) return BadRequest();

            var cachingEnabledInCache = _configuration.GetSection("TerminologyConfig").GetValue<string>("ActiveFormularyFromCache");
            fromCache = fromCache ? cachingEnabledInCache == "1" : fromCache;

            var formularyDTO = await this._formularyQueries.GetActiveFormularyDetailRuleBoundByCode(code, fromCache, includeInvalid);

            if (formularyDTO == null) return NotFound();

            return Ok(formularyDTO);
        }

        [HttpPost, Route("getformularydetailruleboundbycodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyDTO>>> GetActiveFormularyDetailRuleBoundByCodes([FromBody] string[] codes, [FromHeader] bool fromCache = true, [FromHeader] bool includeInvalid = false)
        {
            if (!codes.IsCollectionValid()) return BadRequest();

            var cachingEnabledInCache = _configuration.GetSection("TerminologyConfig").GetValue<string>("ActiveFormularyFromCache");
            fromCache = fromCache ? cachingEnabledInCache == "1" : fromCache;

            List<FormularyDTO> formularyDTOs = await this._formularyQueries.GetActiveFormularyDetailRuleBoundByCodes(codes.ToList(), fromCache, includeInvalid);

            if (!formularyDTOs.IsCollectionValid()) return NotFound();

            return Ok(formularyDTOs);
        }

        [HttpPost, Route("getformularybasicdetailruleboundbycodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ActiveFormularyBasicDTO>>> GetActiveFormularyBasicDetailRuleBoundByCodes([FromBody] string[] codes, [FromHeader] bool fromCache = true, [FromHeader] bool includeInvalid = false)
        {
            if (!codes.IsCollectionValid()) return BadRequest();

            var cachingEnabledInCache = _configuration.GetSection("TerminologyConfig").GetValue<string>("ActiveFormularyFromCache");
            fromCache = fromCache ? cachingEnabledInCache == "1" : fromCache;

            List<ActiveFormularyBasicDTO> formularyDTOs = await this._formularyQueries.GetActiveFormularyBasicDetailRuleBoundByCodes(codes.ToList(), fromCache, includeInvalid);

            if (!formularyDTOs.IsCollectionValid()) return NotFound();

            return Ok(formularyDTOs);
        }

        //[HttpGet, Route("GetActiveFormularyDetailRuleBoundByCodeArray")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status410Gone)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult<FormularyDTO[]>> GetActiveFormularyDetailRuleBoundByCodeArray([FromQuery] string[] code)
        //{
        //    if (code == null || code.Length == 0) return BadRequest();

        //    var dmdCodes = code.Distinct().ToArray();

        //    var formularyDTOList = await this._formularyQueries.GetActiveFormularyDetailRuleBoundByCodeArray(dmdCodes);

        //    if (formularyDTOList == null || formularyDTOList.Length == 0) return NotFound();

        //    return Ok(formularyDTOList);
        //}

        [HttpPost, Route("deriveproductnames")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeriveProductNamesDTO>> DeriveProductNames(DeriveProductNamesRequest request)
        {
            var validationResult = new DeriveProductNamesRequestValidator(request).Validate();

            if (!validationResult.IsValid)
                return BadRequest(validationResult.ValidationErrors);

            var result = await new TaskFactory<DeriveProductNamesDTO>().StartNew(() => _formularyQueries.DeriveProductNames(request.Ingredients, request.UnitDoseFormSize, request.FormulationName, request.SupplierName, request.ProductType));

            return Ok(result);
        }

        [HttpPost, Route("checkifproductexists")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CheckIfProductExistsDTO>> CheckIfProductExists(CheckIfProductExistsRequest request)
        {
            var validationResult = new CheckIfProductExistsRequestValidator(request).Validate();

            if (!validationResult.IsValid)
                return BadRequest(validationResult.ValidationErrors);

            var result = await new TaskFactory<CheckIfProductExistsDTO>().StartNew(() => _formularyQueries.CheckIfProductExists(request.Ingredients, request.UnitDoseFormSize, request.FormulationName, request.SupplierName, request.ProductType));

            return Ok(result);
        }



        [HttpGet, Route("gethistoryofformularies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormularyHistoryDTO>> GetHistoryOfFormularies([FromQuery] HistoryOfFormulariesRequest request = null)
        {
            if (request == null)
                request = new HistoryOfFormulariesRequest() { PageNo = 1, PageSize = 10 };

            if (request.FilterParams.IsNotEmpty())
            {
                request.FilterParamsAsKV = Newtonsoft.Json.JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(request.FilterParams);
            }

            var results = await this._formularyQueries.GetHistoryOfFormularies(request);

            if (results == null) return NoContent();
            return Ok(results);
        }



        [HttpPost, Route("getlocallicenseduse")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyLocalLicensedUseDTO>>> GetLocalLicensedUse(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetLocalLicensedUse(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getlocalunlicenseduse")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyLocalUnlicensedUseDTO>>> GetLocalUnlicensedUse(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetLocalUnlicensedUse(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getlocallicensedroute")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyLocalLicensedRouteDTO>>> GetLocalLicensedRoute(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetLocalLicensedRoute(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getlocalunlicensedroute")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyLocalUnlicensedRouteDTO>>> GetLocalUnlicensedRoute(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetLocalUnlicensedRoute(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getcustomwarning")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CustomWarningDTO>>> GetCustomWarning(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetCustomWarning(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getreminder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ReminderDTO>>> GetReminder(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetReminder(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getendorsement")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<EndorsementDTO>>> GetEndorsement(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetEndorsement(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getmedusapreparationinstruction")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<MedusaPreparationInstructionDTO>>> GetMedusaPreparationInstruction(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetMedusaPreparationInstruction(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("gettitrationtype")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<TitrationTypeDTO>>> GetTitrationType(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetTitrationType(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getroundingfactor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<RoundingFactorDTO>>> GetRoundingFactor(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetRoundingFactor(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getcompatiblediluent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CompatibleDiluentDTO>>> GetCompatibleDiluent(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetCompatibleDiluent(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getclinicaltrialmedication")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ClinicalTrialMedicationDTO>>> GetClinicalTrialMedication(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetClinicalTrialMedication(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getgastroresistant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<GastroResistantDTO>>> GetGastroResistant(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetGastroResistant(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getcriticaldrug")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CriticalDrugDTO>>> GetCriticalDrug(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetCriticalDrug(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getmodifiedrelease")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ModifiedReleaseDTO>>> GetModifiedRelease(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetModifiedRelease(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getexpensivemedication")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ExpensiveMedicationDTO>>> GetExpensiveMedication(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetExpensiveMedication(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("gethighalertmedication")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<HighAlertMedicationDTO>>> GetHighAlertMedication(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetHighAlertMedication(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getivtooral")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<IVToOralDTO>>> GetIVToOral(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetIVToOral(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getnotforprn")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<NotForPRNDTO>>> GetNotForPRN(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetNotForPRN(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getbloodproduct")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<BloodProductDTO>>> GetBloodProduct(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetBloodProduct(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getdiluent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DiluentDTO>>> GetDiluent(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetDiluent(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getprescribable")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PrescribableDTO>>> GetPrescribable(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetPrescribable(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getoutpatientmedication")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<OutpatientMedicationDTO>>> GetOutpatientMedication(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetOutpatientMedication(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getignoreduplicatewarning")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<IgnoreDuplicateWarningDTO>>> GetIgnoreDuplicateWarning(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetIgnoreDuplicateWarning(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getcontrolleddrug")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ControlledDrugDTO>>> GetControlledDrug(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetControlledDrug(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getprescriptionprintingrequired")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PrescriptionPrintingRequiredDTO>>> GetPrescriptionPrintingRequired(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetPrescriptionPrintingRequired(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getindicationmandatory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<IndicationMandatoryDTO>>> GetIndicationMandatory(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetIndicationMandatory(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getwitnessingrequired")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<WitnessingRequiredDTO>>> GetWitnessingRequired(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetWitnessingRequired(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getformularystatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyStatusDTO>>> GetFormularyStatus(List<string> formularyVersionIds)
        {
            var results = await this._formularyQueries.GetFormularyStatus(formularyVersionIds);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        #region - Not being used
        //[HttpGet, Route("GetFormularyDetailByCodes")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status410Gone)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult<List<FormularyDetailResultDTO>>> GetFormularyDetailByCodes([FromQuery] string[] code)
        //{
        //    if (code == null || code.Length == 0) return BadRequest();

        //    var dmdCodes = code.Distinct().ToArray();

        //    var formularyDTOList = await this._formularyQueries.GetFormularyDetailByCodes(dmdCodes);

        //    if (formularyDTOList == null || formularyDTOList.Count == 0) return NotFound();

        //    return Ok(formularyDTOList);
        //}
        #endregion

        [AllowAnonymous]//to be removed
        [HttpPost, Route("GetActiveFormularyCodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormularyDTO[]>> GetActiveFormularyCodes([FromBody] List<string> codes = null)
        {
            var activeCodes = await Task.Run(() => this._formularyQueries.GetActiveFormularyCodes(codes));

            if (activeCodes == null || activeCodes.Count == 0) return NotFound();

            return Ok(activeCodes);
        }

        [HttpPost, Route("validateampstatuschange")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ValidateAMPStatusChangeDTO>> ValidateAMPStatusChange([FromBody] ValidateFormularyStatusChangeRequest request)
        {
            if (request == null || !request.RequestsData.IsCollectionValid())
                return BadRequest();

            var result = await _formularyQueries.ValidateAMPStatusChange(request);

            if (result.Status.ErrorMessages.IsCollectionValid())
                return StatusCode(207, result);

            return Ok(result);
        }

        [HttpPost, Route("getheaderrecordslock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<bool>> GetHeaderRecordsLock(List<string> formularyVersionIds)
        {
            if (!formularyVersionIds.IsCollectionValid()) return BadRequest();
            return Ok(await _formularyCommand.GetHeaderRecordsLock(formularyVersionIds));
        }

        [HttpPost, Route("tryreleaseheaderrecordslock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> TryReleaseHeaderRecordsLock(List<string> formularyVersionIds)
        {
            if (!formularyVersionIds.IsCollectionValid()) return BadRequest();
            await _formularyCommand.TryReleaseHeaderRecordsLock(formularyVersionIds);
            return Ok();
        }

        [HttpGet, Route("hasanyupdateinprogess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<bool>> HasAnyUpdateInProgess()
        {
            var res = await Task.Run(() => _formularyQueries.HasAnyUpdateInProgress());
            return Ok(res);
        }

        [HttpPost, Route("localroutesforids")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormularyLocalRouteDetailDTO>> GetLocalRoutesForIds(GetRoutesRequest request)
        {
            if (request == null || !request.FormularyVersionIds.IsCollectionValid()) return BadRequest();

            if (request.RouteFieldTypeCd != null && !(request.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED || request.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL))
                return BadRequest("Route Field Type Code should be either '002' or '003'");

            var formularyLocalRouteDTO = await Task.Run(() => this._formularyQueries.GetLocalRoutes(request));

            if (formularyLocalRouteDTO == null) return NoContent();

            return Ok(formularyLocalRouteDTO);
        }

        [HttpPost, Route("routesforids")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormularyRouteDetailDTO>> GetRoutesForIds(GetRoutesRequest request)
        {
            if (request == null || !request.FormularyVersionIds.IsCollectionValid()) return BadRequest();

            if (request.RouteFieldTypeCd != null && !(request.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED || request.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_NORMAL))
                return BadRequest("Route Field Type Code should be either '002' or '003'");

            var formularyLocalRouteDTO = await Task.Run(() => this._formularyQueries.GetRoutes(request));

            if (formularyLocalRouteDTO == null) return NoContent();

            return Ok(formularyLocalRouteDTO);
        }
    }
}
