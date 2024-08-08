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
﻿using Interneuron.Terminology.Infrastructure.Domain;
using Interneuron.Terminology.Repository.DBModelsContext;
using Interneuron.Terminology.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.Repository
{
    public class ReadOnlyRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : EntityBase
    {
        private TerminologyDBContext _dbContext;
        private readonly DbSet<TEntity> _entities;

        public IQueryable<TEntity> ItemsAsReadOnly => _entities.AsNoTracking();

        public List<TEntity> ItemsAsListReadOnly => _entities.AsNoTracking().ToList();

        public Task<List<TEntity>> ItemsAsListAsyncReadOnly => _entities.AsNoTracking().ToListAsync();

        public string GetSql()
        {
            return _entities.ToSql();
        }

        public string GetSql<T>(IQueryable<T> query) where T : class
        {
            return query.ToSql();
        }

        public bool AddLike(string matchExpression, string pattern)
        {
            return EF.Functions.Like(matchExpression, pattern);
        }

        public bool AddILike(string matchExpression, string pattern)
        {
            return EF.Functions.ILike(matchExpression, pattern);
        }

        public IQueryable<T> AddILike<T>(IQueryable<T> query, Func<T, string> matchExpression, string pattern)
        {
            return query.Where(con => EF.Functions.ILike(matchExpression(con), pattern));
        }

     

        public ReadOnlyRepository(TerminologyDBContext dbContext)
        {
            _dbContext = dbContext;
            _entities = dbContext.Set<TEntity>();

            //var entityType = dbContext.Model.FindEntityType("");

            //// Table info 
            //var tableName = entityType.GetTableName();
            //var tableSchema = entityType.GetSchema();

            //// Column info 
            //foreach (var property in entityType.GetProperties())
            //{
            //    var columnName = property.GetColumnName();
            //    var columnType = property.GetColumnType();
            //};
        }
    }
}
