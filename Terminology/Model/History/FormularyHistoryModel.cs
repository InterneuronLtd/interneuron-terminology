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
﻿using Interneuron.Terminology.Infrastructure.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interneuron.Terminology.Model.History
{
    public class FormularyHistoryModel : EntityBase
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ProductType { get; set; }
        public string Status { get; set; }
        public string DateTime { get; set; }
        public string User { get; set; }
        public string? FormularyId { get; set; }
        public int? VersionId { get; set; }
        public string? RecStatusCode { get; set; }

        public string PreviousFormularyVersionId { get; set; }
        public string CurrentFormularyVersionId { get; set; }
    }

    public class FormularyHistoryPaginatedModel : EntityBase
    {
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<FormularyHistoryModel> Items { get; set; }
    }
}
