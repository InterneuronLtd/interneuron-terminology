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
﻿using Interneuron.Common.Extensions;
using Interneuron.Terminology.API.AppCode.DTOs;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Queries
{
    public partial class FormularyQueries: IFormularyQueries
    {
        public async Task<List<FormularyUsageStatDTO>> GetFormulariesUsageStatByPrefix(string prefixText, Dictionary<string, object> filterMeta)
        {
            if(prefixText.IsEmpty()) return null;

            var usageStatRepo = _provider.GetService(typeof(IFormularyRepository<FormularyUsageStat>)) as IFormularyRepository<FormularyUsageStat>;

            var usageStats = await usageStatRepo.GetFormularyUsageStatsForSearchTerm(prefixText, filterMeta);

            if (!usageStats.IsCollectionValid()) return null;

            var dtos = _mapper.Map<List<FormularyUsageStatDTO>>(usageStats);

            return dtos;
        }

        public async Task<List<FormularyUsageStatDTO>> GetFormulariesUsageStatByCodes(List<string> codes, Dictionary<string, object> filterMeta)
        {
            if (!codes.IsCollectionValid()) return null;

            codes = codes.Distinct(rec=> rec).ToList();

            var usageStatRepo = _provider.GetService(typeof(IFormularyRepository<FormularyUsageStat>)) as IFormularyRepository<FormularyUsageStat>;

            var usageStats = await usageStatRepo.GetFormularyUsageStatsForCodes(codes, filterMeta);

            if (!usageStats.IsCollectionValid()) return null;

            var dtos = _mapper.Map<List<FormularyUsageStatDTO>>(usageStats);

            return dtos;
        }
    }
}
