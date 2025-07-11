//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using GMAOAPI.DTOs.ReadDTOs;
//using GMAOAPI.DTOs.UpdateDTOs;
//using GMAOAPI.Services;
//using Mapster;
//using GMAOAPI.DTOs.AuthDTO;
//using GMAOAPI.Models.Entities;

//namespace GMAOAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize(Roles = "Admin")]
//    public class ResponsableController : ControllerBase
//    {
//        private readonly UtilisateurService _userService;

//        public ResponsableController(UtilisateurService userService)
//        {
//            _userService = userService;
//        }

//        [HttpGet("list")]
//        public async Task<IActionResult> GetAllResponsables([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
//        {
//            try
//            {
//                var responsablesDto = await _userService.GetAllUserDtosAsync<Responsable>(pageNumber, pageSize);
//                return Ok(responsablesDto);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, "Erreur lors de la récupération des responsables : " + ex.Message);
//            }
//        }

//        [HttpGet("GetResponsableById/{id}")]
//        public async Task<IActionResult> GetResponsableById(string id)
//        {
//            try
//            {
//                var responsableDto = await _userService.GetUserDtoByIdAsync<Responsable>(id);
//                return Ok(responsableDto);
//            }
//            catch (Exception ex)
//            {
//                return NotFound("Responsable non trouvé : " + ex.Message);
//            }
//        }

//        [HttpPut("Update/{id}")]
//        [Authorize(Roles = "Admin,Responsable")]
//        public async Task<IActionResult> UpdateResponsable(string id, [FromBody] UpdateUserDto updatedResp)
//        {
//            if (id != updatedResp.Id)
//                return BadRequest("Identifiant utilisateur non correspondant.");
//            try
//            {
//                var updatedUser = await _userService.UpdateUserAsync(updatedResp);
//                return Ok(updatedUser.Adapt<UtilisateurDto>());
//            }
//            catch (Exception ex)
//            {
//                return BadRequest("Échec de la mise à jour : " + ex.Message);
//            }
//        }
//    }
//}
