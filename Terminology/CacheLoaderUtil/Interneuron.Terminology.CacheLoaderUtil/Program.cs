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
﻿using Interneuron.Terminology.CacheLoaderUtil.Config;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace Interneuron.Terminology.CacheLoaderUtil
{

    public class Program
    {
        private static AccessTokenConfig AccessTokenConfig;
        private static FormularyCacheConfig FormularyCacheConfig;
        private static LookupCacheConfig LookupCacheConfig;


        public static async Task Main(string[] args)
        {
            //Load from configuration
            LoadConfigurationData();

            string option = null;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Select any of these options:");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("1) Flush all and load cache - Type 1");
            Console.WriteLine("2) Load Active Formulary only - Type 2");
            Console.WriteLine("3) Load Lookup data only - Type 3");
            Console.WriteLine("4) To Exit - Type e");

            while (option == null || option == "")
            {
                option = Console.ReadLine();
                option = option.Trim();

                if (option != "1" && option != "2" && option != "3" && option != "e")
                    option = null;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;



            switch (option)
            {
                case "1":
                    await ReloadCache(FormularyCacheConfig.Url, "Formulary");
                    await ReloadCache(LookupCacheConfig.Url, "Lookup");
                    break;
                case "2":
                    //Invoking reloading of formulary cache
                    await ReloadCache(FormularyCacheConfig.Url, "Formulary");
                    break;
                case "3":
                    //Invoking reloading of lookup cache
                    await ReloadCache(LookupCacheConfig.Url, "Lookup");
                    break;
                case "e":
                    break;
                default:
                    break;
            }
        }

        private static void LoadConfigurationData()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false);

            IConfiguration config = builder.Build();

            AccessTokenConfig = config.GetSection("access_token_ep").Get<AccessTokenConfig>();
            FormularyCacheConfig = config.GetSection("formulary_cache_ep").Get<FormularyCacheConfig>();
            LookupCacheConfig = config.GetSection("lookup_cache_ep").Get<LookupCacheConfig>();

        }

        private static async Task ReloadCache(Uri uri, string type, Method method = Method.Get)
        {
            //Get access token
            var accessToken = await GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
                return;


            Console.WriteLine($"Invoking {type} cache reload..");

            try
            {
                using var client = new RestClient(uri);
                var request = new RestRequest() { Method = method, Timeout = -1 };
                request.AddHeader("Authorization", $"Bearer {accessToken}");
                request.AddHeader("Content-Type", "application/json");

                var response = await client.ExecuteAsync(request);

                if (response == null || response.ResponseStatus == null || response.ResponseStatus != ResponseStatus.Completed || response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"Error invoking {type} cache reload. Please check the configuration.");
                    return;
                }

                Console.WriteLine($"Invoking {type} cache reload complete..");
                return;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception {type} Formulary cache reload.");
                Console.WriteLine($"Exception: {ex.ToString()}");
            }
        }

        private static async Task<string> GetAccessToken()
        {
            //Invoke cache api endpoint
            Console.WriteLine("Getting access token..");

            try
            {
                using var client = new RestClient(AccessTokenConfig.Url);
                var request = new RestRequest() { Method = Method.Post, Timeout = -1 };
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

                foreach (var param in AccessTokenConfig.Params)
                {
                    request.AddParameter(param.Key, param.Value);
                }

                var response = await client.ExecuteAsync<AccessTokenDetail>(request);

                if (response == null || response.Data == null || string.IsNullOrEmpty(response.Data.Access_Token))
                {
                    Console.WriteLine("Error getting access token. Cannot continue further. Please check the configuration.");
                    return null;
                }

                Console.WriteLine("Received access token..");
                Console.WriteLine(response.Data.Access_Token);
                return response.Data.Access_Token;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Exception getting access token. Cannot continue further.");
                Console.WriteLine($"Exception: {ex.ToString()}");

                return null;
            }
            
        }
    }
}


