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
using Interneuron.Terminology.API.AppCode.DTOs.Formulary.Requests;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Commands
{
    public partial class FormularyCommand
    {
        public async Task<StatusDTO> SaveFormularyUsageStat(List<SaveFormularyUsageStatRequest> request)
        {
            var status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS };

            if (request == null)
            {
                status.ErrorMessages = new List<string> { "Invalid usage data" };
                status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;//error
                return status;
            }

            foreach (var item in request)
            {
                if (item.Name.IsEmpty() || item.Code.IsEmpty())
                {
                    status.ErrorMessages = new List<string> { "Missing required usage data" };
                    status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;//error
                    return status;
                }
            }

            var codes = new List<string>(); // request.Select(rec => rec.Code)?.ToList();
            //If input itself has duplicate codes, then take the count of those
            var codeCntLk = new Dictionary<string, long>();
            request.Each(rec =>
            {
                codes.Add(rec.Code);
                if (codeCntLk.ContainsKey(rec.Code))
                    codeCntLk[rec.Code]++;
                else
                    codeCntLk[rec.Code] = 1;
            });

            var usageStatRepo = _provider.GetService(typeof(IFormularyRepository<FormularyUsageStat>)) as IFormularyRepository<FormularyUsageStat>;

            var usageStats = usageStatRepo.Items.Where(rec => codes.Contains(rec.Code)).ToList();
            //var usageStats = await usageStatRepo.GetFormularyUsageStatsForCodes(codes);
            var existingUsageStat = new Dictionary<string, FormularyUsageStat>();

            if (usageStats.IsCollectionValid())
            {
                existingUsageStat = usageStats.Select(rec => new { code = rec.Code, rec = rec })?.Distinct(rec => rec.code)?.ToDictionary((k) => k.code, v => v.rec);
            }

            var uniqueRequests = request.Select(rec => rec).Distinct(rec => rec.Code).ToList();

            foreach (var item in uniqueRequests)
            {
                if (existingUsageStat.ContainsKey(item.Code))
                {
                    var existing = existingUsageStat[item.Code];
                    //existing.UsageCount = existing.UsageCount + 1;
                    existing.UsageCount = existing.UsageCount + (codeCntLk.ContainsKey(item.Code) ? codeCntLk[item.Code] : 1);
                    usageStatRepo.Update(existing);
                }
                else
                {
                    var dao = _mapper.Map<FormularyUsageStat>(item);
                    dao.UsageCount = codeCntLk.ContainsKey(item.Code) ? codeCntLk[item.Code] : 1;
                    usageStatRepo.Add(dao);
                }
            }

            usageStatRepo.SaveChanges();

            return status;
        }

        public async Task<StatusDTO> SeedFormularyUsageStatForEPMA(List<FormularyUsageStatDTO> dtos)
        {
            var status = new StatusDTO { StatusCode = TerminologyConstants.STATUS_SUCCESS };

            if (dtos == null)
            {
                status.ErrorMessages = new List<string> { "Invalid usage data" };
                status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;//error
                return status;
            }

            foreach (var item in dtos)
            {
                if (item.Name.IsEmpty() || item.Code.IsEmpty())
                {
                    status.ErrorMessages = new List<string> { "Missing required usage data" };
                    status.StatusCode = TerminologyConstants.STATUS_BAD_REQUEST;//error
                    return status;
                }
            }
            var usageStatRepo = _provider.GetService(typeof(IFormularyRepository<FormularyUsageStat>)) as IFormularyRepository<FormularyUsageStat>;

            var data = _mapper.Map<List<FormularyUsageStat>>(dtos);

            data.Each(rec => usageStatRepo.Add(rec));

            usageStatRepo.SaveChanges();

            return status;
        }
    }
}
