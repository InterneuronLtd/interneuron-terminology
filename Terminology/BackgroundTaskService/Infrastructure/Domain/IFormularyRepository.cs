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
﻿using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;

namespace Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain
{
    public interface IFormularyRepository<TEntity> : IRepository<TEntity> where TEntity : EntityBase
    {
        List<FormularyHeader>? GetLatestFormulariesByCodes(string[] codes, bool onlyNonDeleted = false);
        Task<IEnumerable<TEntity>> GetFormularyDescendentForCodes(string[] codes, bool onlyNonDeleted = true);
        Task<IEnumerable<TEntity>> GetFormularyAncestorForCodes(string[] codes, bool onlyNonDeleted = true);
        IQueryable<FormularyHeader> GetLatestFormulariesAsQueryableWithNoTracking(bool onlyNonDeleted = false);
        IQueryable<FormularyHeader> GetLatestFormulariesAsQueryable(bool onlyNonDeleted = false);
        IQueryable<FormularyHeader> GetAllFormulariesAsQueryableWithNoTracking(bool onlyNonDeleted = false);
        Task<bool> TruncateFormularyChangeLog();
        Task<bool> RefreshFormularyChangeLogMaterializedView();
        Task<bool> TruncateFormularies();
        Task<bool> UpdateAllStatusAsActive();
    }

}
