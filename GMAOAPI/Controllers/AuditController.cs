using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GMAOAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles ="Admin,Responsable")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAudits(
            [FromQuery] int? pageNumber,
            [FromQuery] int? pageSize,
            [FromQuery] string? entityId,
            [FromQuery] string? actionType,
            [FromQuery] string? entityName,
            [FromQuery] string? userId,
                        [FromQuery] string? userName,

            [FromQuery] DateTime? date)
        {
            var audits = await _auditService.GetAllAuditAsync(pageNumber, pageSize, entityId, actionType, entityName, userId,userName, date);
            return Ok(audits);
        }

        [HttpGet("count")]
        public async Task<IActionResult> CountAudits(
            [FromQuery] string? entityId,
            [FromQuery] string? actionType,
            [FromQuery] string? entityName,
            [FromQuery] string? userId,
                                    [FromQuery] string? userName,

            [FromQuery] DateTime? date)
        {
            int count = await _auditService.CountAuditAsync(entityId, actionType, entityName, userId,userName, date);
            return Ok(count);
        }

    }
}
