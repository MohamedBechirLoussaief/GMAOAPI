using System.Threading.Tasks;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;

namespace GMAOAPI.Services.Interfaces
{
    public interface IInterventionTechnicienService
    {

        Task<InterventionTechnicienDto> CreateTechnicienDtoAsync(
            InterventionTechnicienCreateDto createDto);

        Task DeleteTechnicienAsync(object[] ids);
    }
}
