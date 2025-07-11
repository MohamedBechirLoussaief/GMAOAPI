using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Models.Entities;

namespace GMAOAPI.Services.Interfaces
{
    public interface IPlanificationService
    {
        Task<List<PlanificationDto>> GetAllPlanificationDtosListAsync(
            int pageNumber,
            int pageSize,
            int? id = null,
            string? description = null,
            string? statut = null,
            string? type = null,
            DateTime? dateDebut = null,
            bool? semaine = false,
            int? mois = null,
            string? frequence = null,
            int? interventionId = null,
            int? equipementId = null,
            bool? isArchived = false);
        Task<List<PlanificationDto>> GetAllPlanificationDtosAsync(
            DateTime startDate,
            DateTime endDate,
            int? id = null,
            string? description = null,
            string? statut = null,
            string? type = null,
            string? frequence = null,
            int? interventionId = null,
            int? equipementId = null,
            bool? isArchived = false
        );


        Task<PlanificationDto> GetPlanificationDtoByIdAsync(int id);

        Task<PlanificationDto> CreatePlanificationWithInterventionAsync(
            PlanificationCreateDto createDto,
            string userId);

        DateTime CalculateNextGeneration(
            FrequencePlanification frequence,
            DateTime from);

        Task<Planification?> UpdatePlanificationAsync(
            int id,
            PlanificationUpdateDto planification);

        Task<bool> DeletePlanificationAsync(int id);

        Task<bool> UnarchivePlanificationAsync(int id);

        Task<PlanificationDto?> GetPlanificationDtoByInterventionIdAsync(int interventionId);

        Task<int> CountAsync(
            int? id = null,
            DateTime? dateDebut = null,
            int? jour = null,
            bool? semaine = false,
            int? mois = null,
            string? frequence = null,
            int? interventionId = null,
            int? equipementId = null,
            bool? isArchived = false);
    }
}
