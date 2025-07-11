using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("List")]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var dtos = await _notificationService.GetAllNotificationDtosAsync(pageNumber, pageSize);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération des notifications: " + ex.Message);
            }
        }

        [HttpGet("GetNotificationById/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _notificationService.GetNotificationDtoByIdAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la notification: " + ex.Message);
            }
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] NotificationCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = await _notificationService.CreateNotificationDtoAsync(createDto);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur lors de la création du rapport: " + ex.Message);
            }
        }

        //[HttpPut("Update/{id:int}")]
        //public async Task<IActionResult> Update(int id, [FromBody] Notification notification)
        //{
        //    if (id != notification.Id)
        //        return BadRequest("L'ID de la notification ne correspond pas.");
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    try
        //    {
        //        var dto = await _notificationService.UpdateNotificationDtoAsync(notification);
        //        return Ok(dto);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest("Erreur lors de la mise à jour du rapport: " + ex.Message);
        //    }
        //}

        //[HttpDelete("Delete/{id:int}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    try
        //    {
        //        await _notificationService.DeleteNotificationAsync(id);
        //        return NoContent();
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Une erreur est survenue lors de la suppression de la notification: " + ex.Message);
        //    }
        //}
    }
}
