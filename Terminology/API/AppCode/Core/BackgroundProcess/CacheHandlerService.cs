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
﻿using Interneuron.Caching;
using Interneuron.Common.Extensions;
using Interneuron.Terminology.API.AppCode.Infrastructure.Caching;
using Interneuron.Terminology.API.AppCode.Queries;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Interneuron.Terminology.Model.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Core.BackgroundProcess
{
    public class CacheHandlerService
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private IConfiguration _configuration;

        public CacheHandlerService(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
        }

        public void CacheActiveFormularyDetailRuleBound(List<string> codes, bool cacheAllActive = false)
        {
            var shouldRefreshCache = false;
            var terminologyAPISection = _configuration.GetSection("TerminologyConfig");

            if (terminologyAPISection != null)
            {
                var shouldRefreshCacheConfigVal = terminologyAPISection.GetValue<string>("shouldRefreshCache");
                if (shouldRefreshCacheConfigVal.IsNotEmpty())
                    shouldRefreshCache = shouldRefreshCacheConfigVal == "1";
            }

            if (!shouldRefreshCache) return;

            var cacheSection = _configuration.GetSection("cache_service_api");
            var cacheProcessor = cacheSection.GetValue<string>("cache_processor") ?? "inproc";

            if(string.Compare(cacheProcessor, "outproc", true) == 0)
            {
                CacheActiveFormularyDetailRuleBoundExternally(codes, cacheAllActive);
                return;
            }

            CacheActiveFormularyDetailRuleBoundInProc(codes, cacheAllActive);
        }

        public void CacheActiveFormularyDetailRuleBoundExternally(List<string> codes, bool cacheAllActive = false)
        {
            var cacheSection = _configuration.GetSection("cache_service_api");

            var cacheServiceAPIUrl = cacheSection.GetValue<string>("active_formulary_url");

            if (!cacheAllActive && !codes.IsCollectionValid())
            {
                return;
            }

            //Invoke and forget but check whether accepted
            _ = Task.Run(async () =>
            {
                try
                {
                    Log.Logger.Error($"Info: Active Formulary Cache API invocation Started"); //only error severity is configured for logging.

                    Log.Logger.Information("Active Formulary Cache API invocation Started");

                    //Not requuired for now
                    //var accessToken = await GetAccessToken();
                    //if (accessToken == null) return null;

                    using var client = new RestClient(cacheServiceAPIUrl);

                    var request = new RestRequest() { Method = Method.Post, Timeout = -1 };
                    //request.AddHeader("Authorization", $"Bearer {accessToken}");

                    request.AddHeader("Content-Type", "application/json");

                    request.AddJsonBody(codes ?? new List<string>());

                    var response = await client.ExecuteAsync(request);

                    if (response == null || (response.StatusCode != System.Net.HttpStatusCode.Accepted && response.StatusCode != System.Net.HttpStatusCode.OK))
                    {
                        Log.Logger.Error($"Info: Error invoking Active Formulary Cache API invocation");
                        return;
                    }

                    Log.Logger.Error($"Info: Active Formulary Cache API invocation successfully Complete");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Info: Exception invoking Active Formulary Cache API invocation");
                    Log.Logger.Error(ex, ex.ToString());
                }
            });
        }

        //This will work as sigleton as background job - resource extensive

        public void CacheActiveFormularyDetailRuleBoundInProc(List<string> codes, bool cacheAllActive = false)
        //public void CacheActiveFormularyDetailRuleBoundAsBackground(List<string> codes, bool cacheAllActive = false)
        {
            if (!cacheAllActive && (codes == null || codes.Count == 0))
            {
                return;
            }

            int batchsize = 50;//from config

            List<List<string>> batches = new List<List<string>>();

            List<string> currentActiveRecsForCodes = new List<string>();

            List<DTOs.FormularyDTO> results = new List<DTOs.FormularyDTO>();


            _ = Task.Run(async () =>
            {
                try
                {
                    if (codes.IsCollectionValid())
                    {
                        var keys = new List<string>();

                        foreach (var item in codes)
                        {
                            if (!string.IsNullOrEmpty(item))
                                keys.Add($"{CacheKeys.ACTIVE_FORMULARY}{item}");
                        }

                        await CacheService.RemoveKeysAsync(keys);
                    }
                    else if (cacheAllActive)
                    {
                        await CacheService.FlushByKeyPatternAsync(CacheKeys.ACTIVE_FORMULARY);
                    }

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var serviceProvider = scope.ServiceProvider;

                        var repo = serviceProvider.GetRequiredService<IFormularyRepository<FormularyHeader>>();

                        Log.Logger.Error($"Info: Active Formulary Cache Reload Started"); //only error severity is configured for logging.

                        Log.Logger.Information("Active Formulary Cache Reload Started");

                        var formularyQueries = serviceProvider.GetRequiredService<IFormularyQueries>();

                        var basicRepo = serviceProvider.GetRequiredService<IFormularyRepository<FormularyBasicSearchResultModel>>();

                        currentActiveRecsForCodes = repo.ItemsAsReadOnly.Where(rec => rec.IsLatest == true && rec.RecStatusCode == TerminologyConstants.RECORDSTATUS_ACTIVE && (codes == null || codes.Contains(rec.Code)))?.Select(rec => rec.Code)?.ToList();
                    }

                    if (!currentActiveRecsForCodes.IsCollectionValid()) return;

                    for (int codeIndex = 0; codeIndex < currentActiveRecsForCodes.Count; codeIndex += batchsize)
                    {
                        var batch = currentActiveRecsForCodes.Skip(codeIndex).Take(batchsize);
                        batches.Add(batch.ToList());
                    }

                    Log.Logger.Error($"Info: Active Formulary Cache Started Fetching Data. Total Batches: {batches.Count}");


                    foreach (var batch in batches)
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var serviceProvider = scope.ServiceProvider;

                            var repo = serviceProvider.GetRequiredService<IFormularyRepository<FormularyHeader>>();

                            var formularyQueries = serviceProvider.GetRequiredService<IFormularyQueries>();

                            results = await formularyQueries.GetActiveFormularyDetailRuleBoundByCodes(batch, false);
                        }

                        Log.Logger.Error($"Info: batch results received from database");

                        if (!results.IsCollectionValid())
                        {
                            Log.Logger.Error($"Info: No batch results received from database");
                            return;
                        }

                        //MGET not supported in cluster
                        //var kv = results.ToDictionary(k => $"{CacheKeys.ACTIVE_FORMULARY}{k.Code}", v => v);
                        //CacheService.Set(kv);

                        var kv = results.ToDictionary(k => $"{CacheKeys.ACTIVE_FORMULARY}{k.Code}", v => v);
                        await CacheService.SetAsync(kv);
                    }

                    Log.Logger.Error($"Info: Active Formulary Cache Reload Complete");

                    Log.Logger.Information("Active Formulary Cache Reload Complete");

                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, ex.ToString());
                }

            });

        }

    }
}
