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
﻿namespace Interneuron.Terminology.API.AppCode.DTOs
{
    public partial class FormularyChangeLogDTO : TerminologyResource
    {
        public string? Code { get; set; }
        public string? FormularyId { get; set; }
        public string? Name { get; set; }
        public string? ProductType { get; set; }
        public string? ParentCode { get; set; }
        public string? EntitiesCompared { get; set; }
        public bool? HasProductInvalidFlagChanged { get; set; }
        public string? ProductInvalidChanges { get; set; }
        public bool? HasProductDeletedChanged { get; set; }
        public string? ProductDeletedChanges { get; set; }
        public bool? HasProductDetailChanged { get; set; }
        public string? ProductDetailChanges { get; set; }
        public bool? HasProductPosologyChanged { get; set; }
        public string? ProductPosologyChanges { get; set; }
        public bool? HasProductGuidanceChanged { get; set; }
        public string? ProductGuidanceChanges { get; set; }
        public bool? HasProductFlagsChanged { get; set; }
        public string? ProductFlagsChanges { get; set; }
        public string? DeltaDetail { get; set; }

    }
}
