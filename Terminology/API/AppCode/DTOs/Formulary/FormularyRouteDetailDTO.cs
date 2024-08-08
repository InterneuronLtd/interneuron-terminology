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
﻿using Interneuron.Terminology.API.AppCode.DTOs;
using System;
using System.Collections.Generic;

namespace Interneuron.Terminology.API.AppCode.DTOs
{
    public partial class FormularyRouteDetailDTO : TerminologyResource
    {
        public string RowId { get; set; }
        public DateTime? Createddate { get; set; }
        public string Createdby { get; set; }
        public DateTime? Updateddate { get; set; }
        public string Updatedby { get; set; }
        public string FormularyVersionId { get; set; }
        public string RouteCd { get; set; }
        public string RouteDesc { get; set; }
        public string RouteFieldTypeCd { get; set; }
        public string RouteFieldTypeDesc { get; set; }
        public string Source { get; set; }
    }
}
