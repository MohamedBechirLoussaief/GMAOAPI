using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.Services.Interfaces
{
    public interface IInterventionService
    {
        Task<List<InterventionDto>> GetAllInterventionDtosAsync(
            int pageNumber,
            int pageSize,
            int? id = null,
            string? description = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null,
            string? statut = null,
            string? type = null,
            int? equipementId = null,
            string? technicienId = null,
            bool? isArchived = false);

        Task<InterventionDto> GetInterventionDtoByIdAsync(int id);

        Task<InterventionDto> CreateInterventionDtoAsync(
            InterventionCreateDto createDto,
            string userId);

        Task<InterventionDto> UpdateInterventionDtoAsync(
            int id,
            InterventionUpdateDto updateDto);

        Task DeleteInterventionAsync(int id);

        Task UnarchiveInterventionAsync(int id);

        Task<int> CountAsync(
            int? id = null,
            string? description = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null,
            string? statut = null,
            string? type = null,
            int? equipementId = null,
            string? technicienId = null,
            bool? isArchived = false);

        Task<object> GetInterventionStatsAsync();
        Task DemarrerInterventionAsync(int interventionId);
        Task MettreEnAttenteInterventionAsync(int interventionId);
        Task TerminerInterventionAsync(int interventionId);
        Task AnnulerInterventionAsync(int interventionId);
        Task<List<object>> GetChartDataAsync();

    }
}
