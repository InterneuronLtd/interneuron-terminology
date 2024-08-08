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
using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.Controllers
{
    public partial class FormularyController : ControllerBase
    {
        [HttpGet, Route("getusagestats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FormularyUsageStatDTO>>> GetUsageStats(string searchText, string source = null)
        {
            if (searchText.IsEmpty()) return BadRequest();

            var statFetchTimeframe = 60l;

            if (long.TryParse(_configuration["TerminologyConfig:StatFetchTimeframeInDays"], out long statFetchTimeframeInDays))
                statFetchTimeframe = statFetchTimeframeInDays;

            var filterMeta = new Dictionary<string, object>();
            filterMeta["statFetchTimeframeInDays"] = statFetchTimeframe;

            if (source.IsNotEmpty())//not considered in the query for now
                filterMeta["source"] = source;

            var data = await this._formularyQueries.GetFormulariesUsageStatByPrefix(searchText, filterMeta);

            if (!data.IsCollectionValid()) return NoContent();

            return data;
        }

        [HttpPost, Route("saveusagestat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SaveUsageStat([FromBody] List<SaveFormularyUsageStatRequest> request)
        {
            if (request == null) return BadRequest();

            foreach (var item in request)
            {
                if (item.Name.IsEmpty() || item.Code.IsEmpty())
                {
                    return BadRequest();
                }
            }

            var result = await _formularyCommand.SaveFormularyUsageStat(request);

            if (result == null || result.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
                return BadRequest(result?.ErrorMessages);

            return Ok(result);
        }


        //[HttpGet, Route("seedusagestats")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult> SeedUsageStatsFromEPMA()
        //{
        //    var accessToken = await GetAccessToken();
        //    if (accessToken == null) return null;

        //    var dynamicServiceAPIUrl = $"{_configuration["TerminologyConfig:DynamicAPIEndpoint"]}/GetBaseViewList/epma_prescriptionsusagestat";

        //    using var client = new RestClient(dynamicServiceAPIUrl);

        //    var request = new RestRequest() { Method = Method.Get, Timeout = -1 };
        //    request.AddHeader("Authorization", $"Bearer {accessToken}");

        //    request.AddHeader("Content-Type", "application/json");
        //    request.AddHeader("Accept-Language", "application/json");

        //    var response = await client.ExecuteAsync<List<FormularyUsageStatDTO>>(request);

        //    if (response == null || (response.StatusCode != System.Net.HttpStatusCode.Accepted && response.StatusCode != System.Net.HttpStatusCode.OK) || response.Content.IsEmpty())
        //    {
        //        return NoContent();
        //    }
        //    var data = new List<FormularyUsageStatDTO>();
        //    try
        //    {
        //        var dataTemp = JsonConvert.DeserializeObject<dynamic>(response.Content);
        //        data = JsonConvert.DeserializeObject<List<FormularyUsageStatDTO>>(dataTemp);
        //    }
        //    catch { }

        //    if (!data.IsCollectionValid()) return NoContent();

        //    var result = await _formularyCommand.SeedFormularyUsageStatForEPMA(data);

        //    if (result == null || result.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
        //        return BadRequest(result?.ErrorMessages);

        //    return Ok();
        //}

        //private async Task<string> GetAccessToken()
        //{
        //    //Invoke cache api endpoint
        //    var accessTokenUrl = _configuration.GetSection("TerminologyConfig")["AccessTokenUrl"];

        //    var headerParams = new Dictionary<string, string>()
        //    {
        //        ["grant_type"] = "client_credentials",
        //        ["client_id"] = "client",
        //        ["client_secret"] = "secret",
        //        ["scope"] = "terminologyapi.write dynamicapi.read terminologyapi.read carerecordapi.read"
        //    };

        //    using (var client = new RestClient(accessTokenUrl))
        //    {
        //        var request = new RestRequest() { Method = Method.Post, Timeout = -1 };
        //        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

        //        foreach (var param in headerParams)
        //        {
        //            request.AddParameter(param.Key, param.Value);
        //        }

        //        var response = await client.ExecuteAsync<AccessTokenDetail>(request);

        //        if (response == null || response.Data == null || string.IsNullOrEmpty(response.Data.Access_Token))
        //        {
        //            return null;
        //        }
        //        return response.Data.Access_Token;
        //    }


        //}
    }


}
