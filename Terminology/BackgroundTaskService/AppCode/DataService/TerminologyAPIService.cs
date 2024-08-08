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
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using RestSharp;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.DataService
{
    public class TerminologyAPIService
    {
        const string TerminologyAPI_URI = "api/terminology";
        const string Terminology_UTIL_API_URI = "api/util/Configuration";

        //const string FormularyAPI_URI = "api/formulary";
        private IConfiguration _configuration;
        private readonly ILogger<TerminologyAPIService> _logger;

        public TerminologyAPIService(IConfiguration configuration, ILogger<TerminologyAPIService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }


        public async Task<TerminologyAPIResponse<object>> UpdateFormularySyncStatus(List<string> dmdCodes)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            await InvokeService<dynamic>(backgroundBaseURL, $"{TerminologyAPI_URI}/updateformularysyncstatus/", Method.Post, dmdCodes);

            return null;
        }
        public async Task<TerminologyAPIResponse<List<DmdLookupRouteDTO>>> GetRouteLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };
            var results = await InvokeService<List<DmdLookupRouteDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdroutelookup/", Method.Get, queryStringParams: qs );

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupFormDTO>>> GetFormLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupFormDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdformlookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupPrescribingstatusDTO>>> GetPrescribingStatusLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupPrescribingstatusDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdprescribingstatuslookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupControldrugcatDTO>>> GetControlDrugCategoryLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupControldrugcatDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdcontroldrugcategorylookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupSupplierDTO>>> GetSupplierLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupSupplierDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdsupplierlookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupLicauthDTO>>> GetLicensingAuthorityLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupLicauthDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdlicensingauthoritylookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupUomDTO>>> GetDMDUOMLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupUomDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmduomlookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupBasisofnameDTO>>> GetDMDBasisOfNameLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupBasisofnameDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdbasisofnamelookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupAvailrestrictDTO>>> GetDMDAvailRestrictionsLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupAvailrestrictDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdavailrestrictionslookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupDrugformindDTO>>> GetDMDDoseFormLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupDrugformindDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmddoseformlookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupBasisofstrengthDTO>>> GetDMDPharamceuticalStrengthLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupBasisofstrengthDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdpharamceuticalstrengthlookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupIngredientDTO>>> GetDMDIngredientLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupIngredientDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdingredientlookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<AtcLookupDTO>>> GetATCLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<AtcLookupDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getatclookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DmdLookupBNFDTO>>> GetBNFLookup(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdLookupBNFDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getbnflookup/", Method.Get, queryStringParams: qs);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<DMDDetailResultDTO>>> GetDMDFullDataForCodes(List<string> codes)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var results = await InvokeService<List<DMDDetailResultDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdfulldataforcodes/",  Method.Post, codes, null);

            return results;
        }

        internal async Task<TerminologyAPIResponse<List<DmdAmpExcipientDTO>>> GetAMPExcipientsForCodes(List<string> codes)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var results = await InvokeService<List<DmdAmpExcipientDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getampexcipientsforcodes/", Method.Post, codes, null);

            return results;
        }

        internal async Task<TerminologyAPIResponse<List<DmdAmpDrugrouteDTO>>> GetAMPDrugRoutesForCodes(List<string> codes)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var results = await InvokeService<List<DmdAmpDrugrouteDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getampdrugroutesforcodes/", Method.Post, codes);

            return results;
        }

        internal async Task<TerminologyAPIResponse<List<DmdVmpDrugrouteDTO>>> GetVMPDrugRoutesForCodes(List<string> codes)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var results = await InvokeService<List<DmdVmpDrugrouteDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getvmpdrugroutesforcodes/", Method.Post, codes);

            return results;
        }

        internal async Task<TerminologyAPIResponse<List<DmdBNFCodeDTO>>> GetAllBNFCodesFromDMD(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdBNFCodeDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getallbnfcodesfromdmd/", Method.Get, queryStringParams: qs);

            return results;
        }

        internal async Task<TerminologyAPIResponse<List<DmdATCCodeDTO>>> GetAllATCCodesFromDMD(bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var qs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ignoreCacheSource", $"{ignoreFromCache}") };

            var results = await InvokeService<List<DmdATCCodeDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getallatccodesfromdmd/", Method.Get, queryStringParams: qs);

            return results;
        }

        internal async Task<TerminologyAPIResponse<List<DmdSyncLog>>> GetDMDPendingSyncLogs()
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var results = await InvokeService<List<DmdSyncLog>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdpendingsynclogs/", Method.Get);

            return results;
        }

        internal async Task<TerminologyAPIResponse<List<DmdSyncLog>>> GetDMDPendingSyncLogsByPagination(int pageNo, int pageSize)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var results = await InvokeService<List<DmdSyncLog>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getdmdpendingsynclogsbypagination/{pageNo}/{pageSize}", Method.Get);

            return results;
        }



        internal async Task<TerminologyAPIResponse<List<SnomedTradeFamiliesDTO>>> GetTradeFamilyForConceptIds(List<string> codes, bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];

            var headers = new Dictionary<string, string> { {"ignoreCacheSource", $"{ignoreFromCache}"} };

            var results = await InvokeService<List<SnomedTradeFamiliesDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/gettradefamilyforconceptids/", Method.Post, codes, headers: headers);

            return results;
        }

        internal async Task<TerminologyAPIResponse<List<SnomedModifiedReleaseDTO>>> GetModifiedReleaseForConceptIds(List<string> codes, bool ignoreFromCache = false)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyAPIBaseURL"];
            var headers = new Dictionary<string, string> { { "ignoreCacheSource", $"{ignoreFromCache}" } };

            var results = await InvokeService<List<SnomedModifiedReleaseDTO>>(backgroundBaseURL, $"{TerminologyAPI_URI}/getmodifiedreleaseforconceptids/", Method.Post, codes, headers: headers);

            return results;
        }

        public async Task<TerminologyAPIResponse<BackgroundTaskAPIModel>> CreateTerminologyBGTask(BackgroundTaskAPIModel apiModel)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyBackgroundTaskAPIBaseURL"];

            var results = await InvokeService<BackgroundTaskAPIModel>(backgroundBaseURL, $"api/BackgroundTask/", Method.Post, apiModel, null);

            return results;
        }

        public async Task<TerminologyAPIResponse<BackgroundTaskAPIModel>> UpdateTerminologyBGTask(BackgroundTaskAPIModel apiModel)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyBackgroundTaskAPIBaseURL"];

            var results = await InvokeService<BackgroundTaskAPIModel>(backgroundBaseURL, $"api/BackgroundTask/", Method.Put, apiModel, null);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<BackgroundTaskAPIModel>>> GetTaskByNames(List<string> taskNames, short? statusCd)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyBackgroundTaskAPIBaseURL"];

            var stsCd = statusCd.HasValue ? statusCd.Value.ToString() : string.Empty;
            var results = await InvokeService<List<BackgroundTaskAPIModel>>(backgroundBaseURL, $"api/BackgroundTask/GetTaskByNames/{stsCd}", Method.Post, taskNames, null);

            return results;
        }

        public async Task<TerminologyAPIResponse<List<BackgroundTaskAPIModel>>> GetTaskByNamesAndUpdateStatus(GetTaskByNamesAndUpdateStatusRequestAPIModel request)
        {
            var backgroundBaseURL = _configuration["TerminologyBackgroundTaskConfig:TerminologyBackgroundTaskAPIBaseURL"];

            var results = await InvokeService<List<BackgroundTaskAPIModel>>(backgroundBaseURL, $"api/BackgroundTask/GetTaskByNamesAndUpdateStatus", Method.Post, request, null);

            return results;
        }

        //static Task<RestResponse<T>> RetryPolicyWrapper<T>(Func<Task<RestResponse<T>>> callback)
        IAsyncPolicy<RestResponse<T>> GetRetryPolicy<T>(string apiEndpoint, Method method = Method.Get)
        {
            var httpRetryPolicy = Policy
                        .HandleResult<RestResponse<T>>(message => !message.IsSuccessful)
                        .Or<HttpRequestException>()
                        .Or<TimeoutRejectedException>()
                        .WaitAndRetryAsync(new[]
                        {
                                TimeSpan.FromSeconds(1),
                                TimeSpan.FromSeconds(3),
                                TimeSpan.FromSeconds(10),
                        }, (result, timeSpan, retryCount, context) =>
                        {
                            apiEndpoint = apiEndpoint ?? "";
                            var methodName = $"{method}";
                            var statusCode = result != null && result.Result != null ? $"{result.Result.StatusCode}" : "";
                            var message = $"APIEndpoint: {apiEndpoint}. HTTPMethod: {method}. Request failed with {statusCode}. Retry count = {retryCount}. Waiting {timeSpan} before next retry. ";
                            _logger?.LogError(message);
                            Console.WriteLine(message);
                        });

            //var httpRetryPolicy1 = HttpPolicyExtensions
            //    .HandleTransientHttpError()
            //    .OrTransientHttpError()
            //    .Or<TimeoutRejectedException>()
            //    //.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            //    .OrResult(msg => !msg.IsSuccessStatusCode)
            //    .WaitAndRetryAsync(new[]
            //            {
            //                TimeSpan.FromSeconds(1),
            //                TimeSpan.FromSeconds(10),
            //            }, (result, timeSpan, retryCount, context) =>
            //            {
            //                Console.WriteLine($"Request failed with {result.Result.StatusCode}. Retry count = {retryCount}. Waiting {timeSpan} before next retry. ");
            //            });

            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(300));

            //return httpRetryPolicy.WrapAsync(timeoutPolicy).ExecuteAsync(callback);
            return httpRetryPolicy.WrapAsync(timeoutPolicy);
        }

        //static IAsyncPolicy<RestResponse<T>> GetRetryPolicy<T>()
        //{
        //    return Policy.HandleResult<RestResponse<T>>(message => !message.IsSuccessful)
        //                .WaitAndRetryAsync(new[]
        //                {
        //                    TimeSpan.FromSeconds(1),
        //                    TimeSpan.FromSeconds(3),
        //                    TimeSpan.FromSeconds(10),
        //                }, (result, timeSpan, retryCount, context) =>
        //                {
        //                    Console.WriteLine($"Request failed with {result.Result.StatusCode}. Retry count = {retryCount}. Waiting {timeSpan} before next retry. ");
        //                });
        //}

        private async Task<TerminologyAPIResponse<T>> InvokeService<T>(string? baseUrlParam, string apiEndpoint, Method method, dynamic payload = null, Func<string, bool> onError = null, Dictionary<string, string> headers = null, List<KeyValuePair<string, string>> queryStringParams = null)
        {
            var response = new TerminologyAPIResponse<T> { StatusCode = StatusCode.Success };

            var accessToken = await GetAccessToken();

            if (accessToken == null) {
                response.StatusMessage = "Invalid Accesstoken";
                response.StatusCode = StatusCode.Fail;
                return response;
            }

            var baseUrl = baseUrlParam ?? _configuration["TerminologyBackgroundTaskConfig_TerminologyBackgroundTaskBaseAPIURL"];

            var url = baseUrl.EndsWith("/") ? $"{baseUrl}{apiEndpoint}" : $"{baseUrl}/{apiEndpoint}";

            var pollyContext = new Context("Retry");

            var result = await GetRetryPolicy<T>(url, method).ExecuteAsync(async ctx =>
            {
                var restClientOptions = new RestClientOptions();
                restClientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                restClientOptions.BaseUrl = new UriBuilder(url).Uri;
                using var client = new RestClient(restClientOptions);
                
                var request = new RestRequest() { Method = method, Timeout = -1 };
                request.AddHeader("Authorization", $"Bearer {accessToken}");

                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept-Language", "application/json");

                if (payload != null)
                {
                    string payloadAsString = JsonConvert.SerializeObject(payload);
                    request.AddJsonBody(payloadAsString);
                }

                if (queryStringParams.IsCollectionValid())
                    queryStringParams.Each(q => request.AddQueryParameter(q.Key, q.Value));

                var result = await client.ExecuteAsync<T>(request);
                return result;

                //var result = await client.ExecuteAsync<T>(request);
                //var result = await GetRetryPolicy<T>().ExecuteAsync(async ()=> await client.ExecuteAsync<T>(request));
                //var result = await RetryPolicyWrapper(async () => await client.ExecuteAsync<T>(request));
            }, pollyContext);

            

            if (result == null || !result.IsSuccessful)
            {
                response.StatusCode = StatusCode.Fail;

                if (result.ErrorMessage.IsNotEmpty())
                    response.ErrorMessages = new List<string> { result.ErrorMessage };

                onError?.Invoke(result.Content);

                //try
                //{
                //    Log.Error(result.ToString());
                //}
                //catch { }

                //var isErrorHandledInCallback = onError?.Invoke(content);

                //if (isErrorHandledInCallback.GetValueOrDefault() == true)
                //    return default(T);
            }
            else
            {
                response.Data = result.Data;
            }

            return response;
            /*


            var dynamicServiceAPIUrl = $"{_configuration["TerminologyConfig:DynamicAPIEndpoint"]}/GetBaseViewList/epma_prescriptionsusagestat";

            using var client = new RestClient(dynamicServiceAPIUrl);

            var request = new RestRequest() { Method = Method.Get, Timeout = -1 };
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept-Language", "application/json");

            var response = await client.ExecuteAsync<List<FormularyUsageStatDTO>>(request);

            if (response == null || (response.StatusCode != System.Net.HttpStatusCode.Accepted && response.StatusCode != System.Net.HttpStatusCode.OK) || response.Content.IsEmpty())
            {
                return NoContent();
            }
            var data = new List<FormularyUsageStatDTO>();
            try
            {
                var dataTemp = JsonConvert.DeserializeObject<dynamic>(response.Content);
                data = JsonConvert.DeserializeObject<List<FormularyUsageStatDTO>>(dataTemp);
            }
            catch { }

            if (!data.IsCollectionValid()) return NoContent();

            var result = await _formularyCommand.SeedFormularyUsageStatForEPMA(data);

            if (result == null || result.StatusCode == TerminologyConstants.STATUS_BAD_REQUEST)
                return BadRequest(result?.ErrorMessages);

            return Ok();



            var response = new TerminologyAPIResponse<T> { StatusCode = StatusCode.Success };

            string baseUrl = baseUrlParam ?? Environment.GetEnvironmentVariable("connectionString_TerminologyServiceBaseURL");

            using (var client = new HttpClient())
            {
                var url = baseUrl.EndsWith("/") ? $"{baseUrl}{apiEndpoint}" : $"{baseUrl}/{apiEndpoint}";

                if (queryStringParams.IsCollectionValid())
                    url = QueryHelpers.AddQueryString(url, queryStringParams);

                UriBuilder builder = new UriBuilder(url);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.Timeout = TimeSpan.FromHours(24);

                var requestMessage = new HttpRequestMessage(method, builder.Uri);

                if ((payload != null) && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    var json = JsonConvert.SerializeObject(payload);
                    var stringContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
                    requestMessage.Content = stringContent;

                    if (headers.IsCollectionValid())
                    {
                        headers.Keys.Each(k => requestMessage.Headers.Add(k, headers[k]));
                    }

                }

                var result = await client.SendAsync(requestMessage);

                using (StreamReader sr = new StreamReader(result.Content.ReadAsStreamAsync().Result))
                {
                    string content = sr.ReadToEnd();

                    if (!result.IsSuccessStatusCode)
                    {
                        response.StatusCode = StatusCode.Fail;

                        response.ErrorMessages = new List<string> { content };

                        onError?.Invoke(content);

                        //try
                        //{
                        //    Log.Error(result.ToString());
                        //}
                        //catch { }

                        //var isErrorHandledInCallback = onError?.Invoke(content);

                        //if (isErrorHandledInCallback.GetValueOrDefault() == true)
                        //    return default(T);
                    }
                    else
                    {
                        var data = JsonConvert.DeserializeObject<T>(content);
                        response.Data = data;
                    }
                }

                return response;
            
            }
            */
        }

       

        private async Task<TerminologyAPIResponse<T>> InvokeService<T>(string apiEndpoint, Method method, dynamic payload = null, Func<string, bool> onError = null, Dictionary<string, string> headers = null, List<KeyValuePair<string, string>> queryStringParams = null)
        {
            return await InvokeService<T>(null, apiEndpoint, method, payload, onError, headers, queryStringParams);
        }

        public async Task<string> GetAccessToken()
        {
            //Invoke cache api endpoint
            var accessTokenUrl = _configuration.GetSection("TerminologyBackgroundTaskConfig")["AccessTokenUrl"];
            var apiCreds = _configuration.GetSection("TerminologyBackgroundTaskConfig")["TerminologyBackgroundTaskAPICreds"].Split('|');
            var headerParams = new Dictionary<string, string>()
            {
                //["grant_type"] = "client_credentials",
                //["client_id"] = "client",
                //["client_secret"] = "secret",
                //["scope"] = "terminologyapi.write dynamicapi.read terminologyapi.read carerecordapi.read"
            };

            foreach (var item in apiCreds)
            {
                var kv = item.Split(':');
                headerParams.Add(kv[0], kv[1]);
            }

            using (var client = new RestClient(accessTokenUrl))
            {
                var request = new RestRequest() { Method = Method.Post, Timeout = -1 };
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

                foreach (var param in headerParams)
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
    }

    public partial class TerminologyAPIResponse<T>
    {
        public StatusCode StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public T Data { get; set; }
    }

    public enum StatusCode
    {
        Success = 1,
        Fail = 2,
    }

    public partial class TerminiologyAPIStatusModel
    {
        public short StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }

    public static class TerminiologyStatusModelExtension
    {
        public static bool IsSuccess(this TerminiologyAPIStatusModel statusModel)
        {
            return statusModel != null && statusModel.StatusCode == 1;
        }

        public static bool HasErrors(this TerminiologyAPIStatusModel statusModel)
        {
            return statusModel != null && statusModel.ErrorMessages.IsCollectionValid();
        }

        public static List<string> GetErrors(this TerminiologyAPIStatusModel statusModel)
        {
            return statusModel != null && statusModel.ErrorMessages.IsCollectionValid() ? statusModel.ErrorMessages : null;
        }

        public static string GetFlattenedErrors(this TerminiologyAPIStatusModel statusModel)
        {
            return statusModel != null && statusModel.ErrorMessages.IsCollectionValid() ? string.Join('\n', statusModel.ErrorMessages) : null;
        }
    }
}
