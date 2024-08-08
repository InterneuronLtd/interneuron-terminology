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
﻿using Interneuron.Terminology.Infrastructure.Tasks;
using Interneuron.Terminology.Repository.DBModelsContext;
using System;

namespace Interneuron.Terminology.Repository
{
    public class CleanupDBContextTask : IRequestCleanupTask
    {
        private TerminologyDBContext _dbContext;

        public CleanupDBContextTask(TerminologyDBContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void Execute(Exception error)
        {
            if (_dbContext != null)
            {
                if (error == null)
                {
                    _dbContext.SaveChanges();
                }
                _dbContext.Dispose();
            }
        }
    }
}
