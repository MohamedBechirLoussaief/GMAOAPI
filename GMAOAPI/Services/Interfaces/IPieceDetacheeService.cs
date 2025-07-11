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
    public interface IPieceDetacheeService
    {
        Task<List<PieceDetacheeDto>> GetAllPieceDetacheeDtosAsync(
            int? pageNumber = null,
            int? pageSize = null,
            int? id = null,
            string? nom = null,
            string? reference = null,
            int? fournisseurId = null,
            bool? isArchived = false);

        Task<PieceDetacheeDto> GetPieceDetacheeDtoByIdAsync(int id);

        Task<PieceDetacheeDto> CreatePieceDetacheeAsync(PieceDetacheeCreateDto createDto);

        Task<PieceDetacheeDto> UpdatePieceDetacheeAsync(int id, PieceDetacheeUpdateDto updateDto);

        Task DeletePieceDetacheeAsync(int id);

        Task UnarchivePieceDetacheeAsync(int id);

        Task<int> CountAsync(
            int? id = null,
            string? nom = null,
            string? reference = null,
            int? fournisseurId = null,
            bool? isArchived = false);
    }
}
