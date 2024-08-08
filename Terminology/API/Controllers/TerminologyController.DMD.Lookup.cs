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
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.API.AppCode.DTOs.DMD;
using Interneuron.Terminology.API.AppCode.Queries;
using Interneuron.Terminology.Model.DomainModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Interneuron.Terminology.API.Controllers
{
    public partial class TerminologyController : ControllerBase
    {
        [HttpGet, Route("getdmdroutelookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupRouteDTO>> GetRouteLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupRouteDTO>(LookupType.DMDRoute, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdformlookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupFormDTO>> GetFormLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupFormDTO>(LookupType.DMDForm, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdprescribingstatuslookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupPrescribingstatusDTO>> GetPrescribingStatusLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupPrescribingstatusDTO>(LookupType.DMDPrescribingStatus, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdcontroldrugcategorylookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupControldrugcatDTO>> GetControlDrugCategoryLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupControldrugcatDTO>(LookupType.DMDControlDrugCategory, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdsupplierlookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupSupplierDTO>> GetSupplierLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupSupplierDTO>(LookupType.DMDSupplier, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdlicensingauthoritylookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupLicauthDTO>> GetLicensingAuthorityLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupLicauthDTO>(LookupType.DMDLicensingAuthority, ignoreCacheSource);
        }

        [HttpGet, Route("getdmduomlookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupUomDTO>> GetDMDUOMLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupUomDTO>(LookupType.DMDUOM, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdontformroutelookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupOntformrouteDTO>> GetDMDOntFormRouteLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupOntformrouteDTO>(LookupType.DMDOntFormRoute, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdbasisofnamelookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupBasisofnameDTO>> GetDMDBasisOfNameLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupBasisofnameDTO>(LookupType.DMDBasisOfName, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdavailrestrictionslookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupAvailrestrictDTO>> GetDMDAvailRestrictionsLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupAvailrestrictDTO>(LookupType.DMDAvailRestrictions, ignoreCacheSource);
        }

        [HttpGet, Route("getdmddoseformlookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupDrugformindDTO>> GetDMDDoseFormLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupDrugformindDTO>(LookupType.DMDDoseForm, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdpharamceuticalstrengthlookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupBasisofstrengthDTO>> GetDMDPharamceuticalStrengthLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupBasisofstrengthDTO>(LookupType.DMDPharamceuticalStrength, ignoreCacheSource);
        }

        [HttpGet, Route("getdmdingredientlookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupIngredientDTO>> GetDMDIngredientLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupIngredientDTO>(LookupType.DMDIngredient, ignoreCacheSource);
        }

        [HttpGet, Route("getatclookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<AtcLookupDTO>> GetATCLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<AtcLookupDTO>(LookupType.ATCCode, ignoreCacheSource);
        }

        [HttpGet, Route("getbnflookup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DmdLookupBNFDTO>> GetBNFLookup([FromQuery] bool ignoreCacheSource = false)
        {
            return await this._dmdQueries.GetLookup<DmdLookupBNFDTO>(LookupType.BNFCode, ignoreCacheSource);
        }

        [HttpGet, Route("getallatccodesfromdmd")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DmdATCCodeDTO>>> GetAllATCCodesFromDMD([FromQuery] bool ignoreCacheSource = false)
        {
            var results = await _dmdQueries.GetAllATCCodesFromDMD(ignoreCacheSource);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }

        [HttpGet, Route("getallbnfcodesfromdmd")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DmdBNFCodeDTO>>> GetAllBNFCodesFromDMD([FromQuery] bool ignoreCacheSource = false)
        {
            var results = await _dmdQueries.GetAllBNFCodesFromDMD(ignoreCacheSource);

            if (!results.IsCollectionValid()) return NoContent();

            return Ok(results);
        }
    }
}
