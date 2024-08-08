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
using Interneuron.Terminology.API.AppCode.Queries;
using Interneuron.Terminology.Model.DomainModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Interneuron.Terminology.API.Controllers
{
    public partial class TerminologyController : ControllerBase
    {
        [HttpGet, Route("searchdmd")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<DMDSearchResultsDTO> SearchDMD([FromQuery] string q)
        {
            return await this._dmdQueries.SearchDMD(System.Web.HttpUtility.UrlDecode(q));
        }

        
        [HttpGet, Route("searchdmdwithalldescendents")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<DMDSearchResultsWithHierarchyDTO> SearchDMDNamesAndGetWithChildren([FromQuery] string q)
        {
            return await this._dmdQueries.SearchDMDNamesGetWithAllDescendents(System.Web.HttpUtility.UrlDecode(q));
        }

        [HttpGet, Route("searchdmdsyncLog")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<DMDSearchResultsWithHierarchyDTO> SearchDMDSyncLog([FromQuery] string q)
        {
            return await this._dmdQueries.SearchDMDSyncLog(System.Web.HttpUtility.UrlDecode(q));
        }

        [HttpGet, Route("searchdmdwithallnodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DMDSearchResultsWithHierarchyDTO>> SearchDMDNamesGetWithAllLevelNodes([FromQuery] string q)
        {
            if (q.IsEmpty()) return BadRequest();

            var results = await this._dmdQueries.SearchDMDNamesGetWithAllLevelNodes(System.Web.HttpUtility.UrlDecode(q));
            return Ok(results);
        }

        [HttpPost, Route("getdmddescendent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DMDSearchResultWithTreeDTO>> GetDMDDescendentForCodes([FromBody] string[] codes)
        {
            return await this._dmdQueries.GetDMDDescendentForCodes(codes);
        }

        [HttpPost, Route("getdmdancestor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<List<DMDSearchResultWithTreeDTO>> GetDMDAncestorForCodes([FromBody] string[] codes)
        {
            return await this._dmdQueries.GetDMDAncestorForCodes(codes);
        }

        [HttpGet, Route("searchdmdwithtopnodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<DMDSearchResultsWithHierarchyDTO> SearchDMDNamesWithTopNodes([FromQuery] string q)
        {
            return await this._dmdQueries.SearchDMDNamesGetWithTopNodes(System.Web.HttpUtility.UrlDecode(q));
        }


        [HttpGet, Route("getalldmdcodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetAllDMDCodes()
        {
            return await new TaskFactory<ActionResult>().StartNew(() =>
            {
                var results = this._dmdQueries.GetAllDMDCodes();

                if (!results.IsCollectionValid()) return NoContent();

                return Ok(results);
            });
        }

        [HttpGet, Route("getdmdsnomedversion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetDmdSnomedVersion()
        {
            return await new TaskFactory<ActionResult>().StartNew(() =>
            {
                var results = this._dmdQueries.GetDmdSnomedVersion();

                if (results.IsNull()) return NoContent();

                return Ok(results);
            });
        }

        [HttpPost, Route("getdmdfulldataforcodes")]
        [RequestSizeLimit(long.MaxValue)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DMDDetailResultDTO>>> GetDMDFullDataForCodes(List<string> codes)
        {
            if (!codes.IsCollectionValid() || codes.Any(rec=> rec.IsEmpty())) return BadRequest("Codes are empty.");

            var results = await this._dmdQueries.GetDMDFullDataForCodes(codes.ToArray());

            if (results.IsNull()) return NoContent();

            return Ok(results);
        }

        [HttpPost, Route("getampexcipientsforcodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DmdAmpExcipientDTO>>> GetAMPExcipientsForCodes(List<string> dmdCodes)
        {
            if (!dmdCodes.IsCollectionValid()) return BadRequest("Missing input parameter");

            return await new TaskFactory<ActionResult<List<DmdAmpExcipientDTO>>>().StartNew(() =>
            {
                var results = this._dmdQueries.GetAMPExcipientsForCodes(dmdCodes);

                if (results.IsNull()) return NoContent();

                return Ok(results);
            });
        }

        [HttpPost, Route("getampdrugroutesforcodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DmdAmpDrugrouteDTO>>> GetAMPDrugRoutesForCodes(List<string> dmdCodes)
        {
            if (!dmdCodes.IsCollectionValid()) return BadRequest("Missing input parameter");

            return await new TaskFactory<ActionResult<List<DmdAmpDrugrouteDTO>>>().StartNew(() =>
            {
                var results = this._dmdQueries.GetAMPDrugRoutesForCodes(dmdCodes);

                if (results.IsNull()) return NoContent();

                return Ok(results);
            });
        }

        [HttpPost, Route("getvmpdrugroutesforcodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DmdVmpDrugrouteDTO>>> GetVMPDrugRoutesForCodes(List<string> dmdCodes)
        {
            if (!dmdCodes.IsCollectionValid()) return BadRequest("Missing input parameter");

            return await new TaskFactory<ActionResult<List<DmdVmpDrugrouteDTO>>>().StartNew(() =>
            {
                var results = this._dmdQueries.GetVMPDrugRoutesForCodes(dmdCodes);

                if (results.IsNull()) return NoContent();

                return Ok(results);
            });
        }

        [HttpGet, Route("gettopdmdsynclog")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DmdSyncLog>> GetTopDMDSyncLog()
        {
            return await new TaskFactory<ActionResult<DmdSyncLog>>().StartNew(() =>
            {
                var results = this._dmdQueries.GetDMDSyncLogs();

                if (!results.IsCollectionValid()) return NoContent();

                return Ok(results.First());
            });
        }


        [HttpGet, Route("getdmdpendingsynclogs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DmdSyncLog>>> GetDMDPendingSyncLogs()
        {
            return await new TaskFactory<ActionResult<List<DmdSyncLog>>>().StartNew(() =>
            {
                var results = this._dmdQueries.GetDMDPendingSyncLogs();

                if (results.IsNull()) return NoContent();

                return Ok(results);
            });
        }

        [HttpGet, Route("getdmdpendingsynclogsbypagination/{pageNo}/{pageSize}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DmdSyncLog>>> GetDMDPendingSyncLogsByPagination(int pageNo, int pageSize)
        {
            if (pageNo == 0 || pageSize == 0) return BadRequest("Page Number and PageSize should be more than 0");

            return await new TaskFactory<ActionResult<List<DmdSyncLog>>>().StartNew(() =>
            {
                var results = this._dmdQueries.GetDMDPendingSyncLogsByPagination(pageNo, pageSize);

                if (results.IsNull()) return NoContent();

                return Ok(results);
            });
        }

        [HttpPost, Route("updateformularysyncstatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateFormularySyncStatus(List<string> dmdCodes)
        {
            if (!dmdCodes.IsCollectionValid()) return BadRequest("Missing input parameter");

            return await new TaskFactory<ActionResult>().StartNew(() =>
            {
                _dmdCommand.UpdateFormularySyncStatus(dmdCodes);

                return Ok();
            });
        }

        [HttpPost, Route("updateformularysyncstatusforallrecords")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateFormularySyncStatusForAllRecords()
        {
            return await new TaskFactory<ActionResult>().StartNew(() =>
            {
                _dmdCommand.UpdateFormularySyncStatusForAllRecords();

                return Ok();
            });
        }
    }
}
