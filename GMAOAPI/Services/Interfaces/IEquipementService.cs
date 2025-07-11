using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.Services.Interfaces
{
    public interface IEquipementService
    {
        Task<List<EquipementDto>> GetAllEquipementDtosAsync(
            int? pageNumber = null,
            int? pageSize = null,
            int? id = null,
            string? nom = null,
            string? reference = null,
            string? localisation = null,
            DateTime? dateInstallation = null,
            string? type = null,
            string? etat = null,
            bool? isArchived = false);

        Task<EquipementDto> GetEquipementDtoByIdAsync(int id);

        Task<EquipementDto> CreateEquipementDtoAsync(EquipementCreateDto createDto);

        Task<EquipementDto> UpdateEquipementDtoAsync(int id, EquipementUpdateDto updateDto);

        Task DesactiverEquipementAsync(int id);

        Task ReactiverEquipementAsync(int id);

        Task DeleteEquipementAsync(int id);

        Task UnarchiveEquipementAsync(int id);

        Task<int> CountAsync(
            int? id = null,
            string? nom = null,
            string? reference = null,
            string? localisation = null,
            DateTime? dateInstallation = null,
            string? type = null,
            string? etat = null,
            bool? isArchived = false);

        Task<object> GetEquipmentStatsAsync();

        Task<object> GetEquipmentDetailsAsync(int equipmentId);

        Task ModifierEtatEquipementAsync(int id, EtatEquipement etat);
    }
}
