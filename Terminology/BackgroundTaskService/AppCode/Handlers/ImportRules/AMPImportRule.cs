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
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers.ImportRules
{
    public class AMPImportRule : IImportRule
    {
        private DMDDetailResultDTO _dMDDetailResultDTO;
        private FormularyHeader _formularyDAO;

        public AMPImportRule(DMDDetailResultDTO dMDDetailResultDTO, FormularyHeader formularyDAO)
        {
            _dMDDetailResultDTO = dMDDetailResultDTO;

            _formularyDAO = formularyDAO;
        }

        public void MutateByRules()
        {
            IsModifiedReleaseOrGastroResistant();
            IsPrescribable();
        }

        private void IsModifiedReleaseOrGastroResistant()
        {
            if (_formularyDAO == null || !_formularyDAO.FormularyDetail.IsCollectionValid()) return;

            var detailDAO = _formularyDAO.FormularyDetail.First();
            if (_formularyDAO.Name.Contains("modified-release", StringComparison.OrdinalIgnoreCase))
            {
                detailDAO.IsModifiedRelease = true;
            }
            if (_formularyDAO.Name.Contains("gastro-resistant", StringComparison.OrdinalIgnoreCase))
            {
                detailDAO.IsGastroResistant = true;
            }
        }

        private void IsPrescribable()
        {
            if (_formularyDAO == null || !_formularyDAO.FormularyDetail.IsCollectionValid()) return;

            var detail = _formularyDAO.FormularyDetail.First();

            if (detail != null)
            {
                detail.Prescribable = false;
                detail.PrescribableSource = TerminologyConstants.MANUAL_DATA_SRC;
            }
        }
    }
}
