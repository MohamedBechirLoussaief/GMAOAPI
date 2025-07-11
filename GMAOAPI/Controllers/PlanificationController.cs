using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.DTOs.UpdateDTOs;
using Mapster;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlanificationsController : ControllerBase
    {
        private readonly IPlanificationService _planificationService;

        public PlanificationsController(IPlanificationService planificationService)
        {
            _planificationService = planificationService;
        }

        [HttpGet("List2")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? id = null,
            [FromQuery] string? description = null,
                 [FromQuery] string? statut = null,
                      [FromQuery] string? type = null,

            [FromQuery] DateTime? dateDebut = null,
       [FromQuery] bool? semaine = false,
        [FromQuery] int? mois = null,
            [FromQuery] string? frequence = null,
            [FromQuery] int? interventionId = null,
            [FromQuery] int? equipementId = null,

            [FromQuery] bool? isArchived = false
            )
        {
            try
            {
                var dtos = await _planificationService.GetAllPlanificationDtosListAsync(
                    pageNumber, pageSize, id, description, statut, type, dateDebut, semaine, mois, frequence, interventionId, equipementId, isArchived);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération des planifications: " + ex.Message);
            }
        }
        [HttpGet("List")]
        public async Task<IActionResult> GetAll(
     [FromQuery] DateTime startDate,
     [FromQuery] DateTime endDate,
     [FromQuery] int? id = null,
     [FromQuery] string? description = null,
     [FromQuery] string? statut = null,
     [FromQuery] string? type = null,
     [FromQuery] string? frequence = null,
     [FromQuery] int? interventionId = null,
     [FromQuery] int? equipementId = null,
     [FromQuery] bool? isArchived = false
 )
        {
            try
            {
                var dtos = await _planificationService.GetAllPlanificationDtosAsync(
                    startDate,
                    endDate,
                    id,
                    description,
                    statut,
                    type,
                    frequence,
                    interventionId,
                    equipementId,
                    isArchived
                );
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    "Une erreur est survenue lors de la récupération des planifications : "
                    + ex.Message
                );
            }
        }

        [HttpGet("GetPlanificationById/{id:int}", Name = "GetPlanificationById")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _planificationService.GetPlanificationDtoByIdAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la planification: " + ex.Message);
            }
        }

        [HttpGet("GetPlanificationByInterventionId/{id:int}")]
        public async Task<IActionResult> GetByInterventionId(int id)
        {
            try
            {
                var dto = await _planificationService.GetPlanificationDtoByInterventionIdAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la planification: " + ex.Message);
            }
        }


        [HttpPost("Create")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Create([FromBody] PlanificationCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token.");

                var dto = await _planificationService.CreatePlanificationWithInterventionAsync(createDto, userId);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la création de la planification: " + ex.Message);
            }
        }

        [HttpPut("Update/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Update(int id, [FromBody] PlanificationUpdateDto planification)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _planificationService.UpdatePlanificationAsync(id ,planification);
                          
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la mise à jour de la planification: " + ex.Message);
            }
        }

        [HttpDelete("Delete/{id:int}")]

        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Delete(int id)
        {
            try
            {

                await _planificationService.DeletePlanificationAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la planification: " + ex.Message);
            }
        }


        [HttpDelete("Unarchive/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                await _planificationService.UnarchivePlanificationAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la désarchivage de la planification. " + ex.Message);
            }

        }
        [HttpGet("Count")]
        public async Task<IActionResult> Count(
            [FromQuery] int? id = null,
       [FromQuery] DateTime? dateDebut = null,
     [FromQuery] int? jour = null,
   [FromQuery] bool? semaine = false,
    [FromQuery] int? mois = null,
       [FromQuery] string? frequence = "",
       [FromQuery] int? interventionId = null,
                [FromQuery] int? equipementId = null,
                [FromQuery] bool? isArchived = false)
        {
            try
            {
                int count = await _planificationService.CountAsync(
                   id, dateDebut, jour, semaine, mois, frequence, interventionId, equipementId, isArchived);   

                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
