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
﻿namespace Interneuron.Terminology.BackgroundTask.API.AppCode.Infrastructure
{
    public class APIRequestContext
    {
        public APIUser APIUser { get; set; }
        public string ClientId { get; set; }
        public string AuthToken { get; set; }

        public static Func<APIRequestContext> APIRequestContextProvider;

        public static APIRequestContext CurrentContext
        {
            get
            {
                if (APIRequestContextProvider == null)
                    throw new Exception("Context provider for the API is not setup");

                return APIRequestContextProvider();
            }
        }
    }

    public class APIUser
    {
        public string UserId { get; set; }

        public HashSet<string> UserScopes { get; set; }

        public HashSet<string> UserRoles { get; set; }

    }
}
