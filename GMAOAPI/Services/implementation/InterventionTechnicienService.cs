using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Repository;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.SeriLog;
using Mapster;
using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using GMAOAPI.Data;
using GMAOAPI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GMAOAPI.Services.implementation
{
    public class InterventionTechnicienService : IInterventionTechnicienService
    {
        private readonly IGenericRepository<InterventionTechnicien> _repository;
        private readonly IGenericRepository<Intervention> _interventionRepository;
        private readonly UserManager<Utilisateur> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        protected readonly GmaoDbContext _dbContext;


        public InterventionTechnicienService(
            IGenericRepository<InterventionTechnicien> repository,
            IGenericRepository<Intervention> interventionRepository,
            UserManager<Utilisateur> userManager,
            INotificationService notificationService,
            IRedisCacheService cache,
            ISerilogService serilogService,
            IAuditService auditService,
            GmaoDbContext dbContext
            )
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _interventionRepository = interventionRepository ?? throw new ArgumentNullException(nameof(interventionRepository));
            _userManager = userManager;
            _notificationService = notificationService;
            _cache = cache;
            _serilogService = serilogService;
            _auditService = auditService;
            _dbContext = dbContext;
        }

        public async Task<InterventionTechnicienDto> CreateTechnicienDtoAsync(InterventionTechnicienCreateDto createDto)
        {
            var interventionTechnicien = createDto.Adapt<InterventionTechnicien>();

            var intervention = await _interventionRepository.GetByIdAsync(
                new object[] { interventionTechnicien.InterventionId },
                includeProperties: "Equipement,Planification"
            );

            if (intervention == null)
                throw new Exception("L'intervention spécifiée n'existe pas.");

            var technicien = await _userManager.FindByIdAsync(interventionTechnicien.TechnicienId);
            if (technicien == null)
                throw new Exception("Le technicien spécifié n'existe pas.");

            if (intervention.Statut == StatutIntervention.Terminee || intervention.Statut == StatutIntervention.Annulee)
                throw new Exception("Impossible d'affecter un technicien à une intervention terminée ou annulée.");

            var exist = await _repository.ExistsAsync(new object[] { interventionTechnicien.TechnicienId, interventionTechnicien.InterventionId });
            if (exist == true)
                throw new Exception("Ce techncicien est déjà affecté a cette intervention");

            interventionTechnicien.Intervention = intervention;
            interventionTechnicien.Technicien = technicien as Technicien;

            var created = await _repository.CreateAsync(interventionTechnicien) ?? throw new Exception("L'affectation du technicien a échoué.");

            var notification = new NotificationCreateDto
            {
                Message = $"Vous avez été affecté à l'intervention ({intervention.Description}) " +
                          $"qui débute le {intervention.DateDebut ?? intervention.Planification?.DateDebut} pour la réparation de l'équipement {intervention.Equipement.Nom} " +
                          $"({intervention.Equipement.Reference}) situé dans {intervention.Equipement.Localisation}.",
                Date = DateTime.Now,
                DestinataireId = created.TechnicienId
            };

            await _notificationService.CreateNotificationDtoAsync(notification);
            await _cache.RemoveByPrefixAsync($"GMAO_intervention_{interventionTechnicien.InterventionId}");

            var dto = created.Adapt<InterventionTechnicienDto>();

            await _auditService.CreateAuditAsync(
                    actionEffectuee: $"Affectation du technicien {technicien.Nom} {technicien.Prenom}.",
                    type: ActionType.Modification,
                    entityName: "Intervention",
                    entityId: created.InterventionId.ToString()
                );

            return dto;

        }


        public async Task DeleteTechnicienAsync(object[] ids)
        {
            var entity = await _repository.GetByIdAsync(ids, "Intervention,Technicien");
            if (entity == null)
                throw new Exception("Affectation introuvable.");

            if (entity.Intervention.Statut == StatutIntervention.Terminee)
                throw new Exception("Impossible de retirer un technicien : l’intervention est déjà terminée.");

            var result = await _repository.DeleteAsync(entity);
            if (!result)
                throw new Exception("Échec du retrait du technicien de l'intervention.");

            var notification = new NotificationCreateDto
            {
                Message = $"Vous avez été retiré de l’intervention « {entity.Intervention.Description} ».",
                Date = DateTime.Now,
                DestinataireId = entity.Technicien.Id
            };
            await _notificationService.CreateNotificationDtoAsync(notification);

            await _cache.RemoveByPrefixAsync($"GMAO_intervention_{entity.InterventionId}");

            await _auditService.CreateAuditAsync(
                actionEffectuee: $"Retrait du technicien {entity.Technicien.Nom} {entity.Technicien.Prenom}.",
                type: ActionType.Modification,
                entityName: "Intervention",
                entityId: entity.InterventionId.ToString());
        }


    }
}
