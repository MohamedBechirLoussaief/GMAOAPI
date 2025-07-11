using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Repository;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.SeriLog;
using Mapster;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using GMAOAPI.DTOs.UpdateDTOs;
using System.Security.Claims;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Data;
using GMAOAPI.Services.Interfaces;
using Azure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GMAOAPI.Services.implementation
{
    public class RapportService : IRapportService
    {
        private readonly IGenericRepository<Rapport> _repository;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        private readonly IInterventionService _interventionService;
        private readonly IEquipementService _equipementService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly GmaoDbContext _dbContext;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;


        public RapportService(IGenericRepository<Rapport> repository,
                              IRedisCacheService cache,
                              ISerilogService serilogService,
                              IInterventionService interventionService,
                                          IHttpContextAccessor httpContextAccessor,
                                          IEquipementService equipementService,
                                          GmaoDbContext dbContext,
                                          IAuditService auditService,
                                          INotificationService notificationService

                                          )

        {
            _repository = repository;
            _cache = cache;
            _serilogService = serilogService;
            _interventionService = interventionService;
            _httpContextAccessor = httpContextAccessor;
            _equipementService = equipementService;
            _dbContext = dbContext;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<List<RapportDto>> GetAllRapportDtosAsync(
     int pageNumber,
     int pageSize,
     int? id = null,
     string? titre = null,
     int? interventionId = null,
     bool? isArchived = false,
     DateTime? dateCreation = null,
     string? createurId = null,
     bool? isValid = null,
     string? valideurId = null
 )
        {
            var user = _httpContextAccessor.HttpContext.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isTech = user.IsInRole("Technicien");

            string cacheKey = $"rapports_{pageNumber}_{pageSize}_{id}_{titre}_{interventionId}_{isArchived}_{dateCreation}_{createurId}_{isValid}_{valideurId}_{userId}_sorted";
            var cached = _cache.GetData<List<RapportDto>>(cacheKey);
            if (cached != null)
                return cached;

            Expression<Func<Rapport, bool>> filter = r =>
                (!id.HasValue || r.Id == id.Value) &&
                (string.IsNullOrEmpty(titre) || r.Titre.Contains(titre)) &&
                (!interventionId.HasValue || r.InterventionId == interventionId.Value) &&
                (!isArchived.HasValue || r.IsArchived == isArchived.Value) &&
                (!dateCreation.HasValue || r.DateCreation.Date == dateCreation.Value.Date) &&
                (string.IsNullOrEmpty(createurId) || r.CreateurId == createurId) &&
                (!isValid.HasValue || r.IsValid == isValid.Value) &&
                (string.IsNullOrEmpty(valideurId) || r.ValideurId == valideurId) &&
                (!isTech || r.Intervention.InterventionTechniciens.Any(it => it.TechnicienId == userId));

            var rapports = await _repository.FindAllAsync(
                filter: filter,
                includeProperties: "Intervention,Createur,Valideur,Intervention.Equipement,Intervention.InterventionTechniciens",
                orderBy: q => q.OrderByDescending(r => r.DateCreation),
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            var dtos = rapports.Select(r => r.Adapt<RapportDto>()).ToList();

            _cache.SetData(cacheKey, dtos);

            

            return dtos;
        }




        public async Task<RapportDto> GetRapportDtoByIdAsync(int id)
        {
            string cacheKey = $"rapport_{id}";
            var cached = _cache.GetData<RapportDto>(cacheKey);
            if (cached != null)
                return cached;

            var rapport = await _repository.GetByIdAsync(new object[] { id }, includeProperties: "Intervention,Intervention.Createur,Intervention.Equipement,Valideur,Createur,Intervention.InterventionTechniciens.Technicien,Intervention.InterventionPieceDetachees,Intervention.InterventionPieceDetachees.PieceDetachee,Intervention.Createur");
            if (rapport == null)
                throw new Exception("Rapport non trouvé.");

            var dto = rapport.Adapt<RapportDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Get Rapport by Id", $"RapportId: {id}");
            return dto;
        }

        public async Task<RapportDto> CreateRapportDtoAsync(RapportCreateDto createDto, string userId)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (createDto == null)
                    throw new ArgumentNullException(nameof(createDto));

                var rapport = createDto.Adapt<Rapport>();
                rapport.CreateurId = userId;

                var intervention = await _interventionService.GetInterventionDtoByIdAsync(rapport.InterventionId);
                if (intervention == null)
                    throw new Exception("Intervention introuvable .");

                if (intervention.RapportId != null && intervention.RapportId > 0)
                    throw new Exception("Un rapport existe déjà pour cette intervention.");

                if (intervention.Statut == StatutIntervention.Annulee)
                    throw new Exception("Cette intervention est annulée. Vous ne pouvez pas créer un rapport pour cette intervention.");

                var equipement = await _equipementService.GetEquipementDtoByIdAsync(intervention.EquipementDto.Id);

                if (createDto.DateDebut == default || createDto.DateFin == default)
                    throw new Exception("Les dates de début et de fin sont obligatoires.");

                if (createDto.DateDebut > DateTime.Now || createDto.DateFin > DateTime.Now)
                    throw new Exception("Les dates de début et de fin ne peuvent pas être dans le futur.");

                if (createDto.DateDebut > createDto.DateFin)
                    throw new Exception("La date de début ne peut pas être supérieure à la date de fin.");

                var created = await _repository.CreateAsync(rapport);
                if (created == null)
                    return null;

                EtatEquipement etat = EtatEquipement.EnPanne;

                if (created.Resultat == ResultatIntervention.Succes)
                {
                    etat = EtatEquipement.EnService;
                    if(intervention.Type == TypeIntervention.Installation)
                    {
                        equipement.DateInstallation = intervention.DateFin;
                    }
                }
                if (created.Resultat == ResultatIntervention.Echec)
                {
                    if (intervention.Type == TypeIntervention.Installation)
                    {
                        etat = EtatEquipement.HorsService;
                    }
                    else
                    {
                        etat = EtatEquipement.EnPanne;
                    }
                }


                await _equipementService.ModifierEtatEquipementAsync(equipement.Id, etat);
                var equipementUpdateDto = equipement.Adapt<EquipementUpdateDto>();
                await _equipementService.UpdateEquipementDtoAsync(equipement.Id, equipementUpdateDto);
                Intervention interToUpdate = intervention.Adapt<Intervention>();
                interToUpdate.RapportId = created.Id;
                interToUpdate.DateDebut = createDto.DateDebut;
                interToUpdate.DateFin = createDto.DateFin;

                await _interventionService.UpdateInterventionDtoAsync(
                    interToUpdate.Id,
                    interToUpdate.Adapt<InterventionUpdateDto>()
                );

                if (intervention.Statut != StatutIntervention.Terminee)
                    await _interventionService.TerminerInterventionAsync(interToUpdate.Id);

                await _cache.RemoveByPrefixAsync("GMAO_rapports_");
                await _cache.RemoveByPrefixAsync("GMAO_interventions_");

                var dto = created.Adapt<RapportDto>();

                await _auditService.CreateAuditAsync(
                    actionEffectuee: "Creation d'une rapport",
                    type: ActionType.Creation,
                    entityName: "Intervention",
                    entityId: created.InterventionId.ToString()
                );

                var notification = new NotificationCreateDto
                {
                    Message = $"Un rapport a été créé pour l’intervention numero {intervention.Id}.",
                    Date = DateTime.Now,
                    DestinataireId = intervention.CreateurDto.Id,
                };

                await _notificationService.CreateNotificationDtoAsync(notification);

                await transaction.CommitAsync();

                return dto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        public async Task<RapportDto> UpdateRapportDtoAsync(int id, RapportUpdateDto rapport)
        {
            if (rapport == null)
                throw new ArgumentNullException(nameof(rapport));

            var oldRapport = await _repository.GetByIdAsync(
                new object[] { id },
                includeProperties: "Intervention,Intervention.Createur,Intervention.Equipement," +
                                   "Valideur,Createur,Intervention.InterventionTechniciens.Technicien," +
                                   "Intervention.InterventionPieceDetachees," +
                                   "Intervention.InterventionPieceDetachees.PieceDetachee," +
                                   "Intervention.Createur"
            );

            if (oldRapport == null)
                throw new Exception("Rapport non trouvé.");

            var changes = new List<string>();

            if (oldRapport.Titre != rapport.Titre)
            {
                changes.Add($"Titre: « {oldRapport.Titre} » ➜ « {rapport.Titre} »");
                oldRapport.Titre = rapport.Titre;
            }

            if (oldRapport.Contenu != rapport.Contenu)
            {
                changes.Add("Contenu modifié");
                oldRapport.Contenu = rapport.Contenu;
            }

            EtatEquipement etat = EtatEquipement.EnPanne;

            if (oldRapport.Resultat != rapport.Resultat)
            {
                changes.Add($"Résultat: {oldRapport.Resultat} ➜ {rapport.Resultat}");
                oldRapport.Resultat = rapport.Resultat;

                if (oldRapport.Resultat == ResultatIntervention.Succes)
                {
                    etat = EtatEquipement.EnService;
                }

                if (oldRapport.Resultat == ResultatIntervention.Echec)
                {
                    if (oldRapport.Intervention.Type == TypeIntervention.Installation)
                    {
                        etat = EtatEquipement.HorsService;
                    }
                    else
                    {
                        etat = EtatEquipement.EnPanne;
                    }
                }

                oldRapport.Intervention.Equipement.Etat = etat;
            }

            var updated = await _repository.UpdateAsync(oldRapport);

            if (updated == null)
                throw new Exception("Rapport non trouvé ou conflit de mise à jour.");

            var dto = updated.Adapt<RapportDto>();

            await _cache.RemoveByPrefixAsync("GMAO_rapports_");
            await _cache.RemoveByPrefixAsync("GMAO_equipements_");

            string cacheKey = $"rapport_{updated.Id}";
            _cache.SetData(cacheKey, dto);
            _cache.RemoveData($"intervention_{updated.InterventionId}");
            _cache.RemoveData($"equipement_{updated.Intervention.EquipementId}");

            if (changes.Count > 0)
            {
                var description = string.Join(" | ", changes);

                await _auditService.CreateAuditAsync(
                    actionEffectuee: $"Mise à jour du rapport : {description}",
                    type: ActionType.Modification,
                    entityName: "Intervention",
                    entityId: updated.InterventionId.ToString()
                );
            }

            return dto;
        }

        public async Task<RapportDto> ValiderRapportAsync(int id)
        {
            string? userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new Exception("Utilisateur non trouvé.");

            var oldRapport = await _repository.GetByIdAsync(new object[] { id }, includeProperties: "Intervention,Intervention.Createur,Intervention.Equipement,Valideur,Createur,Intervention.InterventionTechniciens.Technicien,Intervention.InterventionPieceDetachees,Intervention.InterventionPieceDetachees.PieceDetachee");
            if (oldRapport == null)
                throw new Exception("Rapport non trouvé.");

            oldRapport.ValideurId = userId;
            oldRapport.IsValid = true;

            var updated = await _repository.UpdateAsync(oldRapport);
            if (updated == null)
                throw new Exception("Erreur lors la validation de rapport.");

            await _cache.RemoveByPrefixAsync("GMAO_rapports_");
            string cacheKey = $"rapport_{updated.Id}";
            var dto = updated.Adapt<RapportDto>();
            _cache.RemoveData(cacheKey);

            await _auditService.CreateAuditAsync(
                actionEffectuee: $"Validation du rapport",
                type: ActionType.Modification,
                entityName: "Intervention",
                entityId: updated.InterventionId.ToString()
            );

            return dto;
        }


        public async Task DeleteRapportAsync(int id)
        {
            var rapport = await _repository.GetByIdAsync(new object[] { id });
            if (rapport == null)
                throw new Exception("Rapport non trouvé.");

            rapport.IsArchived = true;
            var result = await _repository.UpdateAsync(rapport);
            if (result == null)
                throw new Exception("La suppression a échoué.");

            string cacheKey = $"rapport_{id}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_rapports_");

            await _auditService.CreateAuditAsync(
                           actionEffectuee: $"Archivage du rappport ",
                           type: ActionType.Suppression,
                           entityName: "Intervention",
                           entityId: rapport.InterventionId.ToString()
                       );
        }
        public async Task UnarchiveRapportAsync(int id)
        {
            var rapport = await _repository.GetByIdAsync(new object[] { id });
            if (rapport == null)
                throw new Exception("Rapport non trouvé.");

            rapport.IsArchived = false;
            var result = await _repository.UpdateAsync(rapport);
            if (result == null)
                throw new Exception("La Unarchive a échoué.");

            string cacheKey = $"rapport_{id}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_rapports_");

            await _auditService.CreateAuditAsync(
                           actionEffectuee: $"Désarchivage du rappport ",
                           type: ActionType.Modification,
                           entityName: "Intervention",
                           entityId: rapport.InterventionId.ToString()
                       );
        }
        public async Task<int> CountAsync(
           int? id = null,
           string? titre = null,
           int? interventionId = null,
           bool? isArchived = false,
           DateTime? dateCreation = null,
           string? createurId = null,
           bool? isValid = null,
           string? valideurId = null
       )
        {
            var user = _httpContextAccessor.HttpContext.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isTech = user.IsInRole("Technicien");

            Expression<Func<Rapport, bool>> filter = r =>
                (!id.HasValue || r.Id == id.Value) &&
                (string.IsNullOrEmpty(titre) || r.Titre.Contains(titre)) &&
                (!interventionId.HasValue || r.InterventionId == interventionId.Value) &&
                (!isArchived.HasValue || r.IsArchived == isArchived.Value) &&
                (!dateCreation.HasValue || r.DateCreation.Date == dateCreation.Value.Date) &&
                (string.IsNullOrEmpty(createurId) || r.CreateurId == createurId) &&
                (!isValid.HasValue || r.IsValid == isValid.Value) &&
                (string.IsNullOrEmpty(valideurId) || r.ValideurId == valideurId) &&
                (!isTech || r.Intervention.InterventionTechniciens.Any(it => it.TechnicienId == userId));
            

            return await _repository.CountAsync(filter);
        }

        public async Task<bool> IsRapportValidAsync(int rapportId)
        {
            var rapport = await GetRapportDtoByIdAsync(rapportId);
            if (rapport == null)
                throw new Exception("Rapport non trouvé.");

            return rapport.IsValid;
        }

        public async Task InvaliderRapportAsync(int id)
        {
            var oldRapport = await _repository.GetByIdAsync(new object[] { id });
            if (oldRapport == null)
                throw new Exception("Rapport non trouvé.");

            if (!oldRapport.IsValid)
                throw new Exception("Ce rapport est déjà non validé.");

            oldRapport.IsValid = false;
            oldRapport.ValideurId = null;

            var updated = await _repository.UpdateAsync(oldRapport);
            if (updated == null)
                throw new Exception("Erreur lors de l'invalidation du rapport.");

            await _cache.RemoveByPrefixAsync("GMAO_rapports_");
            string cacheKey = $"rapport_{updated.Id}";
            _cache.RemoveData(cacheKey);

            await _auditService.CreateAuditAsync(
                actionEffectuee: $"Invalidation du rapport",
                type: ActionType.Modification,
                entityName: "Intervention",
                entityId: updated.InterventionId.ToString()
            );

        }

    }
}
