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
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.BackgroundTaskService.Infrastructure.Domain
{
    public class TerminologyConstants
    {
        public const string RECORDSTATUS_DRAFT = "001";
        public const string RECORDSTATUS_APPROVED = "002";
        public const string RECORDSTATUS_ACTIVE = "003";
        public const string RECORDSTATUS_ARCHIVED = "004";
        public const string RECORDSTATUS_INACTIVE = "005";
        public const string RECORDSTATUS_DELETED = "006";

        public const string FORMULARYSTATUS_FORMULARY = "001";
        public const string FORMULARYSTATUS_NONFORMULARY = "002";

        public const string DMD_DATA_SRC = "DMD";
        public const string FDB_DATA_SRC = "FDB";
        public const string MANUAL_DATA_SRC = "RNOH";

        public const string PRODUCT_TYPE_VTM = "VTM";
        public const string PRODUCT_TYPE_VMP = "VMP";
        public const string PRODUCT_TYPE_AMP = "AMP";

        public const string ROUTEFIELDTYPE_ADDITONAL = "001";
        public const string ROUTEFIELDTYPE_UNLICENSED = "002";
        public const string ROUTEFIELDTYPE_NORMAL = "003";

        public const string RECORD_SOURCE_IMPORT = "I";

        public const short STATUS_SUCCESS = 1;
        public const short STATUS_FAIL = 2;
        public const short STATUS_PARTIAL_SUCCESS = 3;
        public const short STATUS_BAD_REQUEST = 4;

        public const string DEFAULT_IDENTIFICATION_CODE_SYSTEM = "DMD";

        public const string Custom_IDENTIFICATION_CODE_SYSTEM = "Custom";


        public const string CODE_SYSTEM_CLASSIFICATION_TYPE = "Classification";

        public const string CODE_SYSTEM_IDENTIFICATION_TYPE = "Identification";

        public const string ROUTE_CODE_INHALATION = "18679011000001101";

        public const string STRINGIFIED_BOOL_TRUE = "1";
        public const string STRINGIFIED_BOOL_FALSE = "0";

        //Specific to postgres - so no need to put in config
        public static readonly string[] PG_ESCAPABLE_CHARS = new string[] { "&", "(", ")", "*" };

        public const string DEF_CLASS_CODES = "default_classification_codes";
        public const string DEF_BNF_CLASS_CODE = "default_bnf_classification_code";
        public const string DEF_ATC_CLASS_CODES = "default_atc_classification_code";
        public const string DEF_FDB_CLASS_CODES = "default_fdb_classification_code";
        public const string DEF_TEMPLATE_CLASS_CODES = "default_{template}_classification_code";

    }
}
