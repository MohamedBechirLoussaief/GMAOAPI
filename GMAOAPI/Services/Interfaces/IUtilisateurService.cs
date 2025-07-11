using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Services.Token;

namespace GMAOAPI.Services.Interfaces
{
    public interface IUtilisateurService
    {
        Task<Utilisateur> RegisterAsync(RegisterDto registerDto);
        Task ConfirmEmail(string userId, string token);
        Task<UtilisateurDto> LoginAsync(LoginDto loginDto, ITokenService tokenService);
        Task<Utilisateur> UpdateUserAsync(UpdateUserDto updateDto);
        Task DeleteUserAsync(string id);
        Task UnarchiveUserAsync(string id);
        Task<UtilisateurDto> GetUserDtoByIdAsync(string id);
        Task<List<UtilisateurDto>> GetAllUserDtosAsync<T>(
            int? pageNumber = null,
            int? pageSize = null,
            string? nom = null,
            string? prenom = null,
            string? username = null,
            string? email = null,
            string? role = null,
            string? specialite = null,
            bool? isArchived = false) where T : Utilisateur;
        Task<UtilisateurDto> GetUserDtoByIdAsync<T>(string id) where T : Utilisateur;
        Task<int> CountUsersAsync<T>(
            string? nom = null,
            string? prenom = null,
            string? username = null,
            string? email = null,
            string? role = null,
            string? specialite = null,
            bool? isArchived = false) where T : Utilisateur;
        Task ForgotPassword(string email, string requestHeader);
        Task ResetPassword(ResetPasswordDto resetPasswordDto);
        Task<List<UtilisateurDto>> GetAvailableTechniciensAsync(
            DateTime from,
            DateTime? to = null
        );
    }
}
