using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Services.Token;
using Mapster;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Repository;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IUtilisateurService _userService;
        private readonly ITokenService _tokenService;


        public AccountController(IUtilisateurService userService, ITokenService tokenService )
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var createdUser = await _userService.RegisterAsync(registerDto);
                var userDto = createdUser.Adapt<UtilisateurDto>();
                userDto.Token = _tokenService.CreateToken(createdUser);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de l'enregistrement : " + ex.Message);
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var userDto = await _userService.LoginAsync(loginDto, _tokenService);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return BadRequest("Échec de la connexion : " + ex.Message);
            }
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId ,[FromQuery] string token)
        {
            try
            {
                await _userService.ConfirmEmail(userId,token);
                return Ok("Email confirme");
            }
            catch (Exception ex)
            {
                return BadRequest( ex.Message);
            }
        }

        [HttpGet("GetUserById/{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var userDto = await _userService.GetUserDtoByIdAsync(id);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return NotFound("Utilisateur non trouvé : " + ex.Message);
            }
        }

        [HttpGet("list")]
        [Authorize(Roles = "Admin,Responsable")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? nom = null,
            [FromQuery] string? prenom = null,
            [FromQuery] string? username = null,
            [FromQuery] string? email = null,
            [FromQuery] string? role = null,
            [FromQuery] string? specialite = null,
                                [FromQuery] bool? isArchived = false)

        {
            try
            {
                var usersDto = await _userService.GetAllUserDtosAsync<Utilisateur>(
                    pageNumber, pageSize, nom, prenom, username, email, role, specialite,isArchived);
                return Ok(usersDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur lors de la récupération des utilisateurs : " + ex.Message);
            }
        }


        [HttpPut("Update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto updatedUserDto)
        {
            if (id != updatedUserDto.Id)
                return BadRequest("Identifiant utilisateur non correspondant.");
            try
            {
                var updatedUser = await _userService.UpdateUserAsync(updatedUserDto);
                return Ok(updatedUser.Adapt<UtilisateurDto>());
            }
            catch (Exception ex)
            {
                return BadRequest("Échec de la mise à jour : " + ex.Message);
            }
        }

        [HttpDelete("Delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur lors de la suppression de l'utilisateur : " + ex.Message);
            }
        }


        [HttpDelete("Unarchive/{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Unarchive(string id)
        {
            try
            {
                await _userService.UnarchiveUserAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la désarchivage de utilisateur. " + ex.Message);
            }
        }

        [HttpGet("Count")]
        public async Task<IActionResult> Count(
            [FromQuery] string? nom = "",
            [FromQuery] string? prenom = "",
            [FromQuery] string? username = "",
            [FromQuery] string? email = "",
            [FromQuery] string? role = "",
            [FromQuery] string? specialite = ""
        )
        {
            try
            {
                int count;
                if (!string.IsNullOrEmpty(role) && role.Equals("technicien", StringComparison.OrdinalIgnoreCase))
                {
                    count = await _userService.CountUsersAsync<Technicien>(
                        nom, prenom, username, email, role, specialite
                    );
                }
                else
                {
                    count = await _userService.CountUsersAsync<Utilisateur>(
                        nom, prenom, username, email, role, specialite
                    );
                }

                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("reinitialisation")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                await _userService.ForgotPassword(forgotPasswordDto.Email, requestHeader: Request.Headers.Origin);
                return Ok("Un email de réinitialisation a été envoyé.");
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la réinitialisation : " + ex.Message);
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                await _userService.ResetPassword(resetPasswordDto);
                return Ok("Mot de passe réinitialisé avec succès.");
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la réinitialisation du mot de passe : " + ex.Message);
            }
        }

        [HttpGet("technicien-disponible")]
        [Authorize(Roles = "Admin,Responsable")]

        public async Task<ActionResult<List<UtilisateurDto>>> GetAvailableTechniciens(
         [FromQuery] DateTime from,
         [FromQuery] DateTime? to = null)

        {
           

            var available = await _userService
                .GetAvailableTechniciensAsync(from, to);

            return Ok(available);
        }


    }
}
