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
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers
{
    public class FormularyImportBaseHandler
    {
        public Func<DMDLookupProvider> getDMDLookupProvider = null;

        public void UpdateFormularyDetailDMDLookup(FormularyDetail formularyDetail)
        {
            if (formularyDetail == null) return;

            var dmdLookupProvider = getDMDLookupProvider();//let it throw error if not provided 

            formularyDetail.SupplierName = (dmdLookupProvider._supplierCodeNames.IsCollectionValid() && formularyDetail.SupplierCd != null && dmdLookupProvider._supplierCodeNames.ContainsKey(formularyDetail.SupplierCd)) ? dmdLookupProvider._supplierCodeNames[formularyDetail.SupplierCd] : formularyDetail.SupplierName;

            formularyDetail.RestrictionsOnAvailabilityDesc = (dmdLookupProvider._availrestrictLkp.IsCollectionValid() && formularyDetail.RestrictionsOnAvailabilityCd != null && dmdLookupProvider._availrestrictLkp.ContainsKey(formularyDetail.RestrictionsOnAvailabilityCd)) ? dmdLookupProvider._availrestrictLkp[formularyDetail.RestrictionsOnAvailabilityCd] : formularyDetail.RestrictionsOnAvailabilityDesc;

            formularyDetail.BasisOfPreferredNameDesc = (dmdLookupProvider._basisOfNameLkp.IsCollectionValid() && formularyDetail.BasisOfPreferredNameCd != null && dmdLookupProvider._basisOfNameLkp.ContainsKey(formularyDetail.BasisOfPreferredNameCd)) ? dmdLookupProvider._basisOfNameLkp[formularyDetail.BasisOfPreferredNameCd] : formularyDetail.BasisOfPreferredNameDesc;

            formularyDetail.ControlledDrugCategoryDesc = (dmdLookupProvider._controllerDrugCtgLkp.IsCollectionValid() && formularyDetail.ControlledDrugCategoryCd != null && dmdLookupProvider._controllerDrugCtgLkp.ContainsKey(formularyDetail.ControlledDrugCategoryCd)) ? dmdLookupProvider._controllerDrugCtgLkp[formularyDetail.ControlledDrugCategoryCd] : formularyDetail.ControlledDrugCategoryDesc;

            formularyDetail.DoseFormDesc = (dmdLookupProvider._doseFormsLkp.IsCollectionValid() && formularyDetail.DoseFormCd != null && dmdLookupProvider._doseFormsLkp.ContainsKey(formularyDetail.DoseFormCd)) ? dmdLookupProvider._doseFormsLkp[formularyDetail.DoseFormCd] : formularyDetail.DoseFormDesc;

            formularyDetail.FormDesc = (dmdLookupProvider._formCodeNames.IsCollectionValid() && formularyDetail.FormCd != null && dmdLookupProvider._formCodeNames.ContainsKey(formularyDetail.FormCd)) ? dmdLookupProvider._formCodeNames[formularyDetail.FormCd] : formularyDetail.FormDesc;

            formularyDetail.CurrentLicensingAuthorityDesc = (dmdLookupProvider._licAuthLkp.IsCollectionValid() && formularyDetail.CurrentLicensingAuthorityCd != null && dmdLookupProvider._licAuthLkp.ContainsKey(formularyDetail.CurrentLicensingAuthorityCd)) ? dmdLookupProvider._licAuthLkp[formularyDetail.CurrentLicensingAuthorityCd] : formularyDetail.CurrentLicensingAuthorityDesc;

            formularyDetail.PrescribingStatusDesc = (dmdLookupProvider._prescribingStsLkp.IsCollectionValid() && formularyDetail.PrescribingStatusCd != null && dmdLookupProvider._prescribingStsLkp.ContainsKey(formularyDetail.PrescribingStatusCd)) ? dmdLookupProvider._prescribingStsLkp[formularyDetail.PrescribingStatusCd] : formularyDetail.PrescribingStatusDesc;

            formularyDetail.UnitDoseUnitOfMeasureDesc = (dmdLookupProvider._uomsLkp.IsCollectionValid() && formularyDetail.UnitDoseUnitOfMeasureCd != null && dmdLookupProvider._uomsLkp.ContainsKey(formularyDetail.UnitDoseUnitOfMeasureCd)) ? dmdLookupProvider._uomsLkp[formularyDetail.UnitDoseUnitOfMeasureCd] : formularyDetail.UnitDoseUnitOfMeasureDesc;

            formularyDetail.UnitDoseFormUnitsDesc = (dmdLookupProvider._uomsLkp.IsCollectionValid() && formularyDetail.UnitDoseFormUnits != null && dmdLookupProvider._uomsLkp.ContainsKey(formularyDetail.UnitDoseFormUnits)) ? dmdLookupProvider._uomsLkp[formularyDetail.UnitDoseFormUnits] : formularyDetail.UnitDoseFormUnitsDesc;
        }

        public void UpdateFormularyExcipientDMDLookup(FormularyExcipient excipient)
        {
            if (excipient == null) return;
            var dmdLookupProvider = getDMDLookupProvider();//let it throw error if not provided 

            excipient.StrengthUnitDesc = excipient.StrengthUnitCd != null && dmdLookupProvider._uomsLkp.ContainsKey(excipient.StrengthUnitCd) ? dmdLookupProvider._uomsLkp[excipient.StrengthUnitCd] : excipient.StrengthUnitDesc;
            excipient.IngredientName = excipient.IngredientCd != null && dmdLookupProvider._ingredientsLkp.ContainsKey(excipient.IngredientCd) ? dmdLookupProvider._ingredientsLkp[excipient.IngredientCd] : excipient.IngredientName;
        }
        public void UpdateFormularyRoutesDMDLookup(FormularyRouteDetail routeDetail)
        {
            if (routeDetail == null) return;
            var dmdLookupProvider = getDMDLookupProvider();//let it throw error if not provided 

            routeDetail.RouteDesc = routeDetail.RouteCd.IsEmpty() || !dmdLookupProvider._routesLkp.IsCollectionValid() || !dmdLookupProvider._routesLkp.ContainsKey(routeDetail.RouteCd) ? routeDetail.RouteDesc :
                dmdLookupProvider._routesLkp[routeDetail.RouteCd];
        }

        public void UpdateFormularyLocalRoutesDMDLookup(FormularyLocalRouteDetail routeDetail)
        {
            if (routeDetail == null) return;
            var dmdLookupProvider = getDMDLookupProvider();//let it throw error if not provided 

            routeDetail.RouteDesc = routeDetail.RouteCd.IsEmpty() || !dmdLookupProvider._routesLkp.IsCollectionValid() || !dmdLookupProvider._routesLkp.ContainsKey(routeDetail.RouteCd) ? routeDetail.RouteDesc : dmdLookupProvider._routesLkp[routeDetail.RouteCd];
        }

        public void UpdateFormularyIngredientsDMDLookup(FormularyIngredient ingredient)
        {
            if (ingredient == null) return;
            var dmdLookupProvider = getDMDLookupProvider();//let it throw error if not provided 

            ingredient.BasisOfPharmaceuticalStrengthDesc = ingredient.BasisOfPharmaceuticalStrengthCd.IsEmpty() || !dmdLookupProvider._strengthsLkp.IsCollectionValid() || (dmdLookupProvider._strengthsLkp.ContainsKey(ingredient.BasisOfPharmaceuticalStrengthCd) == false) ? ingredient.BasisOfPharmaceuticalStrengthDesc : dmdLookupProvider._strengthsLkp[ingredient.BasisOfPharmaceuticalStrengthCd];

            ingredient.IngredientName = ingredient.IngredientCd.IsEmpty() || !dmdLookupProvider._ingredientsLkp.IsCollectionValid() || (dmdLookupProvider._ingredientsLkp?.ContainsKey(ingredient.IngredientCd) == false) ? ingredient.IngredientName : dmdLookupProvider._ingredientsLkp[ingredient.IngredientCd];

            ingredient.StrengthValueNumeratorUnitDesc = ingredient.StrengthValueNumeratorUnitCd.IsEmpty() || !dmdLookupProvider._uomsLkp.IsCollectionValid() || (dmdLookupProvider._uomsLkp?.ContainsKey(ingredient.StrengthValueNumeratorUnitCd) == false) ? ingredient.StrengthValueNumeratorUnitDesc : dmdLookupProvider._uomsLkp[ingredient.StrengthValueNumeratorUnitCd];

            ingredient.StrengthValueDenominatorUnitDesc = ingredient.StrengthValueDenominatorUnitCd.IsEmpty() || !dmdLookupProvider._uomsLkp.IsCollectionValid() || (dmdLookupProvider._uomsLkp?.ContainsKey(ingredient.StrengthValueDenominatorUnitCd) == false) ? ingredient.StrengthValueDenominatorUnitDesc : dmdLookupProvider._uomsLkp[ingredient.StrengthValueDenominatorUnitCd];
        }
    }
}
