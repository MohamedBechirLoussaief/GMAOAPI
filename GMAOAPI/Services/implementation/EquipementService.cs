using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Repository;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.SeriLog;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using GMAOAPI.Data;
using System.Numerics;
using GMAOAPI.Services.Interfaces;
using Azure;

namespace GMAOAPI.Services.implementation
{
    public class EquipementService : IEquipementService
    {
        private readonly IGenericRepository<Equipement> _repository;
        private readonly IGenericRepository<Intervention> _interventionRepository;
        private readonly IGenericRepository<Planification> _planificationRepository;
        private readonly IPlanificationService _planificationService;

        private readonly ISerilogService _serilogService;
        private readonly IRedisCacheService _cache;
        private readonly IAuditService _auditService;
        private readonly GmaoDbContext _dbContext;


        public EquipementService(
            IGenericRepository<Equipement> repository,
            ISerilogService serilogService,
            IRedisCacheService cache,
            IAuditService auditService,
            IGenericRepository<Intervention> interventionRepository,
            GmaoDbContext dbContext,
            IGenericRepository<Planification> planificationRepository,
            IPlanificationService planificationService
            )
        {
            _repository = repository;
            _serilogService = serilogService;
            _cache = cache;
            _auditService = auditService;
            _interventionRepository = interventionRepository;
            _dbContext = dbContext;
            _planificationRepository = planificationRepository;
            _planificationService = planificationService;
        }

