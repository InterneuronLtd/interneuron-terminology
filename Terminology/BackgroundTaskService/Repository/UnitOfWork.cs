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
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;
using Interneuron.Terminology.BackgroundTaskService.Model.Search;
using Interneuron.Terminology.BackgroundTaskService.Repository.DBModelsContext;
using Microsoft.EntityFrameworkCore.Storage;

namespace Interneuron.Terminology.BackgroundTaskService.Repository
{
    public class UnitOfWork : IDisposable, IUnitOfWork
    {
        private bool _disposed = false;
        private IFormularyRepository<FormularyHeader> _formularyHeaderFormularyRepository;
        private IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private IFormularyRepository<FormularyIngredient> _formularyIngredientFormularyRepository;
        private IEnumerable<IEntityEventHandler> _entityEventHandlers;
        private TerminologyDBContext _context;
        private IDbContextTransaction _objTran;
        private IFormularyRepository<FormularyAdditionalCode> _formularyAdditionalCodeFormularyRepository;
        private IRepository<FormularyRouteDetail> _formularyRouteRepository;
        private IFormularyRepository<FormularyDetail> _formularyDetailFormularyRepository;
        private IFormularyRepository<FormularyLocalRouteDetail> _formularyLocalRouteFormularyRepository;
        private IFormularyRepository<FormularyBasicSearchResultModel> _formularyBasicResultsFormularyRepository;
        private IFormularyRepository<FormularyChangeLog> _formularyChangeLogFormularyRepository;
        private ITerminologyRepository<FormularyHeader> _terminologyRepository;

        public UnitOfWork(IServiceProvider serviceProvider, IConfiguration configuration, IEnumerable<IEntityEventHandler> entityEventHandlers, TerminologyDBContext terminologyDBContext)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _entityEventHandlers = entityEventHandlers;
            _context = terminologyDBContext;// new TerminologyDBContext(configuration);
        }

        public ITerminologyRepository<FormularyHeader> TerminologyRepository
        {
            get
            {
                if (_terminologyRepository == null)
                {
                    _terminologyRepository = new TerminologyRepository<FormularyHeader>(_context, _configuration, _entityEventHandlers);
                }
                return _terminologyRepository;
            }
        }

        public IFormularyRepository<FormularyHeader> FormularyHeaderFormularyRepository
        {
            get
            {
                if (_formularyHeaderFormularyRepository == null)
                {
                    _formularyHeaderFormularyRepository = new FormularyRepository<FormularyHeader>(_context, _configuration, _entityEventHandlers);
                }
                return _formularyHeaderFormularyRepository;
            }
        }

        public IFormularyRepository<FormularyChangeLog> FormularyChangeLogFormularyRepository
        {
            get
            {
                if (_formularyChangeLogFormularyRepository == null)
                {
                    _formularyChangeLogFormularyRepository = new FormularyRepository<FormularyChangeLog>(_context, _configuration, _entityEventHandlers);
                }
                return _formularyChangeLogFormularyRepository;
            }
        }


        public IFormularyRepository<FormularyAdditionalCode> FormularyAdditionalCodeFormularyRepository
        {
            get
            {
                if (_formularyAdditionalCodeFormularyRepository == null)
                {
                    _formularyAdditionalCodeFormularyRepository = new FormularyRepository<FormularyAdditionalCode>(_context, _configuration, _entityEventHandlers);
                }
                return _formularyAdditionalCodeFormularyRepository;
            }
        }

        public IFormularyRepository<FormularyIngredient> FormularyIngredientFormularyRepository
        {
            get
            {
                if (_formularyIngredientFormularyRepository == null)
                {
                    _formularyIngredientFormularyRepository = new FormularyRepository<FormularyIngredient>(_context, _configuration, _entityEventHandlers);
                }
                return _formularyIngredientFormularyRepository;
            }
        }

        public IRepository<FormularyRouteDetail> FormularyRouteRepository
        {
            get
            {
                if (_formularyRouteRepository == null)
                {
                    _formularyRouteRepository = new Repository<FormularyRouteDetail>(_context, _entityEventHandlers);
                }
                return _formularyRouteRepository;
            }
        }

        public IFormularyRepository<FormularyDetail> FormularyDetailFormularyRepository
        {
            get
            {
                if (_formularyDetailFormularyRepository == null)
                {
                    _formularyDetailFormularyRepository = new FormularyRepository<FormularyDetail>(_context, _configuration, _entityEventHandlers);
                }
                return _formularyDetailFormularyRepository;
            }
        }

        public IFormularyRepository<FormularyLocalRouteDetail> FormularyLocalRouteFormularyRepository
        {
            get
            {
                if (_formularyLocalRouteFormularyRepository == null)
                {
                    _formularyLocalRouteFormularyRepository = new FormularyRepository<FormularyLocalRouteDetail>(_context, _configuration, _entityEventHandlers);
                }
                return _formularyLocalRouteFormularyRepository;
            }
        }

        public IFormularyRepository<FormularyBasicSearchResultModel> FormularyBasicResultsFormularyRepository
        {
            get
            {
                if (_formularyBasicResultsFormularyRepository == null)
                {
                    _formularyBasicResultsFormularyRepository = new FormularyRepository<FormularyBasicSearchResultModel>(_context, _configuration, _entityEventHandlers);
                }
                return _formularyBasicResultsFormularyRepository;
            }
        }

        public TerminologyDBContext Context => _context;

        //This CreateTransaction() method will create a database Trnasaction so that we can do database operations by
        //applying do evrything and do nothing principle
        public void CreateTransaction()
        {
            _objTran = _context.Database.BeginTransaction();
        }
        //If all the Transactions are completed successfuly then we need to call this Commit() 
        //method to Save the changes permanently in the database
        public void Commit()
        {
            _objTran.Commit();
        }
        //If atleast one of the Transaction is Failed then we need to call this Rollback() 
        //method to Rollback the database changes to its previous state
        public void Rollback()
        {
            _objTran.Rollback();
            _objTran.Dispose();
        }


        public int Save()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveAsync()
        {
          return await _context.SaveChangesAsync();
        }

        ~UnitOfWork() => Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public interface IUnitOfWork
    {
        IFormularyRepository<FormularyChangeLog> FormularyChangeLogFormularyRepository { get; }

        IFormularyRepository<FormularyHeader> FormularyHeaderFormularyRepository { get; }
        IFormularyRepository<FormularyAdditionalCode> FormularyAdditionalCodeFormularyRepository { get; }
        IFormularyRepository<FormularyIngredient> FormularyIngredientFormularyRepository { get; }
        IRepository<FormularyRouteDetail> FormularyRouteRepository { get; } 
        IFormularyRepository<FormularyDetail> FormularyDetailFormularyRepository { get; }
        IFormularyRepository<FormularyLocalRouteDetail> FormularyLocalRouteFormularyRepository { get; }
        IFormularyRepository<FormularyBasicSearchResultModel> FormularyBasicResultsFormularyRepository { get; }
        ITerminologyRepository<FormularyHeader> TerminologyRepository { get; }

        TerminologyDBContext Context { get; }
        void CreateTransaction();
        void Commit();
        void Rollback();
        int Save();
        Task<int> SaveAsync();

        void Dispose();
    }
}
