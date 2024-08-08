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
    public class HistoryOfFormulariesRequest
    {
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public List<KeyValuePair<string, string>> FilterParamsAsKV { get; set; }
        public string FilterParams { get; set; }
        public bool NeedTotalRecords { get; set; }
    }
}
