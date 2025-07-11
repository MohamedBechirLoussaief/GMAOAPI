using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Entities;
using System.Security.Claims;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RapportsController : ControllerBase
    {
        private readonly IRapportService _rapportService;

        public RapportsController(IRapportService rapportService)
        {
            _rapportService = rapportService;
        }

        [HttpGet("List")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? id = null,
            [FromQuery] string? titre = null,
            [FromQuery] int? interventionId = null,
            [FromQuery] bool? isArchived = false,

            [FromQuery] DateTime? dateCreation = null,
            [FromQuery] string? createurId = null,
            [FromQuery] bool? isValid = null,
            [FromQuery] string? valideurId = null



            )
        {
            try
            {
                var dtos = await _rapportService.GetAllRapportDtosAsync(
                    pageNumber, pageSize, id, titre, interventionId, isArchived,dateCreation, createurId, isValid, valideurId);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération des rapports: " + ex.Message);
            }
        }

        [HttpGet("GetRapportById/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _rapportService.GetRapportDtoByIdAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération du rapport: " + ex.Message);
            }
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] RapportCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            try
            {
                var dto = await _rapportService.CreateRapportDtoAsync(createDto,userId);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la création du rapport: " + ex.Message);
            }
        }

        [HttpPut("Update/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Update(int id, [FromBody] RapportUpdateDto rapport)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _rapportService.UpdateRapportDtoAsync(id,rapport);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la mise à jour du rapport: " + ex.Message);
            }
        }

        [HttpDelete("Delete/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _rapportService.DeleteRapportAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la suppression du rapport: " + ex.Message);
            }
        }


        [HttpDelete("Unarchive/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                await _rapportService.UnarchiveRapportAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la désarchivage de la rapport. " + ex.Message);
            }
        }

        [HttpPut("Valider/{id:int}")]
        [Authorize (Roles ="Admin,Responsable")]
        public async Task<IActionResult> Valider(int id)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _rapportService.ValiderRapportAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la validation du rapport: " + ex.Message);
            }
        }

        [HttpGet("Count")]
        public async Task<IActionResult> Count(
            [FromQuery] int? id = null,
            [FromQuery] string? titre = null,
            [FromQuery] int? interventionId = null,
            [FromQuery] bool? isArchived = null,
       [FromQuery] DateTime? dateCreation = null,
       [FromQuery] string? createurId =null,
         [FromQuery] bool? isValid = null,
     [FromQuery]  string? valideurId = null

     )
        {
            try
            {
                int count = await _rapportService.CountAsync(
                    id, titre, interventionId, isArchived, dateCreation, createurId, isValid, valideurId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}/is-valid")]
        public async Task<ActionResult<bool>> IsRapportValid(int id)
        {
            try
            {
                var isValid = await _rapportService.IsRapportValidAsync(id);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                return NotFound( ex);
            }
        }
        [HttpPut("Invalider/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]
        public async Task<IActionResult> Invalider(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
               await _rapportService.InvaliderRapportAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de l'invalidation du rapport: " + ex.Message);
            }
        }


    }
}
