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
    public class FournisseursController : ControllerBase
    {
        private readonly IFournisseurService _fournisseurService;

        public FournisseursController(IFournisseurService fournisseurService)
        {
            _fournisseurService = fournisseurService;
        }
        [HttpGet("List")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> GetAll(
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] int? id = null,
            [FromQuery] string? nom = null,
            [FromQuery] string? adresse = null,
            [FromQuery] string? contact = null,
            [FromQuery] bool? isArchived = false
            )
        {
            try
            {
                var dtos = await _fournisseurService.GetAllFournisseurDtosAsync(pageNumber, pageSize, id, nom, adresse, contact, isArchived);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération des fournisseurs. " + ex.Message);
            }
        }


        [HttpGet("GetFournisseurById/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _fournisseurService.GetFournisseurDtoByIdAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return NotFound("Fournisseur non trouvé. " + ex.Message);
            }
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Create([FromBody] FournisseurCreateDto fournisseurCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _fournisseurService.CreateFournisseurAsync(fournisseurCreateDto);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Update/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Update(int id, [FromBody] FournisseurUpdateDto fournisseur)
        {


            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _fournisseurService.UpdateFournisseurAsync(id, fournisseur);
                return Ok(dto);
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
                await _fournisseurService.DeleteFournisseurAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la suppression du fournisseur. " + ex.Message);
            }
        }
        [HttpDelete("Unarchive/{id:int}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                await _fournisseurService.UnarchiveFournisseurAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la désarchivage de fournisseur. " + ex.Message);
            }
        }


        [HttpGet("Count")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Count(
            [FromQuery] int? id = null,
      [FromQuery] string? nom = "",
      [FromQuery] string? adresse = "",
      [FromQuery] string? contact = "",
        [FromQuery] bool? isArchived = false
  )
        {
            try
            {
                int count = await _fournisseurService.CountAsync(id,nom, adresse, contact,isArchived);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
