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
using NpgsqlTypes;

namespace Interneuron.Terminology.Model.DomainModels
{
    public partial class SnomedctTradefamiliesMat : Interneuron.Terminology.Infrastructure.Domain.EntityBase
    {
        public string BrandedDrugId { get; set; }
        public string BrandedDrugTerm { get; set; }
        public NpgsqlTsVector BrandedDrugTermTokens { get; set; }
        public string TradeFamilyId { get; set; }
        public string TradeFamilyTerm { get; set; }
        public NpgsqlTsVector TradeFamilyTermTokens { get; set; }
    }
}
