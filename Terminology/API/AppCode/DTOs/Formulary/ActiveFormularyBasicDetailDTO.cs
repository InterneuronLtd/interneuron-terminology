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
﻿using Interneuron.Terminology.API.AppCode.DTOs.Formulary;
using System.Collections.Generic;

namespace Interneuron.Terminology.API.AppCode.DTOs
{
    public class ActiveFormularyBasicDetailDTO : TerminologyResource
    {
        public string FormularyVersionId { get; set; }

        public string RnohFormularyStatuscd { get; set; }
        public string RnohFormularyStatusDesc { get; set; }
        
        
        public List<string> MedusaPreparationInstructions { get; set; }
        
        public List<FormularyLookupItemDTO> TitrationTypes { get; set; }

        
        public List<FormularyCustomWarningDTO> CustomWarnings { get; set; }
        public List<FormularyReminderDTO> Reminders { get; set; }
        public List<string> Endorsements { get; set; }

        public List<FormularyLookupItemDTO> LicensedUses { get; set; }

        public List<FormularyLookupItemDTO> UnLicensedUses { get; set; }

        public List<FormularyLookupItemDTO> LocalLicensedUses { get; set; }

        public List<FormularyLookupItemDTO> LocalUnLicensedUses { get; set; }
        public string IgnoreDuplicateWarnings { get; set; }

    }
}
