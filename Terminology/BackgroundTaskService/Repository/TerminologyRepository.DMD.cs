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
﻿using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Repository.DBModelsContext;
using Microsoft.EntityFrameworkCore;

namespace Interneuron.Terminology.BackgroundTaskService.Repository
{
    public partial class TerminologyRepository<TEntity> : Repository<TEntity>, ITerminologyRepository<TEntity> where TEntity : EntityBase
    {
        private TerminologyDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private IEnumerable<IEntityEventHandler> _entityEventHandlers;
        public TerminologyRepository(TerminologyDBContext dbContext, IConfiguration configuration, IEnumerable<IEntityEventHandler> entityEventHandlers) : base(dbContext, entityEventHandlers)
        {

            _dbContext = dbContext;
            _configuration = configuration;
            _entityEventHandlers = entityEventHandlers;
        }

        public async Task<bool> TruncateTerminologies()
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_sync_log");

                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_amp");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_amp_drugroute");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_amp_excipient");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_availrestrict");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_basisofname");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_basisofstrength");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_controldrugcat");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_drugformind");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_form");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_ingredient");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_licauth");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_ontformroute");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_prescribingstatus");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_route");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_supplier");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_lookup_uom");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_vmp");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_vmp_controldrug");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_vmp_drugform");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_vmp_drugroute");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_vmp_ontdrugform");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_vmp_ingredient");
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE terminology.dmd_vtm");
            }
            catch { }

            return true;
        }
    }
}

