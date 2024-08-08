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
﻿using Interneuron.Terminology.Infrastructure.Domain.DSLs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interneuron.Terminology.Infrastructure.Domain
{
    public interface IFormularyRepository<TEntity> : IRepository<TEntity> where TEntity : EntityBase
    {
        //Task<IEnumerable<TEntity>> SearchFormularyByFilterCriteria(FormularyFilterCriteria filterCriteria);
        Task<IEnumerable<TEntity>> SearchFormularyBySearchTerm(string searchTerm);

        Task<IEnumerable<TEntity>> GetFormularyDescendentForCodes(string[] codes, bool onlyNonDeleted = true);

        Task<IEnumerable<TEntity>> GetFormularyImmediateDescendentForCodes(string[] codes, bool onlyNonDeleted = true);

        Task<IEnumerable<TEntity>> GetFormularyAncestorForCodes(string[] codes, bool onlyNonDeleted = true);

        IQueryable<TEntity> GetFormularyDetail(string id);

        IQueryable<TEntity> GetFormularyListForIds(List<string> ids, bool onlyNonDeleted = false);
        IQueryable<TEntity> GetFormularyListForFormularyIds(List<string> formularyIds, bool onlyNonDeleted = false);
        IQueryable<TEntity> GetFormularyBasicDetailListForIds(List<string> ids, bool onlyNonDeleted = false);

        IQueryable<TEntity> GetLatestFormulariesByCodes(string[] codes, bool onlyNonDeleted = false);

        //Task<T> SaveFormularyItem<T>(T formularyData) where T: TEntity;

        IEnumerable<TEntity> GetLatestFormulariesBriefInfoByNameOrCode(string productNameOrCode, string productType = null, bool exactMatch = false);

        Task<IEnumerable<TEntity>> GetLatestTopLevelNodesWithBasicResults();

        Task<IEnumerable<TEntity>> GetLatestAMPNodesWithBasicResultsForAttributes(string searchTerm, List<string> recordStatusCodes, List<string> formularyStatusCodes, List<string> flags);

        Task<IEnumerable<TEntity>> GetLatestProductTypeSpecificNodesWithBasicResultsForAttributes(string searchTerm, List<string> recordStatusCodes, List<string> formularyStatusCodes, List<string> flags, string productType);

        Task<TEntity> GetHistoryOfFormularies(int pageNo = 0, int pageSize = 10, List<KeyValuePair<string, string>> filters = null, bool getTotalRecords = false);

        Task<IEnumerable<TEntity>> GetFormularyLocalLicensedUse(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyLocalUnlicensedUse(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyLocalLicensedRoute(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyLocalUnlicensedRoute(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyCustomWarning(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyReminder(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyEndorsement(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyMedusaPreparationInstruction(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyTitrationType(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyRoundingFactor(List<string> formularyVersionIds);
        Task<IEnumerable<TEntity>> GetFormularyCompatibleDiluent(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyClinicalTrialMedication(List<string> formularyVersionIds);
        Task<IEnumerable<TEntity>> GetFormularyGastroResistant(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyCriticalDrug(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyModifiedRelease(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyExpensiveMedication(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyHighAlertMedication(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyIVToOral(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyNotForPRN(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyBloodProduct(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyDiluent(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyPrescribable(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyOutpatientMedication(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyIgnoreDuplicateWarning(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyControlledDrug(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyPrescriptionPrintingRequired(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyIndicationMandatory(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyWitnessingRequired(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyStatus(List<string> formularyVersionIds);

        //Task<IEnumerable<TEntity>> GetFormularyDetailByCodes(string[] codes);

        Task<IEnumerable<TEntity>> GetFormularyUsageStatsForSearchTerm(string searchTerm, Dictionary<string, object> filterMeta);

        //Task<IEnumerable<TEntity>> GetFormularyUsageStatsForCodes(List<string> codes);
        Task<IEnumerable<TEntity>> GetFormularyUsageStatsForCodes(List<string> codes, Dictionary<string, object> filterMeta);

        Task<bool> GetHeaderRecordsLock(List<string> formularyVersionIds);

        Task<bool> ReleaseHeaderRecordsLock(List<string> formularyVersionIds);

        Task<IEnumerable<TEntity>> GetFormularyChangeFromLog(List<string> codes, bool isDmdInvalid = false, bool isDmdDeleted = false, bool hasDetailChanges = false, bool hasPosologyChanges = false, bool hasGuidanceChanges = false, bool hasFlagsChanges = false);

        IEnumerable<TEntity> GetFormularyImmediateDescendentForFormularyIds(List<string> formularyIds, bool onlyNonDeleted = true);
        Dictionary<string, string> GetFormularyAncestorsForFormularyIdsAsLookup(List<string> formularyIds, bool onlyActiveRecs = false);
        Dictionary<string, string> GetFormularyAncestorRootForFormularyIdsAsLookup(List<string> formularyIds, bool onlyActiveRecs = false);

        Dictionary<string, List<string>> GetDescendentFormularyIdsForFormularyIdsAsLookup(List<string> formularyIds, bool onlyActiveRecs = false);
        Dictionary<string, List<string>> GetDescendentFormularyIdsForFormularyIdsAsFlattenedLookup(List<string> formularyIds, bool onlyActiveRecs = false);
    }

}
