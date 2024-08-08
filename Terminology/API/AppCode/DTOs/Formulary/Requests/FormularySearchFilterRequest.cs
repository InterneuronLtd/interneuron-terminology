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
    public class FormularySearchFilterRequest: ITerminologyRequest
    {
        public string SearchTerm { get; set; }

        public bool? HideArchived { get; set; } = true;

        public List<string> RecStatusCds { get; set; }

        public List<string> FormularyStatusCd { get; set; }

        public CategoryDiffenceFilter? CategoryDifference { get; set; }

        public List<string> Flags { get; set; }

        public bool? ShowOnlyDuplicate { get; set; }

        public bool? IncludeDeleted { get; set; } = false;

        public string Source { get; set; } = null;

        public string? ProductType { get; set; }
        public bool IncludeChangeDetails { get; set; } = false;
        public bool? IncludeInvalid { get; set; } = false;

    }

    public record CategoryDiffenceFilter
    {
        public bool? IsDetailChanged { get; set; }
        public bool? IsPosologyChanged { get; set; }
        public bool? IsGuidanceChanged { get; set; }
        public bool? IsFlagsChanged { get; set; }
        public bool? IsInvalid { get; set; }
        public bool? IsDeleted { get; set; }

    }
}
