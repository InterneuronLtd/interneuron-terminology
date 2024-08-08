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
﻿using AutoMapper;
using Interneuron.Common.Extensions;
using Interneuron.Terminology.BackgroundTaskService.AppCode.DataService.APIModels;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers;
using Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain;
using Interneuron.Terminology.BackgroundTaskService.Model;
using Interneuron.Terminology.BackgroundTaskService.Model.DomainModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.AutoMapperProfiles
{
    internal class FormularyMapperProfile : Profile
    {
        public FormularyMapperProfile()
        {
            CreateMap<DmdAmpExcipientDTO, FormularyExcipient>()
                .ForMember(dest => dest.IngredientCd, opt => opt.MapFrom(src => src.Isid))
                .ForMember(dest => dest.Strength, opt => opt.MapFrom(src => src.Strnth))
                .ForMember(dest => dest.StrengthUnitCd, opt => opt.MapFrom(src => src.StrnthUomcd));

            CreateMap<DmdATCCodeDTO, FormularyAdditionalCode>()
                .ForMember(dest => dest.AdditionalCode, opt => opt.MapFrom(src => src.Cd))
                .ForMember(dest => dest.AdditionalCodeDesc, opt => opt.MapFrom(src => src.Desc))
                .ForMember(dest => dest.AdditionalCodeSystem, opt => opt.MapFrom(src => "ATC"))
                .ForMember(dest => dest.CodeType, opt => opt.MapFrom(src => TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => TerminologyConstants.DMD_DATA_SRC));

            CreateMap<DmdBNFCodeDTO, FormularyAdditionalCode>()
                .ForMember(dest => dest.AdditionalCode, opt => opt.MapFrom(src => src.Cd))
                .ForMember(dest => dest.AdditionalCodeDesc, opt => opt.MapFrom(src => src.Desc))
                .ForMember(dest => dest.AdditionalCodeSystem, opt => opt.MapFrom(src => "BNF"))
                .ForMember(dest => dest.CodeType, opt => opt.MapFrom(src => TerminologyConstants.CODE_SYSTEM_CLASSIFICATION_TYPE))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => TerminologyConstants.DMD_DATA_SRC));

            CreateMap<FormularyHeader, FormularyHeader>()
                .ForMember(dest => dest.NameTokens, opt => opt.Ignore())
                .ForMember(dest => dest.ParentNameTokens, opt => opt.Ignore())
                .ForMember(dest => dest.CodeSystem, opt => opt.MapFrom(src => src.CodeSystem.IsNotEmpty() ? src.CodeSystem.Trim() : TerminologyConstants.DEFAULT_IDENTIFICATION_CODE_SYSTEM));

            CreateMap<FormularyDetail, FormularyDetail>();

            CreateMap<FormularyIngredient, FormularyIngredient>();

            CreateMap<FormularyExcipient, FormularyExcipient>();

            CreateMap<FormularyAdditionalCode, FormularyAdditionalCode>();

            CreateMap<FormularyIndication, FormularyIndication>();

            CreateMap<FormularyRouteDetail, FormularyRouteDetail>();
            CreateMap<FormularyLocalRouteDetail, FormularyLocalRouteDetail>();

            CreateMap<FormularyOntologyForm, FormularyOntologyForm>();


            CreateMap<FormularyLocalRouteDetailDTO, FormularyLocalRouteDetail>();

            CreateMap<FormularyLocalRouteDetail, FormularyLocalRouteDetailDTO>();

            
            CreateMap<DmdLookupRouteDTO, FormularyRouteDetail>()
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source.IsEmpty() ? TerminologyConstants.MANUAL_DATA_SRC : src.Source.Trim()))
                .ForMember(dest => dest.RouteCd, opt => opt.MapFrom(src => src.Cd))
                .ForMember(dest => dest.RouteFieldTypeCd, opt => opt.MapFrom(src => TerminologyConstants.ROUTEFIELDTYPE_NORMAL)); //Normal

            CreateMap<DmdAmpDrugrouteDTO, FormularyRouteDetail>()
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => TerminologyConstants.MANUAL_DATA_SRC))
                .ForMember(dest => dest.RouteCd, opt => opt.MapFrom(src => src.Routecd))
                .ForMember(dest => dest.RouteFieldTypeCd, opt => opt.MapFrom(src => TerminologyConstants.ROUTEFIELDTYPE_NORMAL)); //Normal

            CreateMap<DmdVmpDrugrouteDTO, FormularyRouteDetail>()
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => TerminologyConstants.MANUAL_DATA_SRC))
                .ForMember(dest => dest.RouteCd, opt => opt.MapFrom(src => src.Routecd))
                .ForMember(dest => dest.RouteFieldTypeCd, opt => opt.MapFrom(src => TerminologyConstants.ROUTEFIELDTYPE_ADDITONAL)); //Additional

            CreateMap<FormularyChangeLogDTO, FormularyChangeLog>();

            CreateMap<FormularyExcipient, DMDFormularyExcipient>();

            CreateMap<FormularyAdditionalCode, DMDFormularyAdditionalCode>();
            CreateMap<FormularyIngredient, DMDFormularyIngredient>();
            CreateMap<FormularyRouteDetail, DMDFormularyRouteDetail>()
                .ForMember(dest => dest.RouteFieldType, opt => opt.MapFrom((src, dest) => { if (src != null && src.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED) return "UnLicensed"; else return "Licensed"; }));
            
            CreateMap<FormularyLocalRouteDetail, DMDFormularyRouteDetail>()
                .ForMember(dest => dest.RouteFieldType, opt => opt.MapFrom((src, dest) => { if (src != null && src.RouteFieldTypeCd == TerminologyConstants.ROUTEFIELDTYPE_UNLICENSED) return "UnLicensed"; else return "Licensed"; }));
            CreateMap<FormularyDetail, DMDComparableProductDetail>()
                .ForMember(dest => dest.LocalLicensedUses, opt => opt.MapFrom((srcData) => FillCodeDescList(srcData.LocalLicensedUse)))
                .ForMember(dest => dest.LocalUnLicensedUses, opt => opt.MapFrom((srcData) => FillCodeDescList(srcData.LocalUnlicensedUse)));
            CreateMap<FormularyHeader, DMDComparableProductDetail>();
        }

        private List<DMDFormularyLookupItem> FillCodeDescList(string dataAsString)
        {
            try
            {
                if (dataAsString.IsEmpty()) return null;

                var dataAsList = JsonConvert.DeserializeObject<List<DMDFormularyLookupItem>>(dataAsString);//id and text

                if (dataAsList == null) return null;

                return dataAsList;
            }
            catch { }
            return null;
        }
    }
}
