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
using Interneuron.Terminology.Infrastructure;
using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Model.DomainModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Core.BackgroundProcess
{

    public class UpdateExistingFDBCodesFromFDBBackgroundService
    {
        private APIRequestContext _requestContext;
        private IConfiguration _configuration;
        private IServiceScopeFactory _serviceScopeFactory;
        private ILoggerFactory _loggerFactory;
        private ILogger<UpdateExistingFDBCodesFromFDBBackgroundService> _logger;

        public UpdateExistingFDBCodesFromFDBBackgroundService(IConfiguration configuration, APIRequestContext requestContext, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _requestContext = requestContext;
            _serviceScopeFactory = serviceScopeFactory;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<UpdateExistingFDBCodesFromFDBBackgroundService>();
        }

        public async Task UpdateExistingFDBCodesFromFDB(string messageId, IServiceScope scope, IRepository<FormularyHeader> repo, IRepository<FormularyAdditionalCode> additionalCodeRepo, string token)
        {
            //This is idempotent and can be reexecuted
            //Earlier, as a part of the import process, only the last FDB code used to be imported and used.
            //Now, all the FDB codes will be fetched for the AMP from FDB and this api endpoint does it.

            //Invoke and forget but check whether accepted

            //_ = Task.Run(async () =>
            //{
            try
            {
                _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingFDBCodesFromFDB API - invocation Started."); //only error severity is configured for logging.

                List<FormularyHeader> latestAMPFormulariesFromHeader = null;

                //using (var scope = _serviceScopeFactory.CreateScope())
                //{
                //    var serviceProvider = scope.ServiceProvider;

                //    var repo = serviceProvider.GetRequiredService(typeof(IRepository<FormularyHeader>)) as IRepository<FormularyHeader>;
                //    var additionalCodeRepo = serviceProvider.GetRequiredService(typeof(IRepository<FormularyAdditionalCode>)) as IRepository<FormularyAdditionalCode>;

                latestAMPFormulariesFromHeader = repo.ItemsAsReadOnly.Where(rec => rec.ProductType == "AMP" && rec.IsLatest == true)?.ToList();

                if (!latestAMPFormulariesFromHeader.IsCollectionValid())
                {
                    _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingFDBCodesFromFDB API - Stopped: Could not find any AMP header.");
                    return;
                }

                var baseFDBUrl = _configuration.GetSection("FDB").GetValue<string>("BaseURL");
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

                _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingFDBCodesFromFDB API - Total batches: {batchedRequests.Count}");

                for (var itemIndex = 0; itemIndex < batchedRequests.Count; itemIndex++)
                {
                    var item = batchedRequests[itemIndex];

                    var theraupeuticClasses = await fdbClient.GetAllTherapeuticClassificationGroupsByCodes(item, token);

                    if (theraupeuticClasses == null || !theraupeuticClasses.Data.IsCollectionValid()) continue;

                    UpdateFDBData(theraupeuticClasses, latestAMPFormulariesFromHeader, additionalCodeRepo);

                    additionalCodeRepo.SaveChanges();

                    if ((itemIndex % 10) == 0)//log for every 10 recs
                        _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingFDBCodesFromFDB API - Completed for batch: {itemIndex + 1}");
                }
                //}

                _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - UpdateExistingFDBCodesFromFDB API invocation successfully Completed.");

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Info: MessageId: {messageId} Date: {DateTime.UtcNow} - Exception invoking UpdateExistingFDBCodesFromFDB API:" + ex.ToString());
                _logger.LogError(ex, ex.ToString());
            }
            //});
        }

        private void UpdateFDBData(FDBAPIResourceModel<Dictionary<string, List<(string, string)>>> theraupeuticClasses, List<FormularyHeader> latestAMPFormulariesFromHeader, IRepository<FormularyAdditionalCode> additionalCodeRepo)
        {
            if (theraupeuticClasses == null || !theraupeuticClasses.Data.IsCollectionValid()) return;

            //var additionalCodeRepo = this._provider.GetService(typeof(IRepository<FormularyAdditionalCode>)) as IRepository<FormularyAdditionalCode>;

            var codes = theraupeuticClasses.Data.Keys;

            var headerForCodes = latestAMPFormulariesFromHeader.Where(rec => codes.Contains(rec.Code)).ToList();

            var fvIdsForCodesInFDB = headerForCodes.Where(rec => codes.Contains(rec.Code))?
                .Select(rec => rec.FormularyVersionId)?.ToList();

            //Check if these fvids has FDB Codes
            var fdbRecordsForCodes = additionalCodeRepo.Items.Where(rec => rec.AdditionalCodeSystem == "FDB" && fvIdsForCodesInFDB.Contains(rec.FormularyVersionId))
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
                var existingFDBInDB = (fvIdExistingRecsLkp.IsCollectionValid() && fvIdExistingRecsLkp.ContainsKey(header.FormularyVersionId)) ? fvIdExistingRecsLkp[header.FormularyVersionId] : null;
                // fdbRecordsForCodes?.Where(rec => rec.FormularyVersionId == header.FormularyVersionId)?.ToList();

                if (existingFDBInDB.IsCollectionValid())
                {
                    foreach (var fdbItem in fdbData)
                    {
                        var additionalCodeInFDB = existingFDBInDB.Where(rec => fdbItem.Item1 == rec.AdditionalCode && rec.AdditionalCodeSystem == "FDB")?.ToList();

                        if (additionalCodeInFDB.IsCollectionValid())
                        {
                            foreach (var existingRecInDb in additionalCodeInFDB)
                            {
                                existingRecInDb.AdditionalCodeDesc = fdbItem.Item2;
                                existingRecInDb.Updatedby = "System";
                                existingRecInDb.Updateddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
                                existingRecInDb.Updatedtimestamp = DateTime.UtcNow;
                                additionalCodeRepo.Update(existingRecInDb);
                            }
                        }
                        else
                        {
                            var newAddnlCode = new FormularyAdditionalCode
                            {
                                CodeType = TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE,
                                AdditionalCode = fdbItem.Item1,
                                AdditionalCodeDesc = fdbItem.Item2,
                                AdditionalCodeSystem = TerminologyConstants.FDB_DATA_SRC,
                                Source = TerminologyConstants.FDB_DATA_SRC,
                                FormularyVersionId = header.FormularyVersionId,
                                Createdby = "System",
                                Createddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                                Createdtimestamp = DateTime.UtcNow,
                                Updatedby = "System",
                                Updateddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                                Updatedtimestamp = DateTime.UtcNow
                            };
                            additionalCodeRepo.Add(newAddnlCode);
                        }
                    }
                }
                else
                {
                    foreach (var fdbDataItem in fdbData)
                    {
                        var newAddnlCode = new FormularyAdditionalCode
                        {
                            CodeType = TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE,
                            AdditionalCode = fdbDataItem.Item1,
                            AdditionalCodeDesc = fdbDataItem.Item2,
                            AdditionalCodeSystem = TerminologyConstants.FDB_DATA_SRC,
                            Source = TerminologyConstants.FDB_DATA_SRC,
                            FormularyVersionId = header.FormularyVersionId,
                            Createdby = "System",
                            Createddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                            Createdtimestamp = DateTime.UtcNow,
                            Updatedby = "System",
                            Updateddate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                            Updatedtimestamp = DateTime.UtcNow
                        };
                        additionalCodeRepo.Add(newAddnlCode);
                    }
                }
            }
        }
    }
}
