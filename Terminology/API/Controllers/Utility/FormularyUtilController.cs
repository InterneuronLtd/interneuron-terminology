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
﻿
using AutoMapper;
using Interneuron.Common.Extensions;
using Interneuron.FDBAPI.Client;
using Interneuron.FDBAPI.Client.DataModels;
using Interneuron.Terminology.API.AppCode.Commands;
using Interneuron.Terminology.API.AppCode.Core.BackgroundProcess;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.Infrastructure;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.Controllers.Utility
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public partial class FormularyUtilController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IFormularyCommands _formularyCommand;
        private readonly APIRequestContext _requestContext;
        private readonly IServiceProvider _provider;
        private readonly IMapper _mapper;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly UpdateExistingFDBCodesFromFDBBackgroundService _fDBCodesFromFDBBackgroundService;
        private readonly UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService _bNFCodesFromVMPTOAMPsBackgroundService;

        public FormularyUtilController(IConfiguration configuration, IFormularyCommands formularyCommand, APIRequestContext requestContext, IServiceProvider provider, IMapper mapper, IBackgroundTaskQueue backgroundTaskQueue, UpdateExistingFDBCodesFromFDBBackgroundService fDBCodesFromFDBBackgroundService, UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService bNFCodesFromVMPTOAMPsBackgroundService)
        {
            _configuration = configuration;
            _formularyCommand = formularyCommand;
            _requestContext = requestContext;
            _provider = provider;
            _mapper = mapper;
            _backgroundTaskQueue = backgroundTaskQueue;
            _fDBCodesFromFDBBackgroundService = fDBCodesFromFDBBackgroundService;
            _bNFCodesFromVMPTOAMPsBackgroundService = bNFCodesFromVMPTOAMPsBackgroundService;
        }

        [HttpGet, Route("seedusagestats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SeedUsageStatsFromEPMA()
        {
            var accessToken = await GetAccessToken();
            if (accessToken == null) return StatusCode(500, "Unable to get access token. Please check for the scopes grant_type=client_credentials,client_id=client,client_secret=secret,scope=terminologyapi.write dynamicapi.read terminologyapi.read carerecordapi.read");

            var dynamicServiceAPIUrl = $"{_configuration["TerminologyConfig:DynamicAPIEndpoint"]}/GetBaseViewList/epma_prescriptionsusagestat";

            using var client = new RestClient(dynamicServiceAPIUrl);

            var request = new RestRequest() { Method = Method.Get, Timeout = -1 };
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept-Language", "application/json");

            var response = await client.ExecuteAsync<List<FormularyUsageStatDTO>>(request);

            if (response == null || (response.StatusCode != System.Net.HttpStatusCode.Accepted && response.StatusCode != System.Net.HttpStatusCode.OK) || response.Content.IsEmpty())
            {
                return NoContent();
            }
            var data = new List<FormularyUsageStatDTO>();
            try
            {
                var dataTemp = JsonConvert.DeserializeObject<dynamic>(response.Content);
                data = JsonConvert.DeserializeObject<List<FormularyUsageStatDTO>>(dataTemp);
            }
            catch { }

            if (!data.IsCollectionValid()) return NoContent();

            var result = await _formularyCommand.SeedFormularyUsageStatForEPMA(data);

            if (result == null || result.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
                return BadRequest(result?.ErrorMessages);

            return Ok();
        }

        private async Task<string> GetAccessToken()
        {
            //Invoke cache api endpoint
            var accessTokenUrl = _configuration.GetSection("TerminologyConfig")["AccessTokenUrl"];
            var dynamicAPICreds = _configuration.GetSection("TerminologyConfig")["DynamicAPICreds"].Split('|');
            var headerParams = new Dictionary<string, string>()
            {
                //["grant_type"] = "client_credentials",
                //["client_id"] = "client",
                //["client_secret"] = "secret",
                //["scope"] = "terminologyapi.write dynamicapi.read terminologyapi.read carerecordapi.read"
            };

            foreach (var item in dynamicAPICreds)
            {
                var kv = item.Split(':');
                headerParams.Add(kv[0], kv[1]);
            }

            using (var client = new RestClient(accessTokenUrl))
            {
                var request = new RestRequest() { Method = Method.Post, Timeout = -1 };
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

                foreach (var param in headerParams)
                {
                    request.AddParameter(param.Key, param.Value);
                }

                var response = await client.ExecuteAsync<AccessTokenDetail>(request);

                if (response == null || response.Data == null || string.IsNullOrEmpty(response.Data.Access_Token))
                {
                    return null;
                }
                return response.Data.Access_Token;
            }
        }

        //[HttpGet, Route("UpdateExistingFDBCodesFromFDB")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult> UpdateExistingFDBCodesFromFDB()
        //{
        //    //This is idempotent and can be re-executed
        //    //Earlier, as a part of the import process, only the last FDB code used to be imported and used.
        //    //Now, all the FDB codes will be fetched for the AMP from FDB and this api endpoint does it.

        //    return await Task.Run<ActionResult>(() =>
        //    {
        //        var messageId = Guid.NewGuid().ToString();

        //        _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
        //        {
        //            //var updateExistingFDBCodesFromFDBHandler = this._provider.GetService(typeof(UpdateExistingFDBCodesFromFDBBackgroundService)) as UpdateExistingFDBCodesFromFDBBackgroundService;

        //            await _fDBCodesFromFDBBackgroundService.UpdateExistingFDBCodesFromFDB(messageId);
        //        });

        //        return Accepted($"This is a long running process. Please check the log to know the status using the messageId : {messageId}");
        //    });
        //}


        //[HttpGet, Route("UpdateExistingBNFCodesFromVMPTOAMPs")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult> UpdateExistingBNFCodesFromVMPTOAMPs()
        //{
        //    var messageId = Guid.NewGuid().ToString();

        //    return await Task.Run<ActionResult>(() =>
        //    {
        //        _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
        //        {
        //            //var updateExistingBNFCodesFromVMPTOAMPsBackgroundServiceFromFDBHandler = this._provider.GetService(typeof(UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService)) as UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService;
        //           await _bNFCodesFromVMPTOAMPsBackgroundService.UpdateExistingBNFCodesFromVMPTOAMPs(messageId);
        //        });
        //        return Accepted($"This is a long running process. Please check the log to know the status using the messageId : {messageId}");
        //    });
        //}

        [HttpGet, Route("FixExistingLocalAndUnlicensedIndications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> FixExistingLocalAndUnlicensedIndications()
        {
            await _formularyCommand.FixExistingLocalAndUnlicensedIndications();
            return Ok();
        }

        [HttpGet, Route("FixExistingFDBCodesByDescription")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> FixExistingFDBCodesByDescription()
        {
            await _formularyCommand.FixExistingFDBCodesByDescription(_requestContext.AuthToken);
            return Ok();
        }

        /*Split to two - UpdateExistingFDBCodes, UpdateExistingBNFCodesFromVMPTOAMPs
        [HttpGet, Route("UpdateExistingFDBCodesFromFDBAndBNFCodesFromVMPTOAMPs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateExistingFDBCodesFromFDBAndBNFCodesFromVMPTOAMPs([FromServices] IServiceProvider serviceScopeFactory)
        {
            var messageId = Guid.NewGuid().ToString();

            return await Task.Run<ActionResult>(() =>
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    await using (var scopeA = serviceScopeFactory.CreateAsyncScope())
                    {
                        //var fDBCodesFromFDBBackgroundService = scopeA.ServiceProvider.GetRequiredService(typeof(UpdateExistingFDBCodesFromFDBBackgroundService)) as UpdateExistingFDBCodesFromFDBBackgroundService;

                        //var bNFCodesFromVMPTOAMPsBackgroundService = scopeA.ServiceProvider.GetRequiredService(typeof(UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService)) as UpdateExistingBNFCodesFromVMPTOAMPsBackgroundService;

                        var repo = scopeA.ServiceProvider.GetRequiredService(typeof(IRepository<FormularyHeader>)) as IRepository<FormularyHeader>;

                        var additionalCodeRepo = scopeA.ServiceProvider.GetRequiredService(typeof(IRepository<FormularyAdditionalCode>)) as IRepository<FormularyAdditionalCode>;

                        _bNFCodesFromVMPTOAMPsBackgroundService.UpdateExistingBNFCodesFromVMPTOAMPs(messageId, null, repo, additionalCodeRepo);

                        await _fDBCodesFromFDBBackgroundService.UpdateExistingFDBCodesFromFDB(messageId, null, repo, additionalCodeRepo, _requestContext.AuthToken);
                    }
                });
                return Accepted($"This is a long running process. Please check the log to know the status using the messageId : {messageId}");
            });
        }*/

        //Step - 1
        [HttpGet, Route("UpdateExistingBNFCodesFromVMPTOAMPs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateExistingBNFCodesFromVMPTOAMPs([FromServices] IServiceProvider serviceScopeFactory)
        {
            var messageId = Guid.NewGuid().ToString();

            return await Task.Run<ActionResult>(() =>
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    await using (var scopeA = serviceScopeFactory.CreateAsyncScope())
                    {
                        var repo = scopeA.ServiceProvider.GetRequiredService(typeof(IRepository<FormularyHeader>)) as IRepository<FormularyHeader>;

                        var additionalCodeRepo = scopeA.ServiceProvider.GetRequiredService(typeof(IRepository<FormularyAdditionalCode>)) as IRepository<FormularyAdditionalCode>;

                        _bNFCodesFromVMPTOAMPsBackgroundService.UpdateExistingBNFCodesFromVMPTOAMPs(messageId, null, repo, additionalCodeRepo);
                    }
                });
                return Accepted($"This is a long running process. Please check the log to know the status using the messageId : {messageId}");
            });
        }

        //Step -2 ( to be executed only after 1 is complete)
        [HttpGet, Route("UpdateExistingFDBCodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateExistingFDBCodes([FromServices] IServiceProvider serviceScopeFactory)
        {
            var messageId = Guid.NewGuid().ToString();

            return await Task.Run<ActionResult>(() =>
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    await using (var scopeA = serviceScopeFactory.CreateAsyncScope())
                    {
                        var repo = scopeA.ServiceProvider.GetRequiredService(typeof(IRepository<FormularyHeader>)) as IRepository<FormularyHeader>;

                        var additionalCodeRepo = scopeA.ServiceProvider.GetRequiredService(typeof(IRepository<FormularyAdditionalCode>)) as IRepository<FormularyAdditionalCode>;

                        await _fDBCodesFromFDBBackgroundService.UpdateExistingFDBCodesFromFDB(messageId, null, repo, additionalCodeRepo, _requestContext.AuthToken);
                    }
                });
                return Accepted($"This is a long running process. Please check the log to know the status using the messageId : {messageId}");
            });
        }

        [HttpGet, Route("UpdateDefaultBNFClassification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateDefaultBNFClassification([FromServices] IServiceProvider serviceScopeFactory)
        {
            await _formularyCommand.UpdateDefaultBNFs();

            return Ok();
        }

        [HttpGet, Route("UpdateBNFClassificationDescription")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateBNFClassificationDescription([FromServices] IServiceProvider serviceScopeFactory)
        {
            await _formularyCommand.UpdateBNFsDescription();

            return Ok();
        }

        [HttpGet, Route("AddBNFClassificationsInHierarchy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AddBNFClassificationsInHierarchy([FromServices] IServiceProvider serviceScopeFactory)
        {
            await _formularyCommand.AddBNFClassificationsInHierarchy();

            return Ok();
        }
    }
}