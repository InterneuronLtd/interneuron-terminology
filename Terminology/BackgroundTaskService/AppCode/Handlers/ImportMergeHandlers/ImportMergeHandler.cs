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
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers;
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;

namespace Interneuron.Terminology.BackgroundTaskService.API.AppCode.Commands.ImportMergeHandlers
{
    public abstract class ImportMergeHandler
    {
        public DMDLookupProvider DMDLookupProvider { get; set; }
        public abstract void MergeFromExisting();

        protected List<FormularyLookupItemDTO> HandleFormularyLookupItems(List<FormularyLookupItemDTO> sourceLookupItems, List<FormularyLookupItemDTO> destinationLookupItems)
        {
            destinationLookupItems = destinationLookupItems.IsCollectionValid() ? destinationLookupItems : new List<FormularyLookupItemDTO>();
            var customManualSourceItems = destinationLookupItems.Where(rec => rec.Source == TerminologyConstants.MANUAL_DATA_SRC || rec.Source.IsEmpty()).ToList();

            //Delete indications from custom source - manually entered
            if (customManualSourceItems.IsCollectionValid())
                customManualSourceItems.Each(rec => destinationLookupItems.Remove(rec));

            if (sourceLookupItems.IsCollectionValid())
            {
                var destinationLookupItemsCodes = destinationLookupItems.Select(rec => rec.Cd).ToList();

                //Get custom added indications
                var manualSourceLookupItems = sourceLookupItems.Where(rec => !destinationLookupItemsCodes.Contains(rec.Cd) && (rec.Source == TerminologyConstants.MANUAL_DATA_SRC || rec.Source.IsEmpty())).ToList();

                manualSourceLookupItems?.Each(rec =>
                {
                    rec.Source = TerminologyConstants.MANUAL_DATA_SRC;
                    destinationLookupItems.Add(rec);
                });
            }

            return destinationLookupItems;
        }
    }
}