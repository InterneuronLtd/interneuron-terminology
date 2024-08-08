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
using Microsoft.EntityFrameworkCore;
using Interneuron.Terminology.BackgroundTaskService.Repository.DBModelsContext;
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Model.Search;
using Dapper;
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;

namespace Interneuron.Terminology.BackgroundTaskService.Repository
{
    public partial class FormularyRepository<TEntity> : Repository<TEntity>, IFormularyRepository<TEntity> where TEntity : EntityBase
    {
        private TerminologyDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private IEnumerable<IEntityEventHandler> _entityEventHandlers;

        public FormularyRepository(TerminologyDBContext dbContext, IConfiguration configuration, IEnumerable<IEntityEventHandler> entityEventHandlers) : base(dbContext, entityEventHandlers)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _entityEventHandlers = entityEventHandlers;
        }

        /// <summary>
        /// Returns the latest version of formularies for the codes
        /// </summary>
        /// <param name="codes"></param>
        /// <param name="onlyNonDeleted"></param>
        /// <returns></returns>
        public List<FormularyHeader>? GetLatestFormulariesByCodes(string[] codes, bool onlyNonDeleted = false)
        {
            var query = this._dbContext.FormularyHeader
                .Include(hdr => hdr.FormularyDetail)// Include<FormularyDetail>("FormularyDetail");
                .Include(hdr => hdr.FormularyAdditionalCode)
                .Include(hdr => hdr.FormularyIndication)
                .Include(hdr => hdr.FormularyIngredient)
                .Include(hdr => hdr.FormularyRouteDetail)
                .Include(hdr => hdr.FormularyExcipient)
                .Include(hdr => hdr.FormularyLocalRouteDetail)
                .Where(hdr => codes.Contains(hdr.Code)
                && hdr.IsLatest == true);

            if (onlyNonDeleted)
            {
                query = query.Where(hdr => hdr.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED);
            }

            var results = query.ToList();
            results?.Each(rec => rec.IsLatest = true);
            return results;
        }


        /// <summary>
        /// Returns the all versions (both latest and non-latest) of formularies as queryable for the codes
        /// </summary>
        /// <param name="onlyNonDeleted"></param>
        /// <returns></returns>
        public IQueryable<FormularyHeader> GetAllFormulariesAsQueryableWithNoTracking(bool onlyNonDeleted = false)
        {
            IQueryable<FormularyHeader> query = this._dbContext.FormularyHeader
                .Include(hdr => hdr.FormularyDetail)// Include<FormularyDetail>("FormularyDetail");
                .Include(hdr => hdr.FormularyAdditionalCode)
                .Include(hdr => hdr.FormularyIndication)
                .Include(hdr => hdr.FormularyIngredient)
                .Include(hdr => hdr.FormularyRouteDetail)
                .Include(hdr => hdr.FormularyExcipient)
                .Include(hdr => hdr.FormularyLocalRouteDetail);

            if (onlyNonDeleted)
            {
                query = query.Where(hdr => hdr.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED);
            }
#if DEBUG
            var sql = query.ToQueryString();
#endif
            return query.AsNoTracking();
        }


        /// <summary>
        /// Returns the latest version of formularies as queryable for the codes
        /// </summary>
        /// <param name="onlyNonDeleted"></param>
        /// <returns></returns>
        public IQueryable<FormularyHeader> GetLatestFormulariesAsQueryableWithNoTracking(bool onlyNonDeleted = false)
        {
            IQueryable<FormularyHeader> query = this._dbContext.FormularyHeader
                .Include(hdr => hdr.FormularyDetail)// Include<FormularyDetail>("FormularyDetail");
                .Include(hdr => hdr.FormularyAdditionalCode)
                .Include(hdr => hdr.FormularyIndication)
                .Include(hdr => hdr.FormularyIngredient)
                .Include(hdr => hdr.FormularyRouteDetail)
                .Include(hdr => hdr.FormularyExcipient)
                .Include(hdr => hdr.FormularyLocalRouteDetail)
                .Where(hdr => hdr.IsLatest == true);

            if (onlyNonDeleted)
            {
                query = query.Where(hdr => hdr.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED);
            }
#if DEBUG
            var sql = query.ToQueryString();
#endif
            return query.AsNoTracking();
        }

