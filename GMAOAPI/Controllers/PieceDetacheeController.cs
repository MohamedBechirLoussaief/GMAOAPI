using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PiecesDetacheesController : ControllerBase
    {
        private readonly IPieceDetacheeService _pieceDetacheeService;

        public PiecesDetacheesController(IPieceDetacheeService pieceDetacheeService)
        {
            _pieceDetacheeService = pieceDetacheeService;
        }

        [HttpGet("List")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] int? id = null,
            [FromQuery] string? nom = null,
            [FromQuery] string? reference = null,
            [FromQuery] int? fournisseurId = null,
            [FromQuery] bool? isArchived = false
             )
        {
            try
            {
                var dtos = await _pieceDetacheeService.GetAllPieceDetacheeDtosAsync(pageNumber, pageSize, id,
                    nom, reference, fournisseurId ,isArchived);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération des pièces détachées. " + ex.Message);
            }
        }

        [HttpGet("GetPieceDetacheeById/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _pieceDetacheeService.GetPieceDetacheeDtoByIdAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return NotFound("Pièce détachée non trouvée: " + ex.Message);
            }
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Create([FromBody] PieceDetacheeCreateDto pieceDetacheeCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _pieceDetacheeService.CreatePieceDetacheeAsync(pieceDetacheeCreateDto);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la création: " + ex.Message);
            }
        }

        [HttpPut("Update/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Update(int id, [FromBody] PieceDetacheeUpdateDto piece)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _pieceDetacheeService.UpdatePieceDetacheeAsync(id, piece);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la mise à jour: " + ex.Message);
            }
        }

        [HttpDelete("Delete/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _pieceDetacheeService.DeletePieceDetacheeAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la pièce détachée: " + ex.Message);
            }
        }


        [HttpDelete("Unarchive/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                await _pieceDetacheeService.UnarchivePieceDetacheeAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la désarchivage de la piece detachee. " + ex.Message);
            }
        }


        [HttpGet("Count")]

        public async Task<IActionResult> Count(
            [FromQuery] int? id = null,
        [FromQuery] string? nom = "",
        [FromQuery] string? reference = "",
        [FromQuery] int? fournisseurId = null,
        [FromQuery] bool? isArchived = false
            )
        {
            try
            {
                int count = await _pieceDetacheeService.CountAsync(
                    id, nom, reference, fournisseurId, isArchived);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
