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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Extensions
{
    public class CommonUtil
    {
        public static bool DoesFileExists(string folderPath, string fileName)
        {
            if (folderPath.IsEmpty() || fileName.IsEmpty()) return false;
            try
            {
                return File.Exists(Path.Combine(folderPath, fileName));
            }
            catch { }

            return false;
        }

        public static void ProcessInBatch<T>(IEnumerable<T> allRecords, int batchSize, Action<IEnumerable<T>> batchHandler)
        {
            if (!allRecords.IsCollectionValid()) return;
            if (batchSize == 0) batchSize = 100;

            var batchedRequests = new List<IEnumerable<T>>();

            for (var reqIndex = 0; reqIndex < allRecords.Count(); reqIndex += batchSize)
            {
                var batches = allRecords.Skip(reqIndex).Take(batchSize);
                batchedRequests.Add(batches);
            }

            foreach (var batch in batchedRequests)
            {
                batchHandler(batch);
            }
        }

        public static async Task ProcessInBatch<T>(IEnumerable<T> allRecords, int batchSize, Func<IEnumerable<T>, Task> batchHandler)
        {
            if (!allRecords.IsCollectionValid()) return;
            if (batchSize == 0) batchSize = 100;

            var batchedRequests = new List<IEnumerable<T>>();

            for (var reqIndex = 0; reqIndex < allRecords.Count(); reqIndex += batchSize)
            {
                var batches = allRecords.Skip(reqIndex).Take(batchSize);
                batchedRequests.Add(batches);
            }

            foreach (var batch in batchedRequests)
            {
                await batchHandler(batch);
            }
        }
    }
}
