using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Repository;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.Interfaces;
using GMAOAPI.Services.SeriLog;
using System.Linq.Expressions;
using System.Security.Claims;

namespace GMAOAPI.Services.implementation
{
    public class AuditService : IAuditService
    {
        private readonly IGenericRepository<Audit> _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            IGenericRepository<Audit> repository,
            IHttpContextAccessor httpContextAccessor


            )
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Audit>> GetAllAuditAsync(
            int? pageNumber = null,
            int? pageSize = null,
            string? entityId = null,
            string? actionType = null,
            string? entityName = null,
            string? userId = null,
            string? userName =null,
            DateTime? date = null
             )
        {


            ActionType parsedType = default;
            bool isEtatValid = string.IsNullOrEmpty(actionType)
                || Enum.TryParse(actionType, out parsedType);

            Expression<Func<Audit, bool>> filter = e =>
                (string.IsNullOrEmpty(entityId) || e.EntityId == entityId) &&
                (string.IsNullOrEmpty(entityName) || e.EntityName.Contains(entityName)) &&
                (string.IsNullOrEmpty(userId) || e.UtilisateurId.Contains(userId)) &&
                (string.IsNullOrEmpty(userName) || e.UserName.ToLower().Contains(userName.ToLower())) &&
                (!date.HasValue || e.Date.Date == date.Value.Date) &&
                (string.IsNullOrEmpty(actionType) || isEtatValid && e.type == parsedType);

            var audits = await _repository.FindAllAsync(
               filter,
               includeProperties: "",
               orderBy: q => q.OrderByDescending(e => e.Date),
               pageNumber: pageNumber,
               pageSize: pageSize
           );



            return audits.ToList();
        }



        public async Task CreateAuditAsync(string actionEffectuee, ActionType type, string entityName, string entityId)
        {
            var audit = new Audit
            {
                ActionEffectuee = actionEffectuee,
                type = type,
                EntityName = entityName,
                EntityId = entityId,
                Date = DateTime.Now
            };

            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                audit.UserName = context.User.FindFirst(ClaimTypes.GivenName)?.Value ?? "inconnue";
                audit.UtilisateurId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "inconnue";
                audit.IpAddress = context.Connection.RemoteIpAddress?.ToString();
                audit.BrowserInfo = context.Request.Headers["User-Agent"].ToString();
            }

            await _repository.CreateAsync(audit);
        }





        public async Task<int> CountAuditAsync(
      string? entityId = null,
      string? actionType = null,
      string? entityName = null,
      string? userId = null,
      string? userName = null,
      DateTime? date = null
  )
        {
            ActionType parsedType = default;
            bool isTypeValid = string.IsNullOrEmpty(actionType)
                || Enum.TryParse(actionType, out parsedType);

            Expression<Func<Audit, bool>> filter = e =>
                (string.IsNullOrEmpty(entityId) || e.EntityId == entityId) &&
                (string.IsNullOrEmpty(entityName) || e.EntityName.Contains(entityName)) &&
                (string.IsNullOrEmpty(userId) || e.UtilisateurId.Contains(userId)) &&
                (string.IsNullOrEmpty(userName) || e.UserName.Contains(userId)) &&
                (!date.HasValue || e.Date.Date == date.Value.Date) &&
                (string.IsNullOrEmpty(actionType) || isTypeValid && e.type == parsedType);

            return await _repository.CountAsync(filter);
        }





    }
}