        public async Task<List<EquipementDto>> GetAllEquipementDtosAsync(
    int? pageNumber = null,
    int? pageSize = null,
    int? id = null,
    string? nom = null,
    string? reference = null,
    string? localisation = null,
    DateTime? dateInstallation = null,
    string? type = null,
    string? etat = null,
    bool? isArchived = false)
        {
            string cacheKey = $"equipements_{pageNumber}_{pageSize}_{id}_{nom}_{reference}_{localisation}_{dateInstallation}_{type}_{etat}_{isArchived}_sortedByDateInstallation";
            var cached = _cache.GetData<List<EquipementDto>>(cacheKey);
            if (cached != null)
                return cached;

            EtatEquipement parsedEtat = default;
            bool isEtatValid = string.IsNullOrEmpty(etat)
                || Enum.TryParse(etat, out parsedEtat);

            Expression<Func<Equipement, bool>> filter = e =>
                (!id.HasValue || e.Id == id.Value) &&
                (string.IsNullOrEmpty(nom) || e.Nom.Contains(nom)) &&
                (!isArchived.HasValue || e.IsArchived == isArchived.Value) &&
                (string.IsNullOrEmpty(reference) || e.Reference.Contains(reference)) &&
                (string.IsNullOrEmpty(localisation) || e.Localisation.Contains(localisation)) &&
                (!dateInstallation.HasValue || e.DateInstallation == dateInstallation.Value.Date) &&
                (string.IsNullOrEmpty(type) || e.Type.Contains(type)) &&
                (string.IsNullOrEmpty(etat) || isEtatValid && e.Etat == parsedEtat);

            var equipements = await _repository.FindAllAsync(
                filter,
                includeProperties: "",
                orderBy: q => q.OrderByDescending(e => e.DateInstallation),
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            var dtos = equipements
                .Select(e => e.Adapt<EquipementDto>())
                .ToList();

            _cache.SetData(cacheKey, dtos);
            _serilogService.LogAudit("Get All Equipements",
                $"Page: {pageNumber}, Size: {pageSize}, Filters: nom:{nom}, reference:{reference}, localisation:{localisation}, dateInstallation:{dateInstallation}, type:{type}, etat:{etat}, archived:{isArchived}");

            return dtos;
        }


        public async Task<EquipementDto> GetEquipementDtoByIdAsync(int id)
        {
            string cacheKey = $"equipement_{id}";
            var cached = _cache.GetData<EquipementDto>(cacheKey);
            if (cached != null)
                return cached;

            var equipement = await _repository.GetByIdAsync(new object[] { id });
            if (equipement == null)
                throw new Exception("Équipement non trouvé.");

            var dto = equipement.Adapt<EquipementDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Get Equipement By Id", $"EquipementId : {id}");
            return dto;
        }

        public async Task<EquipementDto> CreateEquipementDtoAsync(EquipementCreateDto createDto)
        {
            var equipement = createDto.Adapt<Equipement>();

            if (equipement == null)
                throw new ArgumentNullException(nameof(equipement));

            int existingCount = await _repository.CountAsync(e =>
                e.Reference.ToLower() == createDto.Reference.ToLower());

            if (existingCount > 0)
                throw new Exception("Un équipement avec cette référence existe déjà.");

            if (equipement.DateInstallation != null && equipement.DateInstallation > DateTime.Now)
                throw new Exception("La date d'installation ne peut pas être ultérieure à la date actuelle.");

            if (equipement.DateInstallation != null)
                equipement.Etat = EtatEquipement.EnService;
            if (equipement.DateInstallation == null)
                equipement.Etat = EtatEquipement.EnAttenteInstallation;

            var created = await _repository.CreateAsync(equipement);
            var dto = created.Adapt<EquipementDto>();

            await _cache.RemoveByPrefixAsync("GMAO_equipements_");
            string cacheKey = $"equipement_{created.Id}";
            _cache.SetData(cacheKey, dto);

            await _auditService.CreateAuditAsync(
                       actionEffectuee: "Équipement créé",
                       type: ActionType.Creation,
                       entityName: "Equipement",
                       entityId: created.Id.ToString()
             );

            return dto;
        }


        public async Task<EquipementDto> UpdateEquipementDtoAsync(int id, EquipementUpdateDto equipement)
        {
            if (equipement == null)
                throw new ArgumentNullException(nameof(equipement));

            int existingCount = await _repository.CountAsync(e =>
                e.Reference.ToLower() == equipement.Reference.ToLower() && e.Id != id);

            if (existingCount > 0)
                throw new Exception("Un équipement avec cette référence existe déjà.");

            if (equipement.DateInstallation > DateTime.Now)
                throw new Exception("La date d'installation ne peut pas être ultérieure à la date actuelle.");

            var oldEquipement = await _repository.GetByIdAsync(new object[] { id });
            if (oldEquipement == null)
                throw new Exception("Équipement non trouvé.");

            var changes = new List<string>();

            if (oldEquipement.Nom != equipement.Nom && equipement.Nom != null)
            {
                changes.Add($"Nom: '{oldEquipement.Nom}' ➜ '{equipement.Nom}'");
                oldEquipement.Nom = equipement.Nom;

            }

            if (oldEquipement.Reference != equipement.Reference && equipement.Reference != null)
            {
                changes.Add($"Référence: '{oldEquipement.Reference}' ➜ '{equipement.Reference}'");
                oldEquipement.Reference = equipement.Reference;

            }

            if (oldEquipement.Localisation != equipement.Localisation && equipement.Localisation != null)
            {
                changes.Add($"Localisation: '{oldEquipement.Localisation}' ➜ '{equipement.Localisation}'");
                oldEquipement.Localisation = equipement.Localisation;

            }

            if (oldEquipement.Type != equipement.Type && equipement.Type != null)
            {
                changes.Add($"Type: '{oldEquipement.Type}' ➜ '{equipement.Type}'");
                oldEquipement.Type = equipement.Type;

            }

            if (oldEquipement.DateInstallation != equipement.DateInstallation && equipement.DateInstallation != null)
            {
                changes.Add($"Date Installation: '{oldEquipement.DateInstallation:yyyy-MM-dd HH:mm}' ➜ '{equipement.DateInstallation:yyyy-MM-dd HH:mm}'");
                oldEquipement.DateInstallation = equipement.DateInstallation;

            }

            var updated = await _repository.UpdateAsync(oldEquipement);
            if (updated == null)
                throw new Exception("Équipement non trouvé ou conflit de mise à jour.");

            var dto = updated.Adapt<EquipementDto>();

            string cacheKey = $"equipement_{id}";
            _cache.SetData(cacheKey, dto);
            await _cache.RemoveByPrefixAsync("GMAO_equipements_");

            if (changes.Count > 0)
            {
                var details = string.Join(" | ", changes);
                await _auditService.CreateAuditAsync(
                    actionEffectuee: $"Modification de l’équipement : {details}",
                    type: ActionType.Modification,
                    entityName: "Equipement",
                    entityId: updated.Id.ToString()
                );
            }

            return dto;

        }
        public async Task DesactiverEquipementAsync(int id)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var equipement = await _repository.GetByIdAsync(new object[] { id });
                if (equipement == null)
                    throw new Exception("Équipement non trouvé.");

                if (equipement.Etat == EtatEquipement.HorsService)
                    throw new Exception("Équipement déjà désactivé.");

                if (equipement.IsArchived)
                    throw new Exception("Équipement est archivé, impossible de le désactiver.");

                await ModifierEtatEquipementAsync(equipement.Id, EtatEquipement.HorsService);


                var intervention = await _interventionRepository.FindAllAsync(
                    filter: i => i.EquipementId == id
                                && !i.IsArchived
                                && (i.Statut == StatutIntervention.EnCours || i.Statut == StatutIntervention.EnAttente)
                );

                foreach (var item in intervention)
                {
                    item.Statut = StatutIntervention.Annulee;
                    item.AnnulationReason = AnnulationReason.SystemDeactivation;

                    await _interventionRepository.UpdateAsync(item);
                    string interventionCacheKey = $"intervention_{item.Id}";
                    _cache.RemoveData(interventionCacheKey);

                    await _auditService.CreateAuditAsync(
                        actionEffectuee: $"Annulation de l’intervention",
                        type: ActionType.Modification,
                        entityName: "Intervention",
                        entityId: item.Id.ToString()
                    );
                }

                await transaction.CommitAsync();

                await _cache.RemoveByPrefixAsync("GMAO_planifications_");
                await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ReactiverEquipementAsync(int id)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var equipement = await _repository.GetByIdAsync(new object[] { id });
                if (equipement == null)
                    throw new Exception("Équipement non trouvé.");

                if (equipement.Etat != EtatEquipement.HorsService)
                    throw new Exception("Équipement n'est pas désactivé, impossible de le réactiver.");

                if (equipement.IsArchived)
                    throw new Exception("Équipement est archivé, impossible de le réactiver.");
                EtatEquipement etat;

                if (equipement.DateInstallation == null)
                    etat = EtatEquipement.EnAttenteInstallation;
                else
                    etat = EtatEquipement.EnService;

                var intervention = await _interventionRepository.FindAllAsync(
                    filter: i => i.EquipementId == id
                                && !i.IsArchived
                                && i.Statut == StatutIntervention.Annulee
                                && i.AnnulationReason == AnnulationReason.SystemDeactivation,
                    includeProperties: "Planification"
                );

                foreach (var item in intervention)
                {
                    if (item.Planification != null && item.Planification.DateDebut > DateTime.Now)
                        item.Statut = StatutIntervention.EnAttente;

                    if (item.DateDebut != null && item.DateFin == null)
                        item.Statut = StatutIntervention.EnCours;

                    if (item.Type == TypeIntervention.Installation)
                        etat = EtatEquipement.EnCoursInstallation;
                    if (item.Type == TypeIntervention.Corrective)
                        etat = EtatEquipement.EnMaintenance;

                    item.AnnulationReason = AnnulationReason.None;

                    await _interventionRepository.UpdateAsync(item);
                    string interventionCacheKey = $"intervention_{item.Id}";
                    _cache.RemoveData(interventionCacheKey);

                    if (item.Statut == StatutIntervention.EnAttente)
                    {
                        await _auditService.CreateAuditAsync(
                            actionEffectuee: "Mise en attente de l’intervention",
                            type: ActionType.Modification,
                            entityName: "Intervention",
                            entityId: item.Id.ToString()
                        );
                    }
                    if (item.Statut == StatutIntervention.EnCours)
                    {
                        await _auditService.CreateAuditAsync(
                            actionEffectuee: "Démarrage de l’intervention",
                            type: ActionType.Modification,
                            entityName: "Intervention",
                            entityId: item.Id.ToString()
                        );
                    }
                }

                await ModifierEtatEquipementAsync(equipement.Id, etat);
                

              
                await transaction.CommitAsync();

               
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");
                await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteEquipementAsync(int id)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var equipement = await _repository.GetByIdAsync(new object[] { id });
                if (equipement == null)
                    throw new Exception("Équipement non trouvé.");

                if (equipement.IsArchived)
                    throw new Exception("L'équipement est déjà archivé.");

                equipement.IsArchived = true;

                await ModifierEtatEquipementAsync(equipement.Id, EtatEquipement.HorsService);

                var interventions = await _interventionRepository.FindAllAsync(
                    filter: i => i.EquipementId == id
                                && !i.IsArchived
                                && i.Statut != StatutIntervention.Terminee,
                    includeProperties: "Planification"
                );

                foreach (var inv in interventions)
                {
                    inv.IsArchived = true;
                    inv.AnnulationReason = AnnulationReason.SystemDeactivation;
                    inv.ArchiveReason = ArchiveReason.System;
                    inv.Statut = StatutIntervention.Annulee;

                    await _interventionRepository.UpdateAsync(inv);
                    string interventionCacheKey = $"intervention_{inv.Id}";
                    _cache.RemoveData(interventionCacheKey);

                    await _auditService.CreateAuditAsync(
                        actionEffectuee: $"Annulation de l’intervention",
                        type: ActionType.Modification,
                        entityName: "Intervention",
                        entityId: inv.Id.ToString()
                    );

                    await _auditService.CreateAuditAsync(
                        actionEffectuee: $"Archivage de l'intervention",
                        type: ActionType.Suppression,
                        entityName: "Intervention",
                        entityId: inv.Id.ToString()
                    );

                    var plan = inv.Planification;
                    if (plan != null)
                    {
                        plan.IsArchived = true;
                        plan.ArchiveReason = ArchiveReason.System;

                        await _planificationRepository.UpdateAsync(plan);
                        string planificationCacheKey = $"planification_{plan.Id}";
                        _cache.RemoveData(planificationCacheKey);

                        await _auditService.CreateAuditAsync(
                            actionEffectuee: $"Archivage de la planification",
                            type: ActionType.Suppression,
                            entityName: "Planification",
                            entityId: plan.Id.ToString()
                        );
                    }
                }

                await _auditService.CreateAuditAsync(
                    actionEffectuee: "Archivage de l’équipement",
                    type: ActionType.Suppression,
                    entityName: "Equipement",
                    entityId: id.ToString()
                );

                await tx.CommitAsync();

                _cache.RemoveData($"equipement_{id}");
                await _cache.RemoveByPrefixAsync("GMAO_equipements_");
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");
                await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task UnarchiveEquipementAsync(int id)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var equipement = await _repository.GetByIdAsync(new object[] { id });
                if (equipement == null)
                    throw new Exception("Équipement non trouvé.");

                if (!equipement.IsArchived)
                    throw new Exception("L'équipement n'est pas archivé.");

                equipement.IsArchived = false;

                EtatEquipement etat;
                if (equipement.DateInstallation == null)
                    etat = EtatEquipement.EnAttenteInstallation;
                else
                    etat = EtatEquipement.EnService;

                var interventions = await _interventionRepository.FindAllAsync(
                    filter: i => i.EquipementId == id
                                && i.IsArchived
                                && i.ArchiveReason == ArchiveReason.System,
                    includeProperties: "Planification"
                );

                foreach (var inv in interventions)
                {
                    inv.IsArchived = false;
                    inv.AnnulationReason = AnnulationReason.None;
                    inv.ArchiveReason = ArchiveReason.None;

                    var plan = inv.Planification;
                    if (plan != null)
                    {
                        plan.IsArchived = false;
                        plan.ArchiveReason = ArchiveReason.None;

                        await _planificationRepository.UpdateAsync(plan);
                        string planificationCacheKey = $"planification_{plan.Id}";
                        _cache.RemoveData(planificationCacheKey);
                        await _auditService.CreateAuditAsync(
                            actionEffectuee: $"Désarchivage de la planification",
                            type: ActionType.Modification,
                            entityName: "Planification",
                            entityId: plan.Id.ToString()
                        );
                    }

                    if (inv.DateDebut != null && inv.DateFin == null)
                    {
                        inv.Statut = StatutIntervention.EnCours;
                        if (inv.Type == TypeIntervention.Installation)
                            etat = EtatEquipement.EnCoursInstallation;
                        if (inv.Type == TypeIntervention.Corrective)
                            etat = EtatEquipement.EnMaintenance;
                    }

                    if (inv.Planification != null && inv.Planification.DateDebut > DateTime.Now)
                        inv.Statut = StatutIntervention.EnAttente;

                    await _interventionRepository.UpdateAsync(inv);
                    string interventionCacheKey = $"intervention_{inv.Id}";
                    _cache.RemoveData(interventionCacheKey);

                    if (inv.Statut == StatutIntervention.EnAttente)
                    {
                        await _auditService.CreateAuditAsync(
                            actionEffectuee: "Mise en attente de l’intervention",
                            type: ActionType.Modification,
                            entityName: "Intervention",
                            entityId: inv.Id.ToString()
                        );
                    }
                    if (inv.Statut == StatutIntervention.EnCours)
                    {
                        await _auditService.CreateAuditAsync(
                            actionEffectuee: "Démarrage de l’intervention",
                            type: ActionType.Modification,
                            entityName: "Intervention",
                            entityId: inv.Id.ToString()
                        );
                    }
                }
                await ModifierEtatEquipementAsync(equipement.Id, etat);
                var updatedEquip = await _repository.UpdateAsync(equipement)
                                    ?? throw new Exception("Le désarchivage de l'équipement a échoué.");

                await _auditService.CreateAuditAsync(
                    actionEffectuee: "Désarchivage de l’équipement",
                    type: ActionType.Modification,
                    entityName: "Equipement",
                    entityId: id.ToString()
                );

                await tx.CommitAsync();

                _cache.RemoveData($"equipement_{id}");
                await _cache.RemoveByPrefixAsync("GMAO_equipements_");
                await _cache.RemoveByPrefixAsync("GMAO_planifications_");
                await _cache.RemoveByPrefixAsync("GMAO_interventions_");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }



