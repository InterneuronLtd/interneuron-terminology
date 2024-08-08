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
    public partial class DmdNamesLookupMat : Interneuron.Terminology.Infrastructure.Domain.EntityBase
    {
        public string Name { get; set; }
        public NpgsqlTsVector NameTokens { get; set; }
        public string Code { get; set; }
        public string VmprouteCode { get; set; }
        public string Vmproute { get; set; }
        public string VmpformCode { get; set; }
        public string Vmpform { get; set; }
        public string SupplierCode { get; set; }
        public string Supplier { get; set; }
        public NpgsqlTsVector SupplierNameTokens { get; set; }
        public long? PrescribingStatusCode { get; set; }
        public string PrescribingStatus { get; set; }
        public long? ControlDrugCategoryCode { get; set; }
        public string ControlDrugCategory { get; set; }
        public short? Invalid { get; set; }

    }
}
