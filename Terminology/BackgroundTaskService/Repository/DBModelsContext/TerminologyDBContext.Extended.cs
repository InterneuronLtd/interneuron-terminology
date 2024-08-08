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
﻿using Microsoft.EntityFrameworkCore;

namespace Interneuron.Terminology.BackgroundTaskService.Repository.DBModelsContext
{
    public partial class TerminologyDBContext : DbContext
    {
        private IConfiguration _configuration;

        public TerminologyDBContext(IConfiguration configuration)
        {
            _configuration = configuration;
            //AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public TerminologyDBContext(DbContextOptions<TerminologyDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
            //AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //            if (!optionsBuilder.IsConfigured)
            //            {
            //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            //                optionsBuilder.UseNpgsql("Server=interneuron-ne-db-test.postgres.database.azure.com;User Id=nedbtestadmin@interneuron-ne-db-test;Password=N3ur0n!nedbtest;Database=mcc_terminology;Port=5432;SSL Mode=Require;");
            //            }

            if (!optionsBuilder.IsConfigured)
            {
                var connString = _configuration.GetValue<string>("TerminologyBackgroundTaskConfig:Connectionstring");
                if (!connString.Contains("CommandTimeout"))
                    connString += $";CommandTimeout=0;";
                optionsBuilder.UseNpgsql(connString, o =>
                {
                    o.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                });
            }
        }
    }
}