        public async Task<int> CountAsync(
            int? id = null,
            string? nom = null,
            string? reference = null,
            string? localisation = null,
            DateTime? dateInstallation = null,
            string? type = null,
            string? etat = null,
            bool? isArchived = false)
        {
            EtatEquipement parsedEtat = default;
            bool isEtatValid = string.IsNullOrEmpty(etat) || Enum.TryParse(etat, out parsedEtat);

            Expression<Func<Equipement, bool>> filter = e =>
                (!id.HasValue || e.Id == id.Value) &&
                (string.IsNullOrEmpty(nom) || e.Nom.Contains(nom)) &&
                (!isArchived.HasValue || e.IsArchived == isArchived.Value) &&
                (string.IsNullOrEmpty(reference) || e.Reference.Contains(reference)) &&
                (string.IsNullOrEmpty(localisation) || e.Localisation.Contains(localisation)) &&
                (!dateInstallation.HasValue || e.DateInstallation == dateInstallation.Value.Date) &&
                (string.IsNullOrEmpty(type) || e.Type.Contains(type)) &&
                (string.IsNullOrEmpty(etat) || isEtatValid && e.Etat == parsedEtat);

            return await _repository.CountAsync(filter);
        }


        public async Task<object> GetEquipmentStatsAsync()
        {

            int total = await CountAsync();
            int enService = await CountAsync(etat: "EnService");
            int enMaintenance = await CountAsync(etat: "EnMaintenance");
            int horsService = await CountAsync(etat: "HorsService");
            int enPanne = await CountAsync(etat: "EnPanne");
            int enAttenteInstallation = await CountAsync(etat: "EnAttenteInstallation");
            int enCoursInstallation = await CountAsync(etat: "EnCoursInstallation");


            var stats = new
            {
                totalEquipment = total,
                enService,
                enMaintenance,
                horsService,
                enPanne,
                enAttenteInstallation,
                enCoursInstallation
            };

            return stats;
        }

