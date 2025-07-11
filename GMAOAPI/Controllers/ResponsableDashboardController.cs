using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GMAOAPI.Services.implementation;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles ="Responsable")]

    public class ResponsableDashboardController : ControllerBase
    {
        private readonly ResponsableDashboardService _dashboardService;

        public ResponsableDashboardController(ResponsableDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetStatsAsync();
            return Ok(stats);
        }
    

        [HttpGet("pieces/stock-vide")]
        public async Task<IActionResult> GetPiecesStockVide()
        {
            var pieces = await _dashboardService.GetPiecesAvecStockVideAsync();
            return Ok(pieces);
        }

        
    }
}
