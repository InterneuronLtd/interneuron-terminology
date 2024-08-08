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
﻿using Interneuron.Terminology.Model.DomainModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Interneuron.Terminology.API.AppCode.Queries
{
    public class RuleBoundFormularyBuilderOrchestrator
    {
        private RuleBoundBaseFormularyBuilder _builder;
        private IServiceProvider _serviceProvider;

        public RuleBoundFormularyBuilderOrchestrator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public RuleBoundFormularyBuilderOrchestrator(RuleBoundBaseFormularyBuilder builder)
        {
            _builder = builder;
        }

        public async Task BuildFormulary(FormularyHeader formularyDAO, List<FormularyHeader> ancestors, List<FormularyHeader> descendents, bool getAllAddnlCodes = false)
        {
            _builder.AncestorFormularies = ancestors;
            _builder.DescendentFormularies = descendents;

            await _builder.CreateBase(formularyDAO);
            _builder.CreateDetails();
            await _builder.CreateAdditionalCodes(getAllAddnlCodes);
            _builder.CreateIngredients();
            _builder.CreateExcipients();
            _builder.CreateRouteDetails();
            _builder.CreateLocalRouteDetails();
            //_builder.CreateOntologyForms();
            //_builder.CreateIndications();
            await _builder.HydrateLookupDescriptions();
        }

        public async Task BuildBasicFormulary(FormularyHeader formularyDAO, List<FormularyHeader> ancestors = null, List<FormularyHeader> descendents = null)
        {
            _builder.AncestorFormularies = ancestors;
            _builder.DescendentFormularies = descendents;

            await _builder.CreateBasicActiveFormularyBase(formularyDAO);
            _builder.CreateBasicActiveFormularyDetails();
            await _builder.CreateAdditionalCodes();
            
            await _builder.HydrateLookupDescriptions(true);
        }
    }
}
