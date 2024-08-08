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
using Interneuron.FDBAPI.Client;
using Interneuron.FDBAPI.Client.DataModels;
using Interneuron.Terminology.Utility.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace Interneuron.Terminology.Utility.Handlers.AddFDBClassifications
{
    //sample - not in use right now. to be utilized
    public class AddFDBClassificationHandler
    {
        public IConfiguration Configuration { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        private readonly ILogger<AddFDBClassificationHandler> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public AddFDBClassificationHandler(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AddFDBClassificationHandler>();
            _loggerFactory = loggerFactory;
        }
        public async Task Handle()
        {
            List<string> fvIdsToTest = null;// new List<string>() { "7c61c390-0aeb-40ac-898e-e0c2d6a91ecc" };//to test, otherwise set to null

            try
            {
                var dBContext = ServiceProvider.GetService<TerminologyDBContext>();

                var latestAMPFormulariesFromHeader = dBContext.FormularyHeader.AsNoTracking().Where(rec => rec.ProductType == "AMP" && rec.IsLatest == true && (fvIdsToTest == null || fvIdsToTest.Contains(rec.FormularyVersionId)))
                    ?.Select(rec => new { rec.ProductType, rec.Code, rec.FormularyVersionId }).ToList();
                
                if (!latestAMPFormulariesFromHeader.IsCollectionValid())
                {
                    _logger.LogError($"Info: Date: {DateTime.UtcNow} - AddFDBClassificationHandler  - Stopped: Could not find any AMP header.");
                    return;
                }

                var baseFDBUrl = Configuration.GetSection("FDB").GetValue<string>("BaseURL");
                baseFDBUrl = baseFDBUrl.EndsWith("/") ? baseFDBUrl.TrimEnd('/') : baseFDBUrl;

                //var token = _requestContext.AuthToken;

                var fdbClient = new FDBAPIClient(baseFDBUrl, _loggerFactory);

                var batchsize = 1500;//not more than 2100- due to sql restrictions

                var batchedRequests = new List<List<FDBDataRequest>>();

                var codesAndProductTypes = latestAMPFormulariesFromHeader.Select(res => new FDBDataRequest()
                {
                    ProductType = "AMP",
                    ProductCode = res.Code
                }).ToList();

                for (var reqIndex = 0; reqIndex < codesAndProductTypes.Count; reqIndex += batchsize)
                {
                    var batches = codesAndProductTypes.Skip(reqIndex).Take(batchsize).ToList();
                    batchedRequests.Add(batches.ToList());
                }

                _logger.LogError($"Info: Date: {DateTime.UtcNow} - AddFDBClassificationHandler  - Total batches: {batchedRequests.Count}");

                var token = await GetAccessToken();

                for (var itemIndex = 0; itemIndex < batchedRequests.Count; itemIndex++)
                {
                    var item = batchedRequests[itemIndex];

                    var theraupeuticClasses = await fdbClient.GetAllTherapeuticClassificationGroupsByCodes(item, token);

                    if (theraupeuticClasses == null || !theraupeuticClasses.Data.IsCollectionValid()) continue;

                    UpdateFDBData(theraupeuticClasses, latestAMPFormulariesFromHeader, dBContext);

                    dBContext.SaveChanges();

                    if ((itemIndex % 10) == 0)//log for every 10 recs
                        _logger.LogError($"Info: Date: {DateTime.UtcNow} - AddFDBClassificationHandler - Completed for batch: {itemIndex + 1}");
                }
                //}

                _logger.LogError($"Info: Date: {DateTime.UtcNow} - AddFDBClassificationHandler invocation successfully Completed.");

            }
            catch (Exception ex)
            {
                _logger.LogError($"Info: Date: {DateTime.UtcNow} - Exception invoking UpdateExistingFDBCodesFromFDB API:" + ex.ToString());
                _logger.LogError(ex, ex.ToString());
            }
}

        private void UpdateFDBData(FDBAPIResourceModel<Dictionary<string, List<(string, string)>>> theraupeuticClasses, IEnumerable<dynamic> latestAMPFormulariesFromHeader, TerminologyDBContext dBContext)
        {
            if (theraupeuticClasses == null || !theraupeuticClasses.Data.IsCollectionValid()) return;

            //var additionalCodeRepo = this._provider.GetService(typeof(IRepository<FormularyAdditionalCode>)) as IRepository<FormularyAdditionalCode>;

            var codes = theraupeuticClasses.Data.Keys;

            var headerForCodes = latestAMPFormulariesFromHeader.Where(rec =>
            {
                string code = rec.Code.ToString();
                return code.IsNotEmpty() && codes.Contains(code);
            }).ToList();

            List<string> fvIdsForCodesInFDB = headerForCodes.Where(rec => codes.Contains((String)rec.Code))?
                .Select(rec => (String)rec.FormularyVersionId)?.ToList();

            //Check if these fvids has FDB Codes
            var fdbRecordsForCodes = dBContext.FormularyAdditionalCode.Where(rec => rec.AdditionalCodeSystem == "FDB" && fvIdsForCodesInFDB.Contains(rec.FormularyVersionId))
               .ToList();

            var fvIdExistingRecsLkp = new Dictionary<string, List<FormularyAdditionalCode>>();
            if (fdbRecordsForCodes.IsCollectionValid())
            {
                foreach (var fdbRecordInDbForCodes in fdbRecordsForCodes)
                {
                    if (fvIdExistingRecsLkp.ContainsKey(fdbRecordInDbForCodes.FormularyVersionId))
                        fvIdExistingRecsLkp[fdbRecordInDbForCodes.FormularyVersionId].Add(fdbRecordInDbForCodes);
                    else
                        fvIdExistingRecsLkp[fdbRecordInDbForCodes.FormularyVersionId] = new List<FormularyAdditionalCode> { fdbRecordInDbForCodes };
                }
            }

            foreach (var header in headerForCodes)
            {
                var fdbData = theraupeuticClasses.Data[header.Code];

                if (!fdbData.IsCollectionValid()) continue;

                //Check if this code has existing FDB in formulary
                List<FormularyAdditionalCode> existingFDBInDB = (fvIdExistingRecsLkp.IsCollectionValid() && fvIdExistingRecsLkp.ContainsKey(header.FormularyVersionId)) ? fvIdExistingRecsLkp[header.FormularyVersionId] : null;
                // fdbRecordsForCodes?.Where(rec => rec.FormularyVersionId == header.FormularyVersionId)?.ToList();

                if (existingFDBInDB.IsCollectionValid())
                {
                    foreach (var fdbItem in fdbData)
                    {
                        if (fdbItem.Item2.IsEmpty()) continue;

                        var additionalCodeInFDB = existingFDBInDB.Where(rec => fdbItem.Item1 == rec.AdditionalCode && rec.AdditionalCodeSystem == "FDB")?.ToList();

                        if (additionalCodeInFDB.IsCollectionValid())
                        {
                            foreach (var existingRecInDb in additionalCodeInFDB)
                            {
                                existingRecInDb.AdditionalCodeDesc = fdbItem.Item2;
                                existingRecInDb.Updatedby = "System";
                                existingRecInDb.Updateddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
                                existingRecInDb.Updatedtimestamp = DateTime.UtcNow;
                                dBContext.Update(existingRecInDb);
                            }
                        }
                        else
                        {
                            var newAddnlCode = new FormularyAdditionalCode
                            {
                                CodeType = "Classification",//TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE,
                                AdditionalCode = fdbItem.Item1,
                                AdditionalCodeDesc = fdbItem.Item2,
                                AdditionalCodeSystem = "FDB",//TerminologyConstants.FDB_DATA_SRC,
                                Source = "FDB",//TerminologyConstants.FDB_DATA_SRC,
                                FormularyVersionId = header.FormularyVersionId,
                                Createdby = "System",
                                Createddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                                Createdtimestamp = DateTime.UtcNow,
                                Updatedby = "System",
                                Updateddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                                Updatedtimestamp = DateTime.UtcNow
                            };
                            dBContext.Add(newAddnlCode);
                        }
                    }
                }
                else
                {
                    foreach (var fdbDataItem in fdbData)
                    {
                        var newAddnlCode = new FormularyAdditionalCode
                        {
                            CodeType = "Classification",//TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE,
                            AdditionalCode = fdbDataItem.Item1,
                            AdditionalCodeDesc = fdbDataItem.Item2,
                            AdditionalCodeSystem = "FDB",//TerminologyConstants.FDB_DATA_SRC,
                            Source = "FDB",//TerminologyConstants.FDB_DATA_SRC,
                            FormularyVersionId = header.FormularyVersionId,
                            Createdby = "System",
                            Createddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                            Createdtimestamp = DateTime.UtcNow,
                            Updatedby = "System",
                            Updateddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                            Updatedtimestamp = DateTime.UtcNow
                        };
                        dBContext.Add(newAddnlCode);
                    }
                }
            }
        }

        public async Task<string> GetAccessToken()
        {
            //Invoke cache api endpoint
            var accessTokenUrl = Configuration.GetSection("AddFDBHandlerConfig")["AccessTokenUrl"];
            var apiCreds = Configuration.GetSection("AddFDBHandlerConfig")["TerminologyAPICreds"].Split('|');
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

                try
                {
                    var response = await client.ExecuteAsync<AccessTokenDetail>(request);
                    if (response == null || response.Data == null || string.IsNullOrEmpty(response.Data.Access_Token))
                    {
                        return null;
                    }
                    return response.Data.Access_Token;
                }
                catch(Exception ex) 
                { 
                    return null;
                }
            }
        }
        public class AccessTokenDetail
        {
            public string? Access_Token { get; set; }

            public long? Expires_In { get; set; }

            public string? Token_Type { get; set; }
        }
    }
}
