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
﻿using System.Collections.Generic;

namespace Interneuron.Terminology.API.AppCode.DTOs
{
    public class ActiveFormularyBasicDTO : TerminologyResource, IComposeAdditionalCodes, IFormularyDTO
    {
        public string FormularyId { get; set; }
        public int? VersionId { get; set; }
        public string FormularyVersionId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ProductType { get; set; }
        public string ParentCode { get; set; }
        public string ParentName { get; set; }
        public string ParentProductType { get; set; }
        public string RecStatusCode { get; set; }

        public bool? IsLatest { get; set; }
        public string RecSource { get; set; }
        public string VtmId { get; set; }
        public string VmpId { get; set; }
        public string CodeSystem { get; set; }

        public List<FormularyAdditionalCodeDTO> FormularyAdditionalCodes { get; set; }

        public ActiveFormularyBasicDetailDTO Detail { get; set; }
    }

    public interface IComposeAdditionalCodes
    {
        List<FormularyAdditionalCodeDTO> FormularyAdditionalCodes { get; set; }

    }
}
