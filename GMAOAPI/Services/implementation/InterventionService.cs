using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Repository;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.SeriLog;
using Mapster;
using Microsoft.AspNetCore.Identity;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.DTOs.UpdateDTOs;
using static GMAOAPI.Services.implementation.InterventionService;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GMAOAPI.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Services.implementation
{
    public class InterventionService : IInterventionService
    {
        private readonly IGenericRepository<Intervention> _repository;
        private readonly IGenericRepository<Equipement> _equipmentRepository;
        private readonly IEquipementService _equipmentService;
        private readonly IInterventionPieceDetacheeService _interventionPieceDetacheeService;
        private readonly IInterventionTechnicienService _interventionTechnicienService;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        private readonly UserManager<Utilisateur> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly GmaoDbContext _dbContext;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;


        public InterventionService(
            IGenericRepository<Intervention> repository,
            IGenericRepository<Equipement> equipmentRepository,
            IRedisCacheService cache,
            ISerilogService serilogService,
            UserManager<Utilisateur> userManager,
            IHttpContextAccessor httpContextAccessor,
            IInterventionPieceDetacheeService interventionPieceDetacheeService,
            IInterventionTechnicienService interventionTechnicienService,
            GmaoDbContext dbContext,
            IAuditService auditService,
            IEquipementService equipmentService,
            INotificationService notificationService
            )
        {
            _repository = repository;
            _equipmentRepository = equipmentRepository;
            _cache = cache;
            _serilogService = serilogService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _interventionPieceDetacheeService = interventionPieceDetacheeService;
            _interventionTechnicienService = interventionTechnicienService;
            _dbContext = dbContext;
            _auditService = auditService;
            _equipmentService = equipmentService;
            _notificationService = notificationService;
        }
        public async Task<List<InterventionDto>> GetAllInterventionDtosAsync(
            int pageNumber,
            int pageSize,
            int? id = null,
            string? description = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null,
            string? statut = null,
            string? type = null,
            int? equipementId = null,
            string? technicienId = null,
            bool? isArchived = false)
        {
            var user = _httpContextAccessor.HttpContext.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var isTech = user.IsInRole("Technicien");

            string cacheKey = $"interventions_{pageNumber}_{pageSize}_{id}_{description}_{dateDebut}_{dateFin}_{statut}_{type}_{equipementId}_{isArchived}_{isTech}_{technicienId}"
                               + (isTech ? $"_user_{userId}" : "");
            var cached = _cache.GetData<List<InterventionDto>>(cacheKey);
            if (cached != null)
                return cached;

            StatutIntervention parsedStatut = default;
            bool isStatutValid = string.IsNullOrEmpty(statut)
                || Enum.TryParse(statut, out parsedStatut);

            TypeIntervention parsedType = default;
            bool isTypeValid = string.IsNullOrEmpty(type)
                || Enum.TryParse(type, out parsedType);

            Expression<Func<Intervention, bool>> filter = i =>
                (!id.HasValue || i.Id == id.Value) &&
                (!isArchived.HasValue || i.IsArchived == isArchived.Value) &&
                (string.IsNullOrEmpty(description) || i.Description.Contains(description)) &&
                (!dateDebut.HasValue || (i.DateDebut.HasValue && i.DateDebut.Value.Date == dateDebut.Value.Date))&&
                (!dateFin.HasValue || (i.DateFin.HasValue && i.DateFin.Value.Date == dateFin.Value.Date))&&
                (string.IsNullOrEmpty(statut) || isStatutValid && i.Statut == parsedStatut) &&
                (string.IsNullOrEmpty(type) || isTypeValid && i.Type == parsedType) &&
                (!equipementId.HasValue || i.EquipementId == equipementId.Value) &&
                (!isTech || i.InterventionTechniciens.Any(t => t.TechnicienId == userId)) &&
                (string.IsNullOrEmpty(technicienId) || i.InterventionTechniciens.Any(t => t.TechnicienId == technicienId));




            var interventions = await _repository.FindAllAsync(
                filter,
                includeProperties: "Equipement,Createur",
                orderBy: q => q.OrderByDescending(i => i.DateDebut),
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            var dtos = interventions
                .Select(i => i.Adapt<InterventionDto>())
                .ToList();

            _cache.SetData(cacheKey, dtos);
            _serilogService.LogAudit("Get All Intervention",
                $"Page: {pageNumber}, Size: {pageSize}, Filters: id:{id}, description:{description}, dateDebut:{dateDebut}, dateFin:{dateFin}, statut:{statut}, type:{type}, equipementId:{equipementId}, archive:{(isArchived == true ? "Oui" : "Non")}");

            return dtos;
        }



        public async Task<InterventionDto> GetInterventionDtoByIdAsync(int id)
        {
            string cacheKey = $"intervention_{id}";
            var cached = _cache.GetData<InterventionDto>(cacheKey);
            if (cached != null)
                return cached;

            var intervention = await _repository.GetByIdAsync(new object[] { id }, "Equipement,Createur,InterventionTechniciens.Technicien,InterventionPieceDetachees,InterventionPieceDetachees.PieceDetachee");
            if (intervention == null)
                throw new Exception("Intervention non trouvée.");

            var dto = intervention.Adapt<InterventionDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Get Intervention By Id", $"InterventionId: {id}");
            return dto;
        }

        public async Task<InterventionDto> CreateInterventionDtoAsync(InterventionCreateDto createDto, string userId)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                Utilisateur utilisateur = await _userManager.FindByIdAsync(userId);

                var equipment = await _equipmentRepository.GetByIdAsync(new object[] { createDto.EquipementId });
                if (equipment == null)
                    throw new Exception("L'équipement spécifié n'existe pas.");
                if (equipment.Etat == EtatEquipement.EnMaintenance)
                    throw new Exception("L'équipement est déjà en maintenance.");
                if (equipment.Etat == EtatEquipement.EnAttenteInstallation && createDto.Type != TypeIntervention.Installation)
                    throw new Exception("L'équipement est en attente d'installation.");
                if (equipment.Etat == EtatEquipement.EnCoursInstallation)
                    throw new Exception("L'équipement est en cours d'installation.");

                var intervention = createDto.Adapt<Intervention>();
                intervention.DateDebut = DateTime.Now;
                intervention.Statut = StatutIntervention.EnCours;
                intervention.Createur = utilisateur;

                var created = await _repository.CreateAsync(intervention)
                                 ?? throw new Exception("Échec de la création de l'intervention.");

                await _auditService.CreateAuditAsync(
                            actionEffectuee: "Intervention créée",
                            type: ActionType.Creation,
                            entityName: "Intervention",
                            entityId: created.Id.ToString()
                );

                EtatEquipement etat;
                if (intervention.Type == TypeIntervention.Installation)
                {
                    etat = EtatEquipement.EnCoursInstallation;
                }
                else
                {
                    etat = EtatEquipement.EnMaintenance;
                }

                await _equipmentService.ModifierEtatEquipementAsync(equipment.Id, etat);

                foreach (var technicienId in createDto.TechnicienIds)
                {
                    var techCreate = new InterventionTechnicienCreateDto
                    {
                        InterventionId = created.Id,
                        TechnicienId = technicienId
                    };
                    var it = await _interventionTechnicienService.CreateTechnicienDtoAsync(techCreate);
                    if (it == null)
                        throw new Exception("Échec de l'affectation du technicien");
                }

                var dto = created.Adapt<InterventionDto>();

                await _cache.RemoveByPrefixAsync("GMAO_interventions_");
                await _cache.RemoveByPrefixAsync($"GMAO_intervention_{created.Id}");
                _cache.SetData($"intervention_{created.Id}", dto);
           
                await transaction.CommitAsync();
                return dto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        public async Task<InterventionDto> UpdateInterventionDtoAsync(int id, InterventionUpdateDto interv)
        {
            if (interv == null)
                throw new ArgumentNullException(nameof(interv));

            var intervention = interv.Adapt<Intervention>();

            var oldIntervention = await _repository.GetByIdAsync(new object[] { id }, "Equipement, Createur, InterventionTechniciens.Technicien, InterventionPieceDetachees, InterventionPieceDetachees.PieceDetachee");
            if (oldIntervention == null)
                throw new Exception("Intervention non trouvée.");
            if ((interv.DateDebut != oldIntervention.DateDebut && interv.DateFin != oldIntervention.DateFin) && intervention.DateFin != default && intervention.DateDebut >= intervention.DateFin)
                throw new Exception("La date de début doit être antérieure à la date de fin.");
            if(intervention.DateFin != null && intervention.DateFin> DateTime.Now || intervention.DateDebut != null &&  intervention.DateDebut > DateTime.Now)
                throw new Exception("Les dates de début et de fin ne peuvent pas être dans le futur.");

            var changes = new List<string>();
            if (oldIntervention.Description != intervention.Description)
            {
                changes.Add($"Description: « {oldIntervention.Description} » ➜ « {intervention.Description} »");
                oldIntervention.Description = intervention.Description;
            }
            if (oldIntervention.DateDebut != null && intervention.DateDebut !=null &&
               oldIntervention.DateDebut.Value.ToString("yyyy-MM-dd HH:mm") != intervention.DateDebut.Value.ToString("yyyy-MM-dd HH:mm"))
            {
                changes.Add($"Date début: {oldIntervention.DateDebut:yyyy-MM-dd HH:mm} ➜ {intervention.DateDebut:yyyy-MM-dd HH:mm}");
                oldIntervention.DateDebut = intervention.DateDebut;
            }
            else if (oldIntervention.DateDebut == null && intervention.DateDebut != null)
            {
                throw new Exception("Tu ne peux pas modifier la date de début d'une intervention qui n’a pas encore commencé.");
            }

            if (oldIntervention.DateFin != null && intervention.DateFin!=null &&
                oldIntervention.DateFin.Value.ToString("yyyy-MM-dd HH:mm") != intervention.DateFin.Value.ToString("yyyy-MM-dd HH:mm"))
            {
                changes.Add($"Date fin: {oldIntervention.DateFin:yyyy-MM-dd HH:mm} ➜ {intervention.DateFin:yyyy-MM-dd HH:mm}");
                oldIntervention.DateFin = intervention.DateFin;
            }
            else if (oldIntervention.DateFin == null && intervention.DateFin != null)
            {
                throw new Exception("Tu ne peux pas modifier la date de fin d'une intervention qui n’est pas encore terminée.");
            }

            oldIntervention.RapportId = intervention.RapportId;

            var updated = await _repository.UpdateAsync(oldIntervention);
            if (updated == null)
                throw new Exception("Intervention non trouvée ou conflit de mise à jour.");

            var dto = updated.Adapt<InterventionDto>();

            string cacheKey = $"intervention_{updated.Id}";
            _cache.SetData(cacheKey, dto);
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            await _cache.RemoveByPrefixAsync("GMAO_planifications_");

            if (changes.Count >0)
            {
                var details = string.Join(" | ", changes);

                await _auditService.CreateAuditAsync(
                    actionEffectuee: $"Mise à jour intervention : {details}",
                    type: ActionType.Modification,
                    entityName: "Intervention",
                    entityId: updated.Id.ToString()
                );
            }

            return dto;

        }

        public async Task DeleteInterventionAsync(int id)
        {
            var intervention = await _repository.GetByIdAsync(new object[] { id });

            if (intervention == null)
                throw new Exception("Intervention non trouvée.");

            intervention.IsArchived = true;
            intervention.ArchiveReason = ArchiveReason.UserAction;

            if (intervention.Statut == StatutIntervention.EnAttente)
            {
                intervention.Statut = StatutIntervention.Annulee;
                intervention.AnnulationReason = AnnulationReason.UserAction;
            }

            if (intervention.Statut == StatutIntervention.EnCours)
                throw new Exception("Impossible d'archiver une intervention en cours. Veuillez la terminer ou l'annuler avant de l'archiver.");

            var result = await _repository.UpdateAsync(intervention);
            if (result == null)
                throw new Exception("L'archivage a échoué.");

            string cacheKey = $"intervention_{id}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            if(intervention.Planification!=null)
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");


            await _auditService.CreateAuditAsync(
               actionEffectuee: $"Archivage de l'intervention ",
               type: ActionType.Suppression,
               entityName: "Intervention",
               entityId: id.ToString()
           );
        }

        public async Task UnarchiveInterventionAsync(int id)
        {
            var intervention = await _repository.GetByIdAsync(new object[] { id });
            if (intervention == null)
                throw new Exception("Intervention non trouvée.");

            intervention.IsArchived = false;
            intervention.ArchiveReason = ArchiveReason.None;

            var result = await _repository.UpdateAsync(intervention);
            if (result == null)
                throw new Exception($"Échec de la désarchivage de l'intervention {id}.");

            string cacheKey = $"intervention_{id}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");

            if (intervention.Planification != null)
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");

            await _auditService.CreateAuditAsync(
              actionEffectuee: $"Désarchivage de l'intervention ",
              type: ActionType.Modification,
              entityName: "Intervention",
              entityId: id.ToString()
          );
        }
        public async Task<int> CountAsync(
                                    int? id = null,

    string? description = "",
    DateTime? dateDebut = null,
    DateTime? dateFin = null,
    string? statut = "",
    string? type = "",
    int? equipementId = null,
                string? technicienId = null,

    bool? isArchived = false)
        {
            var user = _httpContextAccessor.HttpContext.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var isTech = user.IsInRole("Technicien");

            StatutIntervention parsedStatut = default;
            bool isStatutValid = string.IsNullOrEmpty(statut) || Enum.TryParse(statut, out parsedStatut);

            TypeIntervention parsedType = default;
            bool isTypeValid = string.IsNullOrEmpty(type) || Enum.TryParse(type, out parsedType);

            Expression<Func<Intervention, bool>> filter = i =>
     (!id.HasValue || i.Id == id.Value) &&
     (!isArchived.HasValue || i.IsArchived == isArchived.Value) &&
     (string.IsNullOrEmpty(description) || i.Description.Contains(description)) &&
(!dateFin.HasValue || i.DateFin.Value.Date == dateFin.Value.Date) &&
     (!dateFin.HasValue || i.DateFin.Value.Date == dateFin.Value.Date) &&
     (string.IsNullOrEmpty(statut) || isStatutValid && i.Statut == parsedStatut) &&
     (string.IsNullOrEmpty(type) || isTypeValid && i.Type == parsedType) &&
     (!equipementId.HasValue || i.EquipementId == equipementId.Value) &&
            (!isTech || i.InterventionTechniciens.Any(t => t.TechnicienId == userId))&&
            (string.IsNullOrEmpty(technicienId) || i.InterventionTechniciens.Any(t => t.TechnicienId == technicienId));



            return await _repository.CountAsync(filter);
        }


        public async Task<object> GetInterventionStatsAsync()
        {
            int total = await CountAsync();
            int enCours = await CountAsync(statut: "EnCours");
            int terminee = await CountAsync(statut: "Terminee");
            int annulee = await CountAsync(statut: "Annulee");
            int enAttente = await CountAsync(statut: "EnAttente");

            return new
            {
                TotalInterventions = total,
                enCours,
                terminee,
                annulee,
                enAttente
            };
        }

        public async Task DemarrerInterventionAsync(int interventionId)
        {
            var intervention = await _repository.GetByIdAsync(new object[] { interventionId }, includeProperties: "Equipement");
            if (intervention == null)
                throw new Exception("Intervention non trouvée.");

            if (intervention.Statut == StatutIntervention.Terminee)
                throw new Exception("Impossible de démarrer une intervention déjà terminée.");

            if (intervention.Equipement.Etat == EtatEquipement.EnMaintenance)
                throw new Exception ("L'équipement est déjà en maintenance. Veuillez terminer l'intervention précédente avant de démarrer une nouvelle intervention.");

            intervention.Statut = StatutIntervention.EnCours;
            intervention.DateDebut = DateTime.Now;

            if(intervention.Type == TypeIntervention.Installation)
                intervention.Equipement.Etat = EtatEquipement.EnCoursInstallation;
            else
                intervention.Equipement.Etat = EtatEquipement.EnMaintenance; 

            var updated = await _repository.UpdateAsync(intervention)
                          ?? throw new Exception("Échec du démarrage de l'intervention.");

            string cacheKey = $"intervention_{interventionId}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            if (intervention.Planification != null)
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");

            await _auditService.CreateAuditAsync(
                actionEffectuee: "Démarrage de l’intervention",
                type: ActionType.Modification,
                entityName: "Intervention",
                entityId: updated.Id.ToString()
            );

            var notification = new NotificationCreateDto
            {
                Message = $"L’intervention numero {intervention.Id} a été démarrée.",
                Date = DateTime.Now,
                DestinataireId = intervention.CreateurId,
            };

            await _notificationService.CreateNotificationDtoAsync(notification);

        }

        public async Task MettreEnAttenteInterventionAsync(int interventionId)
        {
            var intervention = await _repository.GetByIdAsync(new object[] { interventionId });
            if (intervention == null)
                throw new Exception("Intervention non trouvée.");
            if (intervention.Statut != StatutIntervention.EnCours)
                throw new Exception("Impossible de mettre en attente une intervention qui n'est pas en cours.");

            intervention.Statut = StatutIntervention.EnAttente;

            var updated = await _repository.UpdateAsync(intervention)
                          ?? throw new Exception("Échec de la mise en attente de l'intervention.");

            string cacheKey = $"intervention_{interventionId}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            if (intervention.Planification != null)
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");

            await _auditService.CreateAuditAsync(
                actionEffectuee: "Mise en attente de l’intervention",
                type: ActionType.Modification,
                entityName: "Intervention",
                entityId: updated.Id.ToString()
            );

            var notification = new NotificationCreateDto
            {
                Message = $"L’intervention numero {intervention.Id} a été mise en attente.",
                Date = DateTime.Now,
                DestinataireId = intervention.CreateurId,
            };

            await _notificationService.CreateNotificationDtoAsync(notification);
        }

        public async Task TerminerInterventionAsync(int interventionId)
        {
            var intervention = await _repository.GetByIdAsync(new object[] { interventionId });
            if (intervention == null)
                throw new Exception("Intervention non trouvée.");

            if (intervention.Statut == StatutIntervention.Annulee || intervention.Statut == StatutIntervention.EnAttente)
                throw new Exception("Impossible de terminer une intervention annulée ou en attente.");

            intervention.Statut = StatutIntervention.Terminee;
            intervention.DateFin = DateTime.Now;

            var updated = await _repository.UpdateAsync(intervention)
                          ?? throw new Exception("Échec de la clôture de l'intervention.");

            string cacheKey = $"intervention_{interventionId}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            if (intervention.Planification != null)
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");

            await _auditService.CreateAuditAsync(
                actionEffectuee: "Clôture de l’intervention",
                type: ActionType.Modification,
                entityName: "Intervention",
                entityId: updated.Id.ToString()
            );

            var notification = new NotificationCreateDto
            {
                Message = $"L’intervention numero {intervention.Id} a été clôturée.",
                Date = DateTime.Now,
                DestinataireId = intervention.CreateurId,
            };

            await _notificationService.CreateNotificationDtoAsync(notification);
        }

        public async Task AnnulerInterventionAsync(int interventionId)
        {
            var intervention = await _repository.GetByIdAsync(new object[] { interventionId }, includeProperties: "Equipement");
            if (intervention == null)
                throw new Exception("Intervention non trouvée.");
            if (intervention.Statut == StatutIntervention.Terminee)
                throw new Exception("Impossible d’annuler une intervention déjà terminée.");

            intervention.Statut = StatutIntervention.Annulee;
            intervention.AnnulationReason = AnnulationReason.UserAction;

            if(intervention.Type==TypeIntervention.Installation)
                intervention.Equipement.Etat = EtatEquipement.EnAttenteInstallation;
            else
                intervention.Equipement.Etat = EtatEquipement.EnService;

            var updated = await _repository.UpdateAsync(intervention)
                          ?? throw new Exception("Échec de l’annulation de l'intervention.");

            string cacheKey = $"intervention_{interventionId}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            if (intervention.Planification != null)
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");

            await _auditService.CreateAuditAsync(
                actionEffectuee: $"Annulation de l’intervention",
                type: ActionType.Modification,
                entityName: "Intervention",
                entityId: updated.Id.ToString()
            );

            var notification = new NotificationCreateDto
            {
                Message = $"L’intervention numero {intervention.Id} a été annulée.",
                Date = DateTime.Now,
                DestinataireId = intervention.CreateurId,
            };

            await _notificationService.CreateNotificationDtoAsync(notification);

        }

        public async Task<List<object>> GetChartDataAsync()
        {
            DateTime today = DateTime.Today;

            int daysSinceMonday = today.DayOfWeek == DayOfWeek.Sunday
                ? 6
                : (int)today.DayOfWeek - (int)DayOfWeek.Monday;

            DateTime monday = today.DayOfWeek == DayOfWeek.Monday
                ? today.Date
                : today.AddDays(-daysSinceMonday).Date;

            DateTime sunday = monday.AddDays(6).Date.AddDays(1).AddTicks(-1);


            var interventions = await _repository.FindAllAsync(
                includeProperties:"Planification",
                filter: i =>
                    !i.IsArchived && (
                        (i.DateDebut.HasValue &&
                         i.DateDebut.Value.Date >= monday &&
                         i.DateDebut.Value.Date <= sunday)
                        ||
                        (i.Planification != null &&
                         i.Planification.DateDebut >= monday &&
                         i.Planification.DateFin <= sunday &&
                         i.Planification.IsArchived != true)
                    )

            );


            string[] jours = { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
            var result = new List<object>();

            for (int i = 0; i < 7; i++)
            {
                DateTime date = monday.AddDays(i);
                var interventionsDuJour = interventions
            .Where(i => (i.DateDebut.HasValue && i.DateDebut.Value.Date == date.Date) || (i.Planification !=null && i.Planification.DateDebut.Date == date.Date));

                int preventive = interventionsDuJour.Count(i => i.Type == TypeIntervention.Preventive);
                int corrective = interventionsDuJour.Count(i => i.Type == TypeIntervention.Corrective);

                result.Add(new
                {
                    day = jours[i],
                    preventive,
                    corrective
                });
            }

            return result;
        }



    }



}