        public async Task<object> GetEquipmentDetailsAsync(int equipmentId)
        {

            var equipment = await _repository.GetByIdAsync(
                new object[] { equipmentId },
                "Interventions,Interventions.InterventionPieceDetachees,Interventions.InterventionPieceDetachees.PieceDetachee,Interventions.Planification"
            );

            if (equipment == null)
                throw new Exception("Équipement non trouvé.");

            double coutTotalPieceUtilise = equipment.Interventions?
                .SelectMany(i => i.InterventionPieceDetachees ?? Enumerable.Empty<InterventionPieceDetachee>())
                .Sum(ipd => (ipd.PieceDetachee?.Cout ?? 0) * ipd.Quantite) ?? 0;

            int totalPieceUtilise = equipment.Interventions?
                .SelectMany(i => i.InterventionPieceDetachees ?? Enumerable.Empty<InterventionPieceDetachee>())
                .Sum(ipd => ipd.Quantite) ?? 0;

            var nextPlanification = equipment.Interventions
                .Where(i => i.Planification != null
                         && i.Planification.DateDebut > DateTime.Now)
                .OrderBy(i => i.Planification.DateDebut)
                .Select(i => i.Planification!)
                .FirstOrDefault();



            var latestIntervention = equipment.Interventions?
                .Where(i => i.DateDebut <= DateTime.Now)
                .OrderByDescending(i => i.DateDebut)
                .FirstOrDefault();

            var details = new
            {
                CoutTotalPieceUtilise = coutTotalPieceUtilise,
                TotalPieceUtilise = totalPieceUtilise,
                NextIntervention = nextPlanification?.Adapt<PlanificationDto>(),
                LatestIntervention = latestIntervention?.Adapt<InterventionDto>()
            };

            return details;
        }

