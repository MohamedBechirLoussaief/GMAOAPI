using System.Threading.Tasks;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;

namespace GMAOAPI.Services.Interfaces
{
    public interface IInterventionPieceDetacheeService
    {

        Task<InterventionPieceDetacheeDto> CreateDtoAsync(
            InterventionPieceDetacheeCreateDto createDto);

        Task<InterventionPieceDetacheeDto> UpdateDtoAsync(
            int interventionId,
            int pieceDetacheeId,
            InterventionPieceDetacheeUpdateDto updateDto);

        Task DeleteAsync(object[] ids);
    }
}
