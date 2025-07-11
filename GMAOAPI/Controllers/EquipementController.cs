using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.SeriLog;
using Mapster;
using GMAOAPI.Models.Entities;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EquipementsController : ControllerBase
    {
        private readonly IEquipementService _equipementService;

        public EquipementsController(IEquipementService equipementService)
        {
            _equipementService = equipementService;
        }

        [HttpGet("List")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> GetAll(
          [FromQuery] int? pageNumber = null,
          [FromQuery] int? pageSize = null,
                 [FromQuery] int? id = null,

          [FromQuery] string? nom = null,
          [FromQuery] string? reference = null,
          [FromQuery] string? localisation = null,
          [FromQuery] DateTime? dateInstallation = null,
          [FromQuery] string? type = null,
          [FromQuery] string? etat = null,
                    [FromQuery] bool? isArchived = false)

        {
            try
            {
                var dtos = await _equipementService.GetAllEquipementDtosAsync(
                    pageNumber,
                    pageSize,
                    id,
                    nom,
                    reference,
                    localisation,
                    dateInstallation,
                    type,
                    etat,
                    isArchived);

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération des équipements. " + ex.Message);
            }
        }

        [HttpGet("Count")]
        public async Task<IActionResult> Count(

            [FromQuery] int? id = null,
            [FromQuery] string? nom = null,
          [FromQuery] string? reference = null,
          [FromQuery] string? localisation = null,
          [FromQuery] DateTime? dateInstallation = null,
          [FromQuery] string? type = null,
          [FromQuery] string? etat = null,
            [FromQuery] bool? isArchived = false
)        {
            try
            {
                int count = await _equipementService.CountAsync(

                    id,
                    nom,
                    reference,
                    localisation,
                    dateInstallation,
                    type,
                    etat,
                    isArchived);
                return Ok(count);
            }catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetEquipementById/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _equipementService.GetEquipementDtoByIdAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return NotFound("Équipement non trouvé. " + ex.Message);
            }
        }

        [HttpGet("Stat")]
        public async Task<IActionResult> GetStat()
        {
            try
            {
                var stat = await _equipementService.GetEquipmentStatsAsync();
                return Ok(stat);
            }
            catch (Exception ex)
            {
                return NotFound( ex.Message);
            }
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Create([FromBody] EquipementCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var dto = await _equipementService.CreateEquipementDtoAsync(createDto);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Update/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Update(int id, [FromBody] EquipementUpdateDto equipement)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var dto = await _equipementService.UpdateEquipementDtoAsync( id , equipement);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult> Deactivate(int id)
        {
            try
            {
               await _equipementService.DesactiverEquipementAsync(id);
                return NoContent();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("{id}/reactivate")]
        public async Task<ActionResult> Reactivate(int id)
        {
            try
            {
                await _equipementService.ReactiverEquipementAsync(id);
                return NoContent();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("Delete/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _equipementService.DeleteEquipementAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'équipement. " + ex.Message);
            }
        }

        [HttpDelete("Unarchive/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                await _equipementService.UnarchiveEquipementAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la désarchivage de l'équipement. " + ex.Message);
            }
        }

        [HttpGet("{equipmentId}/details")]

        public async Task<IActionResult> GetEquipmentDetails(int equipmentId)
        {
            try
            {
                var result = await _equipementService.GetEquipmentDetailsAsync(equipmentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
   