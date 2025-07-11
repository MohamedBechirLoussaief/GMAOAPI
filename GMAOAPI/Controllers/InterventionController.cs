using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;
using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InterventionController : ControllerBase
    {
        private readonly IInterventionService _interventionService;

        public InterventionController(IInterventionService interventionService)
        {
            _interventionService = interventionService;
        }

        [HttpGet("List")]
        public async Task<IActionResult> GetAll(
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] int? id = null,
     [FromQuery] string? description = null,
     [FromQuery] DateTime? dateDebut = null,
     [FromQuery] DateTime? dateFin = null,
     [FromQuery] string? statut = null,
     [FromQuery] string? type = null,
     [FromQuery] int? equipementId = null,
          [FromQuery] string? technicienId = null,

     [FromQuery] bool? isArchived =false)
        {
            try
            {
                var dtos = await _interventionService.GetAllInterventionDtosAsync(
                    pageNumber, pageSize,id, description, dateDebut, dateFin, statut, type, equipementId,technicienId,isArchived);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération des interventions: " + ex.Message);
            }
        }


        [HttpGet("GetInterventionById/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _interventionService.GetInterventionDtoByIdAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'intervention: " + ex.Message);
            }
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin,Responsable")]
        public async Task<IActionResult> Create([FromBody] InterventionCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token.");

                var dto = await _interventionService.CreateInterventionDtoAsync(createDto, userId);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur de creation de l'intervention: " + ex.Message);
            }
        }

        [HttpPut("Update/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] InterventionUpdateDto intervention)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _interventionService.UpdateInterventionDtoAsync(id , intervention);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur de mise a jour de l'intervention: " + ex.Message);
            }
        }

        [HttpDelete("Delete/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _interventionService.DeleteInterventionAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'intervention: " + ex.Message);
            }
        }

        [HttpDelete("Unarchive/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                await _interventionService.UnarchiveInterventionAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la désarchivage de l'intervention. " + ex.Message);
            }
        }


        [HttpGet("Count")]

        public async Task<IActionResult> Count(
            [FromQuery] int? id = null,
        [FromQuery] string? description = "",
        [FromQuery] DateTime? dateDebut = null,
        [FromQuery] DateTime? dateFin = null,
        [FromQuery] string? statut = "",
        [FromQuery] string? type = "",
        [FromQuery] int? equipementId = null,
        [FromQuery] string? technicienId = null,
                [FromQuery] bool? isArchived = false

        )
        {
            try
            {
                int count = await _interventionService.CountAsync(
                    id, description, dateDebut, dateFin, statut, type, equipementId, technicienId, isArchived);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("Stat")]
        public async Task<IActionResult> GetStat()
        {
            try
            {
                var stat = await _interventionService.GetInterventionStatsAsync();
                return Ok(stat);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id}/demarrer")]
        public async Task<IActionResult> DemarrerIntervention(int id)
        {
            try
            {
                await _interventionService.DemarrerInterventionAsync(id);
                return NoContent(); // 204
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                "Une erreur est survenue lors du démarrage de l'intervention. " + ex.Message);
            }
        }

        [HttpPost("{id}/mettre-en-attente")]
        public async Task<IActionResult> MettreEnAttenteIntervention(int id)
        {
            try
            {
                await _interventionService.MettreEnAttenteInterventionAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                "Une erreur est survenue lors de la mise en attente de l'intervention. " + ex.Message);
            }
        }

        [HttpPost("{id}/terminer")]
        public async Task<IActionResult> TerminerIntervention(int id)
        {
            try
            {
                await _interventionService.TerminerInterventionAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                 "Une erreur est survenue lors de la clôture de l'intervention. " + ex.Message);
            }
        }


        [HttpPost("{id}/annuler")]
        public async Task<IActionResult> AnnulerIntervention(int id)
        {
            try
            {
                await _interventionService.AnnulerInterventionAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500,
               "Une erreur est survenue lors de l’annulation de l'intervention. " + ex.Message);
            }
        }
        [HttpGet("ChartData")]
        [Authorize(Roles = "Admin,Responsable")] // tu peux adapter selon le rôle
        public async Task<IActionResult> GetChartData()
        {
            try
            {
                var data = await _interventionService.GetChartDataAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur lors de la génération des données du graphique : " + ex.Message);
            }
        }



    }
}
