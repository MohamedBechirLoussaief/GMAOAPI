using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Entities;

namespace GMAOAPI.Services.Interfaces
{
    public interface IFournisseurService
    {
        Task<List<FournisseurDto>> GetAllFournisseurDtosAsync(
            int? pageNumber =null,
            int? pageSize = null,
            int? id = null,
            string? nom = null,
            string? adresse = null,
            string? contact = null,
            bool? isArchived = false);

        Task<FournisseurDto> GetFournisseurDtoByIdAsync(int id);

        Task<FournisseurDto> CreateFournisseurAsync(FournisseurCreateDto createDto);

        Task<FournisseurDto> UpdateFournisseurAsync(int id, FournisseurUpdateDto updateDto);

        Task DeleteFournisseurAsync(int id);

        Task UnarchiveFournisseurAsync(int id);

        Task<int> CountAsync(
            int? id = null,
            string? nom = null,
            string? adresse = null,
            string? contact = null,
            bool? isArchived = false);
    }
}
