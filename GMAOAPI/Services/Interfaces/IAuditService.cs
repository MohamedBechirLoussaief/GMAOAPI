using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.Services.Interfaces
{
    public interface IAuditService
    {
 
        Task<List<Audit>> GetAllAuditAsync(
            int? pageNumber = null,
            int? pageSize = null,
            string? entityId = null,
            string? actionType = null,
            string? entityName = null,
            string? userId = null,
            string? userName = null,
            DateTime? date = null
        );

        Task CreateAuditAsync(
            string actionEffectuee,
            ActionType type,
            string entityName,
            string entityId
        );

        Task<int> CountAuditAsync(
            string? entityId = null,
            string? actionType = null,
            string? entityName = null,
            string? userId = null,
            string? userName = null,
            DateTime? date = null
        );
    }
}
