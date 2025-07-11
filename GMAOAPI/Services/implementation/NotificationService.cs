using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Repository;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.SeriLog;
using Mapster;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Services.implementation
{
    public class NotificationService : INotificationService
    {
        private readonly IGenericRepository<Notification> _repository;
        private readonly UserManager<Utilisateur> _userManager;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationService(
            IGenericRepository<Notification> repository,
            UserManager<Utilisateur> userManager,
            IRedisCacheService cache,
            ISerilogService serilogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _userManager = userManager;
            _cache = cache;
            _serilogService = serilogService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<NotificationDto>> GetAllNotificationDtosAsync(int pageNumber, int pageSize)
        {
            string? userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new Exception("Utilisateur non trouvé.");

            Expression<Func<Notification, bool>> filter = n => n.DestinataireId == userId;

            string cacheKey = $"notifications_{pageNumber}_{pageSize}_{userId}";
            var cached = _cache.GetData<List<NotificationDto>>(cacheKey);
            if (cached != null)
                return cached;

            var notifications = await _repository.FindAllAsync(
                filter: filter,
                includeProperties: "Destinataire",
                pageNumber: pageNumber,
                pageSize: pageSize,
                orderBy: o => o.OrderByDescending(n => n.Date)
            );
            var dtos = notifications.Select(n => n.Adapt<NotificationDto>()).ToList();
            _cache.SetData(cacheKey, dtos);
            _serilogService.LogAudit("Get All Notifications", $"PageNumber: {pageNumber}, PageSize: {pageSize}, UserId: {userId}");
            return dtos;
        }


        public async Task<NotificationDto> GetNotificationDtoByIdAsync(int id)
        {
            string cacheKey = $"notification_{id}";
            var cached = _cache.GetData<NotificationDto>(cacheKey);
            if (cached != null)
                return cached;

            var notification = await _repository.GetByIdAsync(new object[] { id }, includeProperties: "Destinataire");
            if (notification == null)
                throw new Exception("Notification non trouvée.");
            notification.Statut = StatutNotification.Lue;
            await _repository.UpdateAsync(notification);
            var dto = notification.Adapt<NotificationDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Get Notification by Id", $"NotificationId: {id}");
            return dto;
        }

        public async Task<NotificationDto> CreateNotificationDtoAsync(NotificationCreateDto createDto)
        {
            if (createDto == null)
                throw new ArgumentNullException(nameof(createDto));

            createDto.Date = DateTime.Now;

            if (string.IsNullOrWhiteSpace(createDto.DestinataireId))
                throw new Exception("Le destinataire est obligatoire.");

            var destinataire = await _userManager.FindByIdAsync(createDto.DestinataireId);
            if (destinataire == null)
                throw new Exception("Le destinataire spécifié n'existe pas.");

            var notification = createDto.Adapt<Notification>();
            var created = await _repository.CreateAsync(notification);

            await _cache.RemoveByPrefixAsync("GMAO_notifications_");

            string cacheKey = $"notification_{created.Id}";
            var dto = created.Adapt<NotificationDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Create Notification", $"NotificationId: {created.Id}");
            return dto;
        }


    }
}
