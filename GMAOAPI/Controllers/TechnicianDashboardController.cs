using System.Collections.Generic;
using System.Threading.Tasks;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Services.implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Technicien")]

    public class TechnicienDashboardController : ControllerBase
    {
        private readonly TechnicienDashboardService _dashboardService;

        public TechnicienDashboardController(TechnicienDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }



        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats([FromQuery] string technicienId)
        {
            var stats = await _dashboardService.GetStatsAsync(technicienId);
            return Ok(stats);
        }


    }
}
