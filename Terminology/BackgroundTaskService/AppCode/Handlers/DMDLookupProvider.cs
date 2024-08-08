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
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers
{
    public class DMDLookupProvider
    {
        public List<DmdATCCodeDTO>? _dmdATCCodes { get; private set; } = new List<DmdATCCodeDTO>();
        public List<DmdBNFCodeDTO>? _dmdBNFCodes { get; private set; } = new List<DmdBNFCodeDTO>();
        public Dictionary<string?, string>? _supplierCodeNames { get; private set; } = new Dictionary<string?, string>();
        public Dictionary<string, string>? _formCodeNames { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _availrestrictLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _basisOfNameLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _controllerDrugCtgLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _uomsLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _doseFormsLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _licAuthLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _prescribingStsLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _strengthsLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _ingredientsLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? _routesLkp { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, DmdLookupRouteDTO>? _routesLkpWithAllAttributes { get; private set; } = new Dictionary<string, DmdLookupRouteDTO>();
        public Dictionary<string, DmdLookupRouteDTO>? _routesLkpWithAllAttributesForPrevCode { get; private set; } = new Dictionary<string, DmdLookupRouteDTO>();

        public Dictionary<string, string>? _bnfsLkp { get; private set; } = new Dictionary<string, string>();   

        public TerminologyAPIService _terminologyAPIService { get; private set; }

        private DMDLookupProvider()
        {   
        }

        public static async Task<DMDLookupProvider> CreateAsync(TerminologyAPIService terminologyAPIService)
        {
            var inst = new DMDLookupProvider();
            inst._terminologyAPIService = terminologyAPIService;
            await inst.PrefillDMDLookup();
            return inst;
        }

        private async Task PrefillDMDLookup()
        {
            var suppliersTask = _terminologyAPIService.GetSupplierLookup(true);
            var formsTask = _terminologyAPIService.GetFormLookup(true);
            var availRestTask = _terminologyAPIService.GetDMDAvailRestrictionsLookup(true);
            var basisOfNameTask = _terminologyAPIService.GetDMDBasisOfNameLookup(true);
            var controlleredDrugTask = _terminologyAPIService.GetControlDrugCategoryLookup(true);
            var uomTask = _terminologyAPIService.GetDMDUOMLookup(true);
            var doseFormTask = _terminologyAPIService.GetDMDDoseFormLookup(true);
            var licAuthTask = _terminologyAPIService.GetLicensingAuthorityLookup(true);
            var prescribingTask = _terminologyAPIService.GetPrescribingStatusLookup(true);
            var strengthsTask = _terminologyAPIService.GetDMDPharamceuticalStrengthLookup(true);
            var ingredientsTask = _terminologyAPIService.GetDMDIngredientLookup(true);
            var routesTask = _terminologyAPIService.GetRouteLookup(true);
            var bnfsTask = _terminologyAPIService.GetBNFLookup(true);

            //var dmdATCCodesTask = _terminologyAPIService.GetAllATCCodesFromDMD(true);

            var dmdBNFCodesTask = _terminologyAPIService.GetAllBNFCodesFromDMD(true);


            //await Task.WhenAll(suppliersTask, formsTask, availRestTask, basisOfNameTask, controlleredDrugTask, uomTask, doseFormTask, licAuthTask, prescribingTask, strengthsTask, ingredientsTask, routesTask, dmdATCCodesTask, dmdBNFCodesTask, bnfsTask);
            await Task.WhenAll(suppliersTask, formsTask, availRestTask, basisOfNameTask, controlleredDrugTask, uomTask, doseFormTask, licAuthTask, prescribingTask, strengthsTask, ingredientsTask, routesTask, dmdBNFCodesTask, bnfsTask);

            var suppliersLkp = (await suppliersTask)?.Data;
            var formsLkp = (await formsTask)?.Data;
            var availRestLkp = (await availRestTask)?.Data;
            var basisOfNameLkp = (await basisOfNameTask)?.Data;
            var contollerDrugLkp = (await controlleredDrugTask)?.Data;
            var uomLkp = (await uomTask)?.Data;
            var doseFormLkp = (await doseFormTask)?.Data;
            var licAuthLkp = (await licAuthTask)?.Data;
            var prescribingLkp = (await prescribingTask)?.Data;
            var strenghtsLkp = (await strengthsTask)?.Data;
            var ingredientsLkp = (await ingredientsTask)?.Data;
            var routesLkp = (await routesTask)?.Data;
            var bnfsLkp = (await bnfsTask)?.Data;

            //_dmdATCCodes = (await dmdATCCodesTask)?.Data;
            _dmdBNFCodes = (await dmdBNFCodesTask)?.Data;

            if (suppliersLkp.IsCollectionValid())
                _supplierCodeNames = suppliersLkp?.AsParallel().Where(rec=> rec.Cd.IsNotEmpty() && rec.Invalid != 1)?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd, v => v.Desc);

            if (formsLkp.IsCollectionValid())
                _formCodeNames = formsLkp?.AsParallel().Where(rec => rec.Cd.IsNotEmpty())?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd, v => v.Desc);
            
            if (availRestLkp.IsCollectionValid())
                _availrestrictLkp = availRestLkp?.AsParallel().Where(rec => rec.Cd != null)?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd.ToString(), v => v.Desc);

            if (basisOfNameLkp.IsCollectionValid())
                _basisOfNameLkp = basisOfNameLkp?.AsParallel().Where(rec => rec.Cd != null)?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd.ToString(), v => v.Desc);
            
            if (contollerDrugLkp.IsCollectionValid())
                _controllerDrugCtgLkp = contollerDrugLkp?.AsParallel().Where(rec => rec.Cd != null)?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd.ToString(), v => v.Desc);
            
            if (uomLkp.IsCollectionValid())
                _uomsLkp = uomLkp?.AsParallel().Where(rec => rec.Cd.IsNotEmpty())?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd, v => v.Desc);
            
            if (doseFormLkp.IsCollectionValid())
                _doseFormsLkp = doseFormLkp?.AsParallel().Where(rec => rec.Cd != null)?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd.ToString(), v => v.Desc);
            
            if (licAuthLkp.IsCollectionValid())
                _licAuthLkp = licAuthLkp?.AsParallel().Where(rec => rec.Cd != null)?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd.ToString(), v => v.Desc);
            
            if (prescribingLkp.IsCollectionValid())
                _prescribingStsLkp = prescribingLkp?.AsParallel().Where(rec => rec.Cd != null)?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd.ToString(), v => v.Desc);
            
            if (strenghtsLkp.IsCollectionValid())
                _strengthsLkp = strenghtsLkp?.AsParallel().Where(rec => rec.Cd != null)?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd.ToString(), v => v.Desc);
            
            if (ingredientsLkp.IsCollectionValid())
                _ingredientsLkp = ingredientsLkp?.AsParallel().Where(rec => rec.Isid != null && rec.Invalid != 1)?.Distinct(rec => rec.Isid).ToDictionary(k => k.Isid, v => v.Nm);

            if (routesLkp.IsCollectionValid())
            {
                _routesLkp = routesLkp?.AsParallel().Where(rec => rec.Cd.IsNotEmpty())?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd, v => v.Desc);
                _routesLkpWithAllAttributes = routesLkp.Where(rec => rec.Cd.IsNotEmpty())?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd, v => v);
                _routesLkpWithAllAttributesForPrevCode = routesLkp.Where(rec => rec.Cdprev.IsNotEmpty())?.Distinct(rec => rec.Cdprev).ToDictionary(k => k.Cdprev, v => v);
            }

            if (bnfsLkp.IsCollectionValid())
                _bnfsLkp = bnfsLkp?.AsParallel().Where(rec => rec.Cd.IsNotEmpty())?.Distinct(rec => rec.Cd).ToDictionary(k => k.Cd, v => v.Desc);
        }
    }
}
