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
using Interneuron.Common.Extensions;
using Interneuron.Terminology.CacheService.Config;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using Microsoft.CSharp.RuntimeBinder;

namespace Interneuron.Terminology.CacheService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheServiceController : ControllerBase
    {
        private IConfiguration _configuration;
        private FormularyConfig _formularyCacheConfig;

        public CacheServiceController(IConfiguration configuration)
        {
            _configuration = configuration;
            _formularyCacheConfig = configuration.GetSection("terminology_ep").Get<FormularyConfig>();
        }

        //    [Route("dummy")]
        //    [HttpGet]
        //    public async Task<ActionResult> CacheActiveFormulariesForCodes()
        //    {
        //        var z = 222;
        //        _ = Task.Run(async () =>
        //        {
        //            await Task.Delay(10000);

        //            var x = 123;

        //            //using (var scope = serviceScopeFactory.CreateScope())
        //            //{
        //            //    var context = scope.ServiceProvider.GetRequiredService<ContosoDbContext>();

        //            //    context.Contoso.Add(new Contoso());

        //            //    await context.SaveChangesAsync();
        //            //}
        //        });

        //        return Accepted();
        //    }

        [Route("cacheactiveformularies")]
        [HttpPost]
        public async Task<ActionResult> CacheActiveFormularies([FromBody] List<string> codes = null)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    const string ACTIVE_FORMULARY_CACHE_KEY_NAME = "activeformulary:";

                    int batchsize = _formularyCacheConfig.BatchSize;//from config

                    if (codes.IsCollectionValid())
                    {
                        var keys = new List<string>();

                        foreach (var item in codes)
                        {
                            if (!string.IsNullOrEmpty(item))
                                keys.Add($"{ACTIVE_FORMULARY_CACHE_KEY_NAME}{item}");
                        }                        

                        await Interneuron.Caching.CacheService.RemoveKeysAsync(keys);

                    }
                    else
                    {
                        await Interneuron.Caching.CacheService.FlushByKeyPatternAsync($"{ACTIVE_FORMULARY_CACHE_KEY_NAME}");
                    }

                    var activeCodes = await GetActiveFormularyCodes(codes);

                    if (!activeCodes.IsCollectionValid())
                    {
                        Log.Logger.Error($"Info: no batch results received from database");
                        return;
                    }


                    var batches = new List<List<string>>();

                    for (int codeIndex = 0; codeIndex < activeCodes.Count; codeIndex += batchsize)
                    {
                        var batch = Enumerable.Skip<string>(activeCodes, codeIndex).Take<string>(batchsize);
                        batches.Add(batch.ToList<string>());
                    }

                    await ProcessActiveCodesInBatches(ACTIVE_FORMULARY_CACHE_KEY_NAME, batches);
                });

                return Accepted();
            }
            catch (Exception ex)
            {
                //log
                Log.Logger.Error($"Error: Unable to cache the data due to exception {ex}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
           
        }

        private async Task ProcessActiveCodesInBatches(string ACTIVE_FORMULARY_CACHE_KEY_NAME, List<List<string>> batches)
        {
            var exceptions = new ConcurrentQueue<Exception>();

            var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };
            //foreach (var batch in batches)
            await Parallel.ForEachAsync(batches, options, async (batch, token) =>
            {
                try
                {
                    //var get active results from terminology api
                    var results = await GetActiveFormularies(batch);

                    Log.Logger.Error($"Info: batch results received from database");

                    if (results.IsCollectionValid())
                    {
                        //var dataToCache = new ConcurrentDictionary<string, object>();

                        //Parallel.ForEach<dynamic>(results, (rec) =>
                        //{
                        //    if (rec is not null && IsPropertyExists(rec, "code"))
                        //    {
                        //        var code = Convert.ToString(rec.code);

                        //        if (!string.IsNullOrEmpty(code))
                        //            dataToCache.TryAdd($"{ACTIVE_FORMULARY_CACHE_KEY_NAME}{code}", rec);
                        //    }
                        //});

                        var dataToCache = new Dictionary<string, object>();

                        foreach (var item in results)
                        {
                            if (item is not null && IsPropertyExists(item, "code"))
                            {
                                var code = Convert.ToString(item.code);

                                if (!string.IsNullOrEmpty(code) && !dataToCache.ContainsKey(code))
                                {
                                    //MGET not supported in cluster - uncomment if supports
                                    dataToCache.TryAdd($"{ACTIVE_FORMULARY_CACHE_KEY_NAME}{code}", item);
                                    Interneuron.Caching.CacheService.Set($"{ACTIVE_FORMULARY_CACHE_KEY_NAME}{code}", item);
                                }
                            }
                        }

                        //MGET not supported in cluster
                        //Interneuron.Caching.CacheService.Set(dataToCache);//.ToDictionary(k=> k.Key, v=> v.Value));
                        await Interneuron.Caching.CacheService.SetAsync(dataToCache);

                        //Parallel.ForEach<dynamic>(results, (rec) =>
                        //{
                        //    Interneuron.Caching.CacheService.Set($"{ACTIVE_FORMULARY_CACHE_KEY_NAME}{rec.code}", rec);
                        //});
                    }
                    else
                    {
                        Log.Logger.Error($"Info: No batch results received for active formulary from api.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Error: Exception results for active formulary from api. {ex}");
                    exceptions.Enqueue(ex);
                }
            });

            // Throw the exceptions here after the loop completes.
            if (!exceptions.IsEmpty)
            {
                throw new AggregateException(exceptions);
            }
        }

        private async Task<List<string>> GetActiveFormularyCodes(List<string> codes)
        {
            var accessToken = await GetAccessToken();
            if (accessToken == null) return null;

            using var client = new RestClient(_formularyCacheConfig.ActiveFormularyCodesUrl);
            var request = new RestRequest() { Method = Method.Post, Timeout = -1 };
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(codes);

            var response = await client.ExecuteAsync(request);

            if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK || response.Content == null)
            {
                return null;
            }
            return JsonConvert.DeserializeObject<List<string>>(response.Content);

        }

        private async Task<List<dynamic>> GetActiveFormularies(List<string> codes)
        {
            var accessToken = await GetAccessToken();
            if (accessToken == null) return null;

            using var client = new RestClient(_formularyCacheConfig.ActiveFormularyUrl);
            var request = new RestRequest() { Method = Method.Post, Timeout = -1 };
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("fromCache", false);//not from cache

            request.AddJsonBody(codes);

            var response = await client.ExecuteAsync(request);

            if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK || response.Content == null)
            {
                return null;
            }
            return JsonConvert.DeserializeObject<List<dynamic>>(response.Content);
        
        }

        private async Task<string> GetAccessToken()
        {
            //Invoke cache api endpoint
            var accessTokenConfig = _configuration.GetSection("access_token_ep").Get<AccessTokenConfig>();

            using (var client = new RestClient(accessTokenConfig.Url))
            {
                var request = new RestRequest() { Method = Method.Post, Timeout = -1 };
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

                foreach (var param in accessTokenConfig.Params)
                {
                    request.AddParameter(param.Key, param.Value);
                }

                var response = await client.ExecuteAsync<AccessTokenDetail>(request);

                if (response == null || response.Data == null || string.IsNullOrEmpty(response.Data.Access_Token))
                {
                    return null;
                }
                return response.Data.Access_Token;
            }

        }

        private static bool IsPropertyExists(dynamic dynamicObj, string property)
        {
            try
            {
                var value = dynamicObj[property].Value;
                return true;
            }
            catch (RuntimeBinderException)
            {

                return false;
            }

        }
    }
}

