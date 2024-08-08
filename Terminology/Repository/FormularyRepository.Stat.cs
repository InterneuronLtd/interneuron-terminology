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
﻿using Dapper;
using Interneuron.Common.Extensions;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Interneuron.Terminology.Repository
{
    public partial class FormularyRepository<TEntity> 
    {
        /// <summary>
        /// Search for the Formulary usage stat for the search term
        /// </summary>
        /// <param name="filterCriteria"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TEntity>> GetFormularyUsageStatsForSearchTerm(string searchTerm, Dictionary<string, object> filterMeta)
        {
            IEnumerable<FormularyUsageStat> results;

            if (searchTerm.IsEmpty()) return null;

            string tokenToSearch = null;
            string codeToSearch = null;

            var isSearchForCodeAsLong = long.TryParse(searchTerm, out long searchCodeAsLong);
            var isSearchForCodeAsGuid = Guid.TryParse(searchTerm, out Guid searchCodeAsGuid);//In case of custom formularies

            if (isSearchForCodeAsLong || isSearchForCodeAsGuid)
            {
                codeToSearch = isSearchForCodeAsLong ? searchCodeAsLong.ToString() : searchCodeAsGuid.ToString();
            }
            else
            {
                TerminologyConstants.PG_ESCAPABLE_CHARS.Each(escRec => searchTerm = searchTerm.Replace(escRec, $"\\{escRec}"));
                var searchTermTokens = searchTerm.Split(" ");
                searchTermTokens.Each(s => { if (s.IsNotEmpty()) tokenToSearch = $"{tokenToSearch} & {s}:*"; });
                tokenToSearch = $"%{searchTerm}%";// tokenToSearch.TrimStart(' ', '&');//use like search for now
            }

            //            var qryStmt = @$"SELECT name, code, full_name, formulary_id, usage_count as usagecount FROM local_formulary.formulary_usage_stats
            //WHERE name_tokens @@ to_tsquery(@in_name) or code = @in_search_code 
            //AND (now()::date - _updatedtimestamp::date) <= @in_within_days
            //ORDER BY usage_count desc;";

            var qryStmt = @$"SELECT name, code, full_name, formulary_id, usage_count as usagecount FROM local_formulary.formulary_usage_stats
WHERE name ilike @in_name or code = @in_search_code 
AND (now()::date - _updatedtimestamp::date) <= @in_within_days
ORDER BY usage_count desc;";

            var connString = _configuration.GetValue<string>("TerminologyConfig:Connectionstring");
            
            var statFetchTimeframeInDays = 60l;

            if (filterMeta.IsCollectionValid() && filterMeta.ContainsKey("statFetchTimeframeInDays") 
                && long.TryParse(filterMeta["statFetchTimeframeInDays"].ToString(), out long inDays))
                statFetchTimeframeInDays = inDays;

            using (var conn = new Npgsql.NpgsqlConnection(connString))
            {
                results = await conn.QueryAsync<FormularyUsageStat>(qryStmt, new { in_name = tokenToSearch, in_search_code = codeToSearch, in_within_days = statFetchTimeframeInDays });
            }

            if (results == null) return null;

            return (IEnumerable<TEntity>)results;
        }

        public async Task<IEnumerable<TEntity>> GetFormularyUsageStatsForCodes(List<string> codes, Dictionary<string, object> filterMeta)
        {
            IEnumerable<FormularyUsageStat> results = null;

            if (!codes.IsCollectionValid()) return null;

            var qryStmt = @$"SELECT name, code, full_name, formulary_id, usage_count as usagecount FROM local_formulary.formulary_usage_stats
WHERE code = any(@in_search_code)
AND (now()::date - _updatedtimestamp::date) <= @in_within_days
ORDER BY usage_count;";

            var connString = _configuration.GetValue<string>("TerminologyConfig:Connectionstring");

            var statFetchTimeframeInDays = 60l;

            if (filterMeta.IsCollectionValid() && filterMeta.ContainsKey("statFetchTimeframeInDays")
                && long.TryParse(filterMeta["statFetchTimeframeInDays"].ToString(), out long inDays))
                statFetchTimeframeInDays = inDays;

            using (var conn = new Npgsql.NpgsqlConnection(connString))
            {
                results = await conn.QueryAsync<FormularyUsageStat>(qryStmt, new { in_search_code = codes, in_within_days = statFetchTimeframeInDays  });
            }

            if (results == null) return null;

            return (IEnumerable<TEntity>)results;
        }
    }
}
