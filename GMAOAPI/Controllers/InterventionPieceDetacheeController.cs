using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
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
    public class InterventionPieceDetacheesController : ControllerBase
    {
        private readonly IInterventionPieceDetacheeService _service;

        public InterventionPieceDetacheesController(IInterventionPieceDetacheeService service)
        {
            _service = service;
        }

        //[HttpGet("List")]
        //public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        //{
        //    try
        //    {
        //        var dtos = await _service.(pageNumber, pageSize);
        //        return Ok(dtos);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        return StatusCode(500, "Une erreur est survenue lors de la récupération des interventions pièces détachées: " + ex.Message);
        //    }
        //}

        //[HttpGet("GetInterventionPieceDetacheeById/{interventionId:int}/{pieceDetacheeId:int}")]
        //public async Task<IActionResult> GetById(int interventionId, int pieceDetacheeId)
        //{
        //    try
        //    {
        //        var dto = await _service.GetDtoByIdAsync(new object[] { interventionId, pieceDetacheeId });
        //        return Ok(dto);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        return StatusCode(500, "Une erreur est survenue lors de la récupération de l'intervention pièce détachée: " + ex.Message);
        //    }
        //}

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] InterventionPieceDetacheeCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _service.CreateDtoAsync(createDto);
                return Ok(dto);
            }
            catch (System.Exception ex)
            {
                return BadRequest("Erreur lors de la création: " + ex.Message);
            }
        }

        [HttpPut("Update/{interventionId:int}/{pieceDetacheeId:int}")]
        public async Task<IActionResult> Update(int interventionId, int pieceDetacheeId, [FromBody] InterventionPieceDetacheeUpdateDto interventionPieceDetachee)
        {
           

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _service.UpdateDtoAsync( interventionId,  pieceDetacheeId, interventionPieceDetachee);
                return Ok(dto);
            }
            catch (System.Exception ex)
            {
                return BadRequest("Erreur lors de la mise à jour: " + ex.Message);
            }
        }

        [HttpDelete("Delete/{interventionId:int}/{pieceDetacheeId:int}")]
        public async Task<IActionResult> Delete(int interventionId, int pieceDetacheeId)
        {
            try
            {
                await _service.DeleteAsync(new object[] { interventionId, pieceDetacheeId });
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'intervention pièce détachée: " + ex.Message);
            }
        }

        //[HttpGet("ListPieceDetachee/{interventionId}")]
        //public async Task<IActionResult> GetPiecesDeatcheeByIntervention(int interventionId)
        //{
        //    try
        //    {
        //        var dtos = await _service.GetAllPiecesDetacheeByInterventionIdDtosAsync(interventionId);
        //        return Ok(dtos);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        return StatusCode(500, "Une erreur est survenue lors de la récupération des interventions pièces détachées by interventionId: " + ex.Message);
        //    }
        //}
        
    }
}