        /// <summary>
        /// Returns the latest version of formularies
        /// </summary>
        /// <param name="codes"></param>
        /// <param name="onlyNonDeleted"></param>
        /// <returns></returns>
        public IQueryable<FormularyHeader> GetLatestFormulariesAsQueryable(bool onlyNonDeleted = false)
        {
            var query = this._dbContext.FormularyHeader
                .Include(hdr => hdr.FormularyDetail)// Include<FormularyDetail>("FormularyDetail");
                .Include(hdr => hdr.FormularyAdditionalCode)
                .Include(hdr => hdr.FormularyIndication)
                .Include(hdr => hdr.FormularyIngredient)
                .Include(hdr => hdr.FormularyRouteDetail)
                .Include(hdr => hdr.FormularyExcipient)
                .Include(hdr => hdr.FormularyLocalRouteDetail)
                .Where(hdr => hdr.IsLatest == true);

            if (onlyNonDeleted)
            {
                query = query.Where(hdr => hdr.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED);
            }

            return query;
        }

        public async Task<IEnumerable<TEntity>> GetFormularyDescendentForCodes(string[] codes, bool onlyNonDeleted = true)
        {
            if (!codes.IsCollectionValid()) return null;

            var qryStmt = $"SELECT * from local_formulary.udf_formulary_get_descendents_by_codes(@in_codes)";

            IEnumerable<FormularyBasicSearchResultModel> results;

            var connString = _configuration.GetValue<string>("TerminologyBackgroundTaskConfig:Connectionstring");

            using (var conn = new Npgsql.NpgsqlConnection(connString))
            {
                results = await conn.QueryAsync<FormularyBasicSearchResultModel>(qryStmt, new { in_codes = codes });
            }
            if (results == null) return null;

            if (onlyNonDeleted)
                results = results.Where(rec => rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED);

            return (IEnumerable<TEntity>)results;
        }

        public async Task<IEnumerable<TEntity>> GetFormularyImmediateDescendentForCodes(string[] codes, bool onlyNonDeleted = true)
        {
            if (!codes.IsCollectionValid()) return null;

            var qryStmt = $"SELECT * from local_formulary.udf_formulary_get_next_descendents_by_codes(@in_codes)";

            IEnumerable<FormularyBasicSearchResultModel> results;

            var connString = _configuration.GetValue<string>("TerminologyBackgroundTaskConfig:Connectionstring");

            using (var conn = new Npgsql.NpgsqlConnection(connString))
            {
                results = await conn.QueryAsync<FormularyBasicSearchResultModel>(qryStmt, new { in_codes = codes });
            }
            if (results == null) return null;

            if (onlyNonDeleted)
                results = results.Where(rec => rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED);

            return (IEnumerable<TEntity>)results;
        }

        public async Task<IEnumerable<TEntity>> GetFormularyAncestorForCodes(string[] codes, bool onlyNonDeleted = true)
        {
            if (!codes.IsCollectionValid()) return null;

            var qryStmt = $"SELECT * from local_formulary.udf_formulary_get_ancestors_by_codes(@in_codes)";

            IEnumerable<FormularyBasicSearchResultModel> results;

            var connString = _configuration.GetValue<string>("TerminologyBackgroundTaskConfig:Connectionstring");

            using (var conn = new Npgsql.NpgsqlConnection(connString))
            {
                results = await conn.QueryAsync<FormularyBasicSearchResultModel>(qryStmt, new { in_codes = codes });
            }
            if (results == null) return null;

            if (onlyNonDeleted)
                results = results.Where(rec => rec.RecStatusCode != TerminologyConstants.RECORDSTATUS_DELETED);

            return (IEnumerable<TEntity>)results;
        }

        public async Task<bool> TruncateFormularyChangeLog()
        {
            var isUpdated = await _dbContext.Database.ExecuteSqlRawAsync("Truncate table local_formulary.formulary_change_log");

            return (isUpdated >= 0);
        }

        public async Task<bool> RefreshFormularyChangeLogMaterializedView()
        {
            var isUpdated = await _dbContext.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW local_formulary.formulary_change_log_mat");

            return (isUpdated >= 0);
        }

        public async Task<bool> TruncateFormularies()
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_sync_log");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM local_formulary.formulary_additional_code");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM local_formulary.formulary_detail");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM local_formulary.formulary_ingredient");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM local_formulary.formulary_local_route_detail");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM local_formulary.formulary_route_detail");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM local_formulary.formulary_excipient");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM local_formulary.formulary_header");
            }
            catch { }

            return true;
        }

        public async Task<bool> UpdateAllStatusAsActive()
        {
            var isUpdated = await _dbContext.Database.ExecuteSqlRawAsync(@"UPDATE local_formulary.formulary_header 
                                                                        SET rec_status_code = '003',
                                                                            rec_statuschange_msg = 'by system',
                                                                            rec_statuschange_date = now(),
                                                                            _updatedtimestamp = timezone('utc', now()),
                                                                            _updateddate = now(),
                                                                            _updatedby = 'SYSTEM'");

            return (isUpdated >= 0);
        }
    }
}
