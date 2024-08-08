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
﻿namespace Interneuron.Terminology.BackgroundTask.API.AppCode.DTOs
{
    public class BackgroundTaskDTO
    {
        public string? TaskId { get; set; }
        public string Name { get; set; }
        public short StatusCd { get; set; }
        public string? Status { get; set; }
        public string? Detail { get; set; }
        public DateTime? Createdtimestamp { get; set; }
        public DateTime? Createddate { get; set; }
        public string? Createdby { get; set; }
        public DateTime? Updatedtimestamp { get; set; }
        public DateTime? Updateddate { get; set; }
        public string? Updatedby { get; set; }
        public string? CorrelationTaskId { get; set; }
    }
}
