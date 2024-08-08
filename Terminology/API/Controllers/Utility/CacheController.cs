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
using Interneuron.Caching;
using Interneuron.Common.Extensions;
using Interneuron.Terminology.API.AppCode.Core.BackgroundProcess;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.Infrastructure.Caching;
using Interneuron.Terminology.API.AppCode.Queries;
using Interneuron.Terminology.Infrastructure.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Interneuron.Terminology.API.Controllers.Utility
{
    //[Authorize]
    [Route("api/util/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {
        private IServiceProvider _provider;
        private IFormularyQueries _formularyQueries;

        public CacheController(IServiceProvider provider, IFormularyQueries formularyQueries)
        {
            _provider = provider;
            _formularyQueries = formularyQueries;
        }

        [HttpGet, Route("allcachekeys")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public List<string> GetAllCacheKeys()
        {
            return CacheKeys.GetAllCacheKeys();
        }

        [HttpGet, Route("clearcache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ClearCache()
        {
            await CacheService.FlushAllAsync();
            return Accepted();
        }

        [HttpGet, Route("cacheserverstatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetCacheServerStatus()
        {
            var canPing = await CacheService.ServerPingAsync();
            if (!canPing) return NoContent();
            return Accepted();
        }

        [HttpGet, Route("clearcache/{key}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<bool> ClearCache(string key)
        {
            return await CacheService.RemoveAsync(key);
        }

        [HttpGet, Route("reloadcache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReloadCache()
        {
            await CacheService.FlushAllAsync();
            return Accepted();
        }

        [HttpGet, Route("reloadcache/{key}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<bool> ReloadCache(string key)
        {
            return await CacheService.RemoveAsync(key);
        }


        [HttpGet, Route("cacheformularydetailruleboundbycode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CacheActiveFormularyDetailRuleBoundByCode()
        {
            Log.Logger.Error("Info: Received the request to cache formulary detail rulebound by code");

            var cacheHandler = this._provider.GetService(typeof(CacheHandlerService)) as CacheHandlerService;

            await Task.Run(() => cacheHandler.CacheActiveFormularyDetailRuleBound(null, true));

            return Accepted();
        }

        [HttpPost, Route("cacheactiveformularydetailruleboundforcodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CacheActiveFormularyDetailRuleBoundForCodes([FromBody]List<string> codes)
        {
            Log.Logger.Error("Info: Received the request to cache formulary detail rulebound by code");

            if (!codes.IsCollectionValid()) return BadRequest();

            var cacheHandler = this._provider.GetService(typeof(CacheHandlerService)) as CacheHandlerService;

            await Task.Run(() => cacheHandler.CacheActiveFormularyDetailRuleBound(codes, true));

            return Accepted();
        }

        [HttpPost, Route("flushcacheeactiveformularydetailruleboundforcodes/{allactive}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CacheActiveFormularyDetailRuleBoundForCodes([FromBody] List<string> codes = null, [FromQuery] bool allactive = false)
        {
            Log.Logger.Error("Info: Received the request to flush formulary detail cache");

            if (!allactive && !codes.IsCollectionValid()) return BadRequest();

            if (codes.IsCollectionValid())
            {
                var keys = codes.Select(rec => $"{CacheKeys.ACTIVE_FORMULARY}{rec}").ToList();

                await CacheService.RemoveKeysAsync(keys);

                return Accepted();
            }
            await CacheService.FlushByKeyPatternAsync($"{CacheKeys.ACTIVE_FORMULARY}");

            return Accepted();
        }

        [HttpGet, Route("reloadlookupcache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReloadLookupCache()
        {
            await CacheService.RemoveAsync(CacheKeys.DMDDoseForm);
            await CacheService.RemoveAsync(CacheKeys.DMDPrescribingStatus);
            await CacheService.RemoveAsync(CacheKeys.DMDBasisOfName);
            await CacheService.RemoveAsync(CacheKeys.DMDForm);
            await CacheService.RemoveAsync(CacheKeys.DMDSupplier);
            await CacheService.RemoveAsync(CacheKeys.DMDUOM);
            await CacheService.RemoveAsync(CacheKeys.DMDAvailRestrictions);
            await CacheService.RemoveAsync(CacheKeys.DMDRoute);
            await CacheService.RemoveAsync(CacheKeys.DMDPharamceuticalStrength);
            await CacheService.RemoveAsync(CacheKeys.DMDLicensingAuthority);
            await CacheService.RemoveAsync(CacheKeys.DMDIngredient);
            await CacheService.RemoveAsync(CacheKeys.DMDControlDrugCategory);

            await _formularyQueries.GetLookup<DmdLookupDrugformindDTO, string, string>(LookupType.DMDDoseForm, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupPrescribingstatusDTO, string, string>(LookupType.DMDPrescribingStatus, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupBasisofnameDTO, string, string>(LookupType.DMDBasisOfName, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupFormDTO, string, string>(LookupType.DMDForm, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupSupplierDTO, string, string>(LookupType.DMDSupplier, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupUomDTO, string, string>(LookupType.DMDUOM, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupAvailrestrictDTO, string, string>(LookupType.DMDAvailRestrictions, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupRouteDTO, string, string>(LookupType.DMDRoute, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupBasisofstrengthDTO, string, string>(LookupType.DMDPharamceuticalStrength, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupLicauthDTO, string, string>(LookupType.DMDLicensingAuthority, rec => rec.Cd.ToString(), rec => rec.Desc);
            await _formularyQueries.GetLookup<DmdLookupIngredientDTO, string, string>(LookupType.DMDIngredient, rec => rec.Isid.ToString(), rec => rec.Nm);
            await _formularyQueries.GetLookup<DmdLookupControldrugcatDTO, string, string>(LookupType.DMDControlDrugCategory, rec => rec.Cd.ToString(), rec => rec.Desc);

            return Ok();
        }
    }
}
