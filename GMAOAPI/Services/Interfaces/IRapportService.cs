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
    public interface IRapportService
    {
        Task<List<RapportDto>> GetAllRapportDtosAsync(
            int pageNumber,
            int pageSize,
            int? id = null,
            string? titre = null,
            int? interventionId = null,
            bool? isArchived = false,
            DateTime? dateCreation = null,
            string? createurId = null,
            bool? isValid = null,
            string? valideurId = null
        );

        Task<RapportDto> GetRapportDtoByIdAsync(int id);

        Task<RapportDto> CreateRapportDtoAsync(RapportCreateDto createDto, string userId);

        Task<RapportDto> UpdateRapportDtoAsync(int id, RapportUpdateDto updateDto);

        Task<RapportDto> ValiderRapportAsync(int id);

        Task DeleteRapportAsync(int id);

        Task UnarchiveRapportAsync(int id);

        Task<int> CountAsync(
            int? id = null,
            string? titre = null,
            int? interventionId = null,
            bool? isArchived = false,
            DateTime? dateCreation = null,
            string? createurId = null,
            bool? isValid = null,
            string? valideurId = null
        );

        Task<bool> IsRapportValidAsync(int rapportId);
        Task InvaliderRapportAsync(int id);
    }
}
