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
using Interneuron.Terminology.BackgroundTask.API.AppCode.Core;
using Interneuron.Terminology.BackgroundTask.API.AppCode.DTOs;
using Interneuron.Terminology.BackgroundTask.API.AppCode.Infrastructure;
using Interneuron.Terminology.BackgroundTask.API.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Interneuron.Terminology.BackgroundTask.API.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class BackgroundTaskController : ControllerBase
    {
        private APIRequestContext _apiRequestContext;
        private BackgroundTaskRepositoryUtil _backgroundTaskRepositoryUtil;
        private IMapper _mapper;

        public BackgroundTaskController(APIRequestContext apiRequestContext, BackgroundTaskRepositoryUtil backgroundTaskRepositoryUtil, IMapper mapper)
        {
            _apiRequestContext = apiRequestContext;
            _backgroundTaskRepositoryUtil = backgroundTaskRepositoryUtil;
            _mapper = mapper;
        }

        [HttpGet, Route("{taskName}/statusCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<BackgroundTaskDTO>>> GetTaskByName(string taskName, short? statusCode = null)
        {
            if (taskName.IsEmpty()) return BadRequest("Need to provide taskName");

            var task = await _backgroundTaskRepositoryUtil.GetTasksByNameAndStatus(taskName, statusCode);
            if (task == null) return NoContent();
            var taskDTO = _mapper.Map<List<BackgroundTaskDTO>>(task);
            return Ok(taskDTO);
        }

        [HttpGet, Route("[Action]/{taskId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BackgroundTaskDTO>> GetTaskByTaskId(string taskId)
        {
            if (taskId.IsEmpty()) return BadRequest("Need to provide TaskId");

            var task = await _backgroundTaskRepositoryUtil.GetTaskByTaskId(taskId);
            if (task == null) return NoContent();
            var taskDTO = _mapper.Map<BackgroundTaskDTO>(task);
            return Ok(taskDTO);
        }

        [HttpGet, Route("[Action]/{correlationTaskId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<BackgroundTaskDTO>>> GetTasksByCorrelationTaskId(string correlationTaskId)
        {
            if (correlationTaskId.IsEmpty()) return BadRequest("Need to provide CorrelationTaskId");

            var tasks = await _backgroundTaskRepositoryUtil.GetTasksByCorrelationTaskId(correlationTaskId);
            if (!tasks.IsCollectionValid()) return NoContent();
            var taskDTO = _mapper.Map<List<BackgroundTaskDTO>>(tasks);
            return Ok(taskDTO);
        }

        [HttpPost("[Action]/{statusCode?}")]
        public async Task<ActionResult<List<BackgroundTaskDTO>>> GetTaskByNames(List<string> taskNames, [FromRoute] short? statusCode)
        {
            if (!taskNames.IsCollectionValid() || taskNames.Any(rec=> rec.IsEmpty())) return BadRequest("Need to provide taskName");

            var tasks = await _backgroundTaskRepositoryUtil.GetTaskByNamesAndStatus(taskNames, statusCode);
            if (!tasks.IsCollectionValid()) return NoContent();
            var taskDTO = _mapper.Map<List<BackgroundTaskDTO>>(tasks);
            return Ok(taskDTO);
        }

        [HttpPost("[Action]")]
        public async Task<ActionResult<List<BackgroundTaskDTO>>> GetTaskByNamesAndUpdateStatus(GetTaskByNamesAndUpdateStatusRequestDTO request)
        {
            if (request == null || !request.TaskNamesToLookFor.IsCollectionValid() || request.TaskNamesToLookFor.Any(rec => rec.IsEmpty())) 
                return BadRequest("Need to provide taskName");

            var tasks = await _backgroundTaskRepositoryUtil.GetTaskByNamesWithStatusAndUpdateStatus(request.TaskNamesToLookFor, request.StatusCodeToLookFor, request.StatusCdToUpdate, request.StatusToUpdate, request.DetailToUpdate);

            if (!tasks.IsCollectionValid()) return NoContent();
            
            var taskDTO = _mapper.Map<List<BackgroundTaskDTO>>(tasks);
            
            return Ok(taskDTO);
        }

        [HttpPost]
        public async Task<ActionResult<BackgroundTaskDTO>> CreateTask(BackgroundTaskDTO request)
        {
            if (request == null || request.Status.IsEmpty()) return BadRequest();

            request.Createdby = _apiRequestContext?.APIUser?.UserId;
            request.Updatedby = _apiRequestContext?.APIUser?.UserId;
            request.TaskId = Guid.NewGuid().ToString();
            //request.CorrelationTaskId = request.CorrelationTaskId ?? request.TaskId;

            var model = _mapper.Map<Models.BackgroundTask>(request);

            var hasCreated = await _backgroundTaskRepositoryUtil.CreateBackgroundTask(model);
            if (!hasCreated) StatusCode(500, "Unable to create task.");

            return Ok(request);
        }

        [HttpPut]
        public async Task<ActionResult<BackgroundTaskDTO>> UpdateTask(BackgroundTaskDTO request)
        {
            if(request == null || request.TaskId.IsEmpty()) return BadRequest("Task Id cannot be empty");
            request.Updatedby = _apiRequestContext?.APIUser?.UserId;

            var model = _mapper.Map<Models.BackgroundTask>(request);
            var hasUpdated = await _backgroundTaskRepositoryUtil.UpdateTask(model);
            if (!hasUpdated) StatusCode(500, "Unable to create task.");
            return Ok(request);
        }
    }
}
