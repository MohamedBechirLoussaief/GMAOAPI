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
using GMAOAPI.Models.Enumerations;
using GMAOAPI.DTOs.UpdateDTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using GMAOAPI.Data;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI.Services.implementation
{
    public class PlanificationService : IPlanificationService
    {
        private readonly IGenericRepository<Planification> _repository;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        private readonly IGenericRepository<Intervention> _interventionRepository;
        private readonly IGenericRepository<Equipement> _equipementRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly GmaoDbContext _dbContext;
        private readonly IInterventionTechnicienService _interventionTechnicienService;
        private readonly IAuditService _auditService;


        public PlanificationService(
            IGenericRepository<Planification> repository,
            IRedisCacheService cache,
            ISerilogService serilogService,
            IGenericRepository<Equipement> equipementRepository,

            IGenericRepository<Intervention> interventionRepository
            ,
                       IInterventionTechnicienService interventionTechnicienService,
            GmaoDbContext dbContext,

            IHttpContextAccessor httpContextAccessor,
            IAuditService auditService)

        {
            _repository = repository;
            _cache = cache;
            _serilogService = serilogService;
            _equipementRepository = equipementRepository;
            _interventionRepository = interventionRepository;
            _httpContextAccessor = httpContextAccessor;
            _interventionTechnicienService = interventionTechnicienService;
            _dbContext = dbContext;
            _auditService = auditService;
        }

        public async Task<List<PlanificationDto>> GetAllPlanificationDtosListAsync(
    int pageNumber,
    int pageSize,
    int? id = null,
    string? description = null,
    string? statut = null,
    string? type = null,
    DateTime? dateDebut = null,
    bool? semaine = false,
    int? mois = null,
    string? frequence = null,
    int? interventionId = null,
    int? equipementId = null,
    bool? isArchived = false)
        {
            var today = DateTime.Today;
            var firstDayOfWeek = DayOfWeek.Monday;
            int diff = (7 + (today.DayOfWeek - firstDayOfWeek)) % 7;
            var startOfWeek = today.AddDays(-diff);
            var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

            var user = _httpContextAccessor.HttpContext.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var isTech = user.IsInRole("Technicien");

            string cacheKey =
                $"planifications_{pageNumber}_{pageSize}_" +
                $"{description}_{statut}_{type}_" +
                $"{id}_{dateDebut}_{semaine}_{mois}_" +
                $"{frequence}_{interventionId}_{equipementId}_{isArchived}"
                  + (isTech ? $"_user_{userId}" : "");
            var cached = _cache.GetData<List<PlanificationDto>>(cacheKey);
            if (cached != null)
                return cached;

            FrequencePlanification parsedFrequence = default;
            bool isFrequenceValid =
                string.IsNullOrEmpty(frequence)
                || Enum.TryParse(frequence, out parsedFrequence);

            StatutIntervention parsedStatut = default;
            bool isStatutValid = string.IsNullOrEmpty(statut)
                || Enum.TryParse(statut, out parsedStatut);

            TypeIntervention parsedType = default;
            bool isTypeValid = string.IsNullOrEmpty(type)
                || Enum.TryParse(type, out parsedType);

            Expression<Func<Planification, bool>> filter = p =>
                (!id.HasValue || p.Id == id.Value) &&
                (!isArchived.HasValue || p.IsArchived == isArchived.Value) &&
                (!dateDebut.HasValue || p.DateDebut.Date == dateDebut.Value.Date) &&
                (!mois.HasValue || p.DateDebut.Month == mois.Value && p.DateDebut.Year == today.Year) &&
                (!semaine.HasValue || !semaine.Value || p.DateDebut.Date >= startOfWeek && p.DateDebut.Date <= endOfWeek) &&
                (string.IsNullOrEmpty(frequence) || isFrequenceValid && p.Frequence == parsedFrequence) &&
                (!interventionId.HasValue || p.InterventionId == interventionId.Value) &&
                (string.IsNullOrEmpty(description) || p.Intervention.Description.Contains(description)) &&
                (string.IsNullOrEmpty(statut) || isStatutValid && p.Intervention.Statut == parsedStatut) &&
                (string.IsNullOrEmpty(type) || isTypeValid && p.Intervention.Type == parsedType) &&
                (!equipementId.HasValue || p.Intervention != null && p.Intervention.EquipementId == equipementId.Value) &&
                (!isTech || p.Intervention.InterventionTechniciens.Any(t => t.TechnicienId == userId));

            var planifications = await _repository.FindAllAsync(
                filter,
                includeProperties: "Intervention,Intervention.Equipement",
                orderBy: q => q.OrderBy(p => p.DateDebut),
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            var dtos = planifications
                .Select(p => p.Adapt<PlanificationDto>())
                .ToList();

            _cache.SetData(cacheKey, dtos);
            _serilogService.LogAudit(
                "Get All Planifications",
                $"Page: {pageNumber}, Size: {pageSize}, " +
                $"Filters: id={id}, dateDebut={dateDebut?.ToShortDateString()}, " +
                $"semaine={semaine}, mois={mois}, " +
                $"frequence={frequence}, interventionId={interventionId}, " +
                $"equipementId={equipementId}, archive={(isArchived == true ? "Oui" : "Non")}"
            );

            return dtos;
        }
        public async Task<List<PlanificationDto>> GetAllPlanificationDtosAsync(
            DateTime startDate,
            DateTime endDate,
            int? id = null,
            string? description = null,
            string? statut = null,
            string? type = null,
            string? frequence = null,
            int? interventionId = null,
            int? equipementId = null,
            bool? isArchived = false)
        {
            var from = startDate.Date;
            var to = endDate.Date.AddDays(1).AddTicks(-1);

            var user = _httpContextAccessor.HttpContext!.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var isTech = user.IsInRole("Technicien");

            string cacheKey =
                $"planifications_{from:yyyyMMdd}_{to:yyyyMMdd}_" +
                $"{id}_{description}_{statut}_{type}_{frequence}_" +
                $"{interventionId}_{equipementId}_{isArchived}"
                + (isTech ? $"_user_{userId}" : "");

            if (_cache.GetData<List<PlanificationDto>>(cacheKey) is { } cached)
                return cached;

            FrequencePlanification parsedFrequence = default;
            bool isFrequenceValid = string.IsNullOrEmpty(frequence)
                || Enum.TryParse(frequence, out parsedFrequence);

            StatutIntervention parsedStatut = default;
            bool isStatutValid = string.IsNullOrEmpty(statut)
                || Enum.TryParse(statut, out parsedStatut);

            TypeIntervention parsedType = default;
            bool isTypeValid = string.IsNullOrEmpty(type)
                || Enum.TryParse(type, out parsedType);

            Expression<Func<Planification, bool>> filter = p =>
                (!id.HasValue || p.Id == id.Value) &&
                (!isArchived.HasValue || p.IsArchived == isArchived.Value) &&
                (p.DateDebut >= from && p.DateDebut <= to) &&
                (string.IsNullOrEmpty(frequence) || (isFrequenceValid && p.Frequence == parsedFrequence)) &&
                (!interventionId.HasValue || p.InterventionId == interventionId.Value) &&
                (string.IsNullOrEmpty(description) || p.Intervention.Description.Contains(description)) &&
                (string.IsNullOrEmpty(statut) || (isStatutValid && p.Intervention.Statut == parsedStatut)) &&
                (string.IsNullOrEmpty(type) || (isTypeValid && p.Intervention.Type == parsedType)) &&
                (!equipementId.HasValue || p.Intervention.EquipementId == equipementId.Value) &&
                (!isTech || p.Intervention.InterventionTechniciens.Any(t => t.TechnicienId == userId));

            var planifications = await _repository.FindAllAsync(
                filter,
                includeProperties: "Intervention,Intervention.Equipement",
                orderBy: q => q.OrderBy(p => p.DateDebut)
            );

            var dtos = planifications
                .Select(p => p.Adapt<PlanificationDto>())
                .ToList();

            _cache.SetData(cacheKey, dtos);

           
            return dtos;
        }


        public async Task<PlanificationDto> GetPlanificationDtoByIdAsync(int id)
        {
            string cacheKey = $"planification_{id}";
            var cached = _cache.GetData<PlanificationDto>(cacheKey);
            if (cached != null)
                return cached;

            var planification = await _repository.GetByIdAsync(new object[] { id });
            if (planification == null)
                throw new Exception("Planification non trouvée.");

            var dto = planification.Adapt<PlanificationDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Get Planification by Id", $"PlanificationId: {id}");
            return dto;
        }
        public async Task<PlanificationDto> CreatePlanificationWithInterventionAsync(PlanificationCreateDto createDto, string userId)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (createDto == null)
                    throw new ArgumentNullException(nameof(createDto));

                if (createDto.DateDebut < DateTime.Now)
                    throw new Exception("La date de planification ne peut pas être dans le passé.");

                if (createDto.DateDebut >= createDto.DateFin)
                    throw new Exception("La date de début doit être antérieure à la date de fin.");

                var intervention = createDto.interventionCreateDto.Adapt<Intervention>();

                var equipment = await _equipementRepository.GetByIdAsync(
                    new object[] { createDto.interventionCreateDto.EquipementId });

                if (equipment == null)
                    throw new Exception("L'équipement spécifié n'existe pas.");

                var exist = await _repository.FindAllAsync(
                    p => p.Intervention.EquipementId == createDto.interventionCreateDto.EquipementId &&
                         createDto.DateDebut < p.DateFin &&
                         p.DateDebut < createDto.DateFin &&
                         p.IsArchived == false);

                if (exist.Any())
                    throw new Exception("Une planification existe déjà pour cet équipement dans cette période.");

                intervention.Statut = StatutIntervention.EnAttente;
                intervention.CreateurId = userId;

                var createdIntervention = await _interventionRepository.CreateAsync(intervention);
                if (createdIntervention == null)
                    throw new Exception("Erreur lors de la création de l'intervention.");

                await _auditService.CreateAuditAsync(
                    actionEffectuee: "Intervention planifiée créée",
                    type: ActionType.Creation,
                    entityName: "Intervention",
                    entityId: createdIntervention.Id.ToString());

                if (createDto.interventionCreateDto.TechnicienIds != null)
                {
                    foreach (var technicienId in createDto.interventionCreateDto.TechnicienIds)
                    {
                        var techCreate = new InterventionTechnicienCreateDto
                        {
                            InterventionId = createdIntervention.Id,
                            TechnicienId = technicienId
                        };
                        await _interventionTechnicienService.CreateTechnicienDtoAsync(techCreate);
                    }
                }

                var planification = createDto.Adapt<Planification>();
                planification.InterventionId = createdIntervention.Id;
                planification.Intervention = createdIntervention;
                planification.IsRecurring = planification.Frequence != FrequencePlanification.Ponctuelle;
                planification.ProchaineGeneration = planification.IsRecurring
                    ? CalculateNextGeneration(planification.Frequence, createDto.DateDebut)
                    : null;

                var createdPlanification = await _repository.CreateAsync(planification);
                if (createdPlanification == null)
                    throw new Exception("Erreur lors de la création de la planification.");

                await _auditService.CreateAuditAsync(
                    actionEffectuee: "Planification d'une intervention",
                    type: ActionType.Creation,
                    entityName: "Planification",
                    entityId: createdPlanification.InterventionId.ToString());

                await transaction.CommitAsync();

                await _cache.RemoveByPrefixAsync("GMAO_planifications_");
                await _cache.RemoveByPrefixAsync("GMAO_interventions_");

                string cacheKey = $"planification_{createdPlanification.Id}";
                var dto = createdPlanification.Adapt<PlanificationDto>();
                _cache.SetData(cacheKey, dto);

                return dto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public DateTime CalculateNextGeneration(FrequencePlanification frequence, DateTime from)
        {
            return frequence switch
            {
                FrequencePlanification.Hebdomadaire => from.AddDays(7),
                FrequencePlanification.Mensuelle => from.AddMonths(1),
                FrequencePlanification.Trimestrielle => from.AddMonths(3),
                FrequencePlanification.Semestrielle => from.AddMonths(6),
                FrequencePlanification.Annuelle => from.AddYears(1),

                _ => throw new ArgumentOutOfRangeException(
                                                           nameof(frequence),
                                                           frequence,
                                                           "Fréquence inconnue")
            };
        }



        public async Task<Planification?> UpdatePlanificationAsync(int id, PlanificationUpdateDto planif)
        {
            if (planif == null)
                throw new ArgumentNullException(nameof(planif));
            var planification = planif.Adapt<Planification>();

            var oldPlanification = await _repository.GetByIdAsync(new object[] { id });
            if (oldPlanification == null)
                throw new Exception("Planification non trouvée.");

            if (oldPlanification.DateDebut != planification.DateDebut && planification.DateDebut < DateTime.Now)
                throw new Exception("La date de planification ne peut pas être dans le passé.");
            if (oldPlanification.DateFin != planification.DateFin && planification.DateDebut >= planification.DateFin)
                throw new Exception("L'heure de début doit être antérieure à l'heure de fin.");



            var changes = new List<string>();

            if (oldPlanification.DateDebut != planification.DateDebut)
                changes.Add($"Date début: {oldPlanification.DateDebut:yyyy-MM-dd HH:mm} ➜ {planification.DateDebut:yyyy-MM-dd HH:mm}");

            if (oldPlanification.DateFin != planification.DateFin)
                changes.Add($"Date fin: {oldPlanification.DateFin:yyyy-MM-dd HH:mm} ➜ {planification.DateFin:yyyy-MM-dd HH:mm}");

            if (oldPlanification.Frequence != planification.Frequence)
                changes.Add($"Fréquence: {oldPlanification.Frequence} ➜ {planification.Frequence}");

            oldPlanification.DateDebut = planification.DateDebut;
            oldPlanification.DateFin = planification.DateFin;
            oldPlanification.Frequence = planification.Frequence;


            var updated = await _repository.UpdateAsync(oldPlanification);
            if (updated == null)
                throw new Exception("Planification non trouvé ou conflit de mise à jour.");

            string cacheKey = $"planification_{id}";
            var dto = updated.Adapt<PlanificationDto>();
            _cache.SetData(cacheKey, dto);
            await _cache.RemoveByPrefixAsync("GMAO_planifications_");
            _cache.RemoveData($"planification_intervention_{oldPlanification.InterventionId}");

            if (changes.Count>0)
            {
                var description = string.Join(" | ", changes);

                await _auditService.CreateAuditAsync(
                    actionEffectuee: $"Mise à jour de la planification : {description}",
                    type: ActionType.Modification,
                    entityName: "Intervention",
                    entityId: updated.InterventionId.ToString()
                );
            }

            return updated;

        }

        public async Task<bool> DeletePlanificationAsync(int id)
        {
            var planification = await _repository.GetByIdAsync(new object[] { id }, "Intervention");
            if (planification == null)
                throw new Exception("Planification non trouvée.");

            planification.IsArchived = true;

            if (planification.Intervention != null &&
                planification.Intervention.Statut == StatutIntervention.EnAttente)
            {
                planification.Intervention.IsArchived = true;
                await _auditService.CreateAuditAsync(
   actionEffectuee: $"Archivage de l'intervention ",
   type: ActionType.Suppression,
   entityName: "Intervention",
   entityId: planification.InterventionId.ToString()
);
            }

            var result = await _repository.UpdateAsync(planification);
            if (result == null)
                throw new Exception("Erreur lors de la suppression de la planification.");

            await _cache.RemoveByPrefixAsync("GMAO_planifications_");
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            _cache.RemoveData($"planification_{planification.Id}");
            _cache.RemoveData($"intervention_{planification.InterventionId}");
            _cache.RemoveData($"planification_intervention_{planification.InterventionId}");

            await _auditService.CreateAuditAsync(
                actionEffectuee: "Archivage de la planification",
                type: ActionType.Suppression,
                entityName: "Planification",
                entityId: planification.InterventionId.ToString()
            );

            return true;
        }


        public async Task<bool> UnarchivePlanificationAsync(int id)
        {


            var planification = await _repository.GetByIdAsync(new object[] { id }, "Intervention");
            if (planification == null)
                throw new Exception("Planification non trouvée.");
            planification.IsArchived = false;
            if (planification.Intervention.IsArchived = true)
            {
                planification.Intervention.IsArchived = false;
                await _auditService.CreateAuditAsync(
  actionEffectuee: $"Désarchivage de l'intervention ",
  type: ActionType.Modification,
  entityName: "Intervention",
  entityId: planification.InterventionId.ToString()
);
            }

            var result = await _repository.UpdateAsync(planification);
            if (result == null)
                throw new Exception("Erreur lors de la désarchivage de la planification.");


            await _cache.RemoveByPrefixAsync("GMAO_planifications_");
            await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            _cache.RemoveData($"planification_{planification.Id}");
            _cache.RemoveData($"intervention_{planification.InterventionId}");
            _cache.RemoveData($"planification_intervention_{planification.InterventionId}");

            _serilogService.LogAudit("Unarchive Planification", $"PlanificationId: {id}");
            return true;


        }





        public async Task<PlanificationDto?> GetPlanificationDtoByInterventionIdAsync(int id)
        {
            string cacheKey = $"planification_intervention_{id}";
            var cached = _cache.GetData<PlanificationDto>(cacheKey);
            if (cached != null)
                return cached;

            var planification = await _repository.GetByAsync(
                p => p.InterventionId == id
                );

            if (planification != null)
            {

                var dto = planification.Adapt<PlanificationDto>();
                _cache.SetData(cacheKey, dto);
                _serilogService.LogAudit("Get Planification by intervention Id", $"InterventionId: {id}");
                return dto;
            }
            return null;

        }



        public async Task<int> CountAsync(
     int? id = null,
     DateTime? dateDebut = null,
     int? jour = null,
     bool? semaine = false,
     int? mois = null,
     string? frequence = null,
     int? interventionId = null,
     int? equipementId = null,
     bool? isArchived = false)
        {

            var user = _httpContextAccessor.HttpContext.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var isTech = user.IsInRole("Technicien");

            FrequencePlanification parsedFrequence = default;
            bool isFrequenceValid = string.IsNullOrEmpty(frequence)
                || Enum.TryParse(frequence, out parsedFrequence);

            Expression<Func<Planification, bool>> filter = p =>
                (!id.HasValue || p.Id == id.Value) &&
                (!isArchived.HasValue || p.IsArchived == isArchived.Value) &&
                (!dateDebut.HasValue || p.DateDebut.Date == dateDebut.Value.Date) &&
                (!jour.HasValue || p.DateDebut.Day == jour.Value) &&
                (!mois.HasValue || p.DateDebut.Month == mois.Value) &&
                (!semaine.HasValue || !semaine.Value ||
                    p.DateDebut >= DateTime.Now && p.DateFin <= DateTime.Now.AddDays(7)) &&
                (string.IsNullOrEmpty(frequence) || isFrequenceValid && p.Frequence == parsedFrequence) &&
                (!interventionId.HasValue || p.InterventionId == interventionId.Value) &&
                (!equipementId.HasValue || p.Intervention != null && p.Intervention.EquipementId == equipementId.Value) &&
            (!isTech || p.Intervention.InterventionTechniciens.Any(t => t.TechnicienId == userId));

            return await _repository.CountAsync(filter);
        }


    }
}