        public async Task ModifierEtatEquipementAsync(int id, EtatEquipement nouvelEtat)
        {
            var equipement = await _repository.GetByIdAsync(new object[] { id });
            if (equipement == null)
                throw new Exception("Équipement non trouvé.");


            if(equipement.Etat==nouvelEtat)
                return;

            equipement.Etat = nouvelEtat;
            var updated = await _repository.UpdateAsync(equipement)
                          ?? throw new Exception("Échec de la mise à jour de l'état de l'équipement.");
            string action;
            switch (nouvelEtat)
            {
                case EtatEquipement.EnService:
                    action = "Mise en service";
                    break;
                case EtatEquipement.EnPanne:
                    action = "Mise en panne";
                    break;
                case EtatEquipement.EnMaintenance:
                    action = "Mise en maintenance";
                    break;
                case EtatEquipement.EnCoursInstallation:
                    action = "Démarrage de l'installation";
                    break;
                case EtatEquipement.HorsService:
                    action = "Mise hors service";
                    break;
                case EtatEquipement.EnAttenteInstallation:
                    action = "Mise en attente d'installation";
                    break;
                default:
                    action = $"Changement d'état → {nouvelEtat}";
                    break;
            }

            var cacheKey = $"equipement_{id}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_equipements_");

          
            await _auditService.CreateAuditAsync(
                actionEffectuee: $"{action} de l'équipement ",
                type: ActionType.Modification,
                entityName: "Equipement",
                entityId: id.ToString()
            );


        }

    }
}

