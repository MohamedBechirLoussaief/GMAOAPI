using System.Collections.Generic;
using System.Threading.Tasks;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;

namespace GMAOAPI.Services.Interfaces
{
    public interface INotificationService
    {

        Task<List<NotificationDto>> GetAllNotificationDtosAsync(int pageNumber, int pageSize);

        Task<NotificationDto> GetNotificationDtoByIdAsync(int id);

        Task<NotificationDto> CreateNotificationDtoAsync(NotificationCreateDto createDto);
    }
}
