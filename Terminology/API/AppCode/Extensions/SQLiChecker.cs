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
﻿using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Interneuron.Infrastructure.CustomExceptions;


namespace Interneuron.Terminology.API.AppCode.Extensions

{
    public static class SQLiChecker
    {
        public static bool CheckStringForSQLi(this string parameter, IConfiguration configuration)
        {


            var RegexConfigSection = configuration.GetSection("BlackListRegex");

            string regex = RegexConfigSection["global"];

            CheckBlacklistedKeywords(parameter, regex);


            return true;
        }
        public static void CheckBlacklistedKeywords(string statement, string regex)
        {
            if (!string.IsNullOrEmpty(statement))
            {
                Match m = Regex.Match(statement, regex);
                if (m.Success)
                {
                    throw new InterneuronBusinessException(400, "Invalid Sql Keyword");

                }
            }
        }
    }
}
