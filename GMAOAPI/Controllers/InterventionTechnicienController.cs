using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InterventionTechniciensController : ControllerBase
    {
        private readonly IInterventionTechnicienService _service;

        public InterventionTechniciensController(IInterventionTechnicienService service)
        {
            _service = service;
        }

        //[HttpGet("List")]
        //public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        //{
        //    try
        //    {
        //        var dtos = await _service.GetAllTechnicienDtosAsync(pageNumber, pageSize);
        //        return Ok(dtos);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Une erreur est survenue lors de la récupération des interventions-techniciens: " + ex.Message);
        //    }
        //}

        //[HttpGet("GetInterventionTechnicienById/{interventionId:int}/{technicienId:int}")]
        //public async Task<IActionResult> GetById(int interventionId, int technicienId)
        //{
        //    try
        //    {
        //        var dto = await _service.GetTechnicienDtoByIdAsync(new object[] { technicienId, interventionId });
        //        return Ok(dto);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Une erreur est survenue lors de la récupération de l'intervention-technicien: " + ex.Message);
        //    }
        //}

        [HttpPost("Create")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Create([FromBody] InterventionTechnicienCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _service.CreateTechnicienDtoAsync(createDto);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la création: " + ex.Message);
            }
        }

        //[HttpPut("Update/{interventionId:int}/{technicienId:int}")]
        //public async Task<IActionResult> Update(int interventionId, int technicienId, [FromBody] InterventionTechnicien interventionTechnicien)
        //{
        //    // Optionally, you can validate that the composite keys match.
        //    if (interventionTechnicien == null || interventionTechnicien.InterventionId != interventionId)
        //        return BadRequest("L'ID de l'intervention ne correspond pas.");

        //    try
        //    {
        //        var dto = await _service.UpdateTechnicienDtoAsync(interventionTechnicien);
        //        return Ok(dto);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest("Erreur lors de la mise à jour: " + ex.Message);
        //    }
        //}

        [HttpDelete("Delete/{interventionId}/{technicienId}")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<IActionResult> Delete(int interventionId, string technicienId)
        {
            try
            {
                await _service.DeleteTechnicienAsync(new object[] { technicienId, interventionId });
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'intervention-technicien: " + ex.Message);
            }
        }

        //[HttpGet("ListTechniciens/{interventionId:int}")]
        //public async Task<IActionResult> GetAllTechniciensByIdItervention(int interventionId)
        //{
        //    try
        //    {
        //        var dtos = await _service.GetAllTechnicienByInterventionIdDtosAsync(interventionId);
        //        return Ok(dtos);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Une erreur est survenue lors de la récupération les techniciens de cette intervention: " + ex.Message);
        //    }
        //}
    }
}
