using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GMAOAPI.Data;
using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Repository;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.Interfaces;
using GMAOAPI.Services.SeriLog;
using Mapster;
using Microsoft.AspNetCore.Http.HttpResults;


namespace GMAOAPI.Services.implementation
{
    public class InterventionPieceDetacheeService : IInterventionPieceDetacheeService
    {
        private readonly IGenericRepository<InterventionPieceDetachee> _repository;
        private readonly IGenericRepository<Intervention> _interventionRepository;
        private readonly IGenericRepository<PieceDetachee> _pieceDetacheeRepository;
        private readonly IPieceDetacheeService _pieceDetacheeService;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        private readonly IAuditService _auditService;
        protected readonly GmaoDbContext _dbContext;


        public InterventionPieceDetacheeService(
            IGenericRepository<InterventionPieceDetachee> repository,
            IGenericRepository<Intervention> interventionRepository,
            IGenericRepository<PieceDetachee> pieceDetacheeRepository,
            IRedisCacheService cache,
            ISerilogService serilogService,
            IPieceDetacheeService pieceDetacheeService,
            IAuditService auditService,
            GmaoDbContext dbContext
            )
        {
            _repository = repository;
            _interventionRepository = interventionRepository;
            _pieceDetacheeRepository = pieceDetacheeRepository;
            _cache = cache;
            _serilogService = serilogService;
            _pieceDetacheeService = pieceDetacheeService;
            _auditService = auditService;
            _dbContext = dbContext;
        }



        public async Task<InterventionPieceDetacheeDto> CreateDtoAsync(InterventionPieceDetacheeCreateDto createDto)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var interventionPieceDetachee = createDto.Adapt<InterventionPieceDetachee>();

                if (interventionPieceDetachee == null)
                    throw new ArgumentNullException(nameof(interventionPieceDetachee));

                var intervention = await _interventionRepository.GetByIdAsync(new object[] { interventionPieceDetachee.InterventionId },includeProperties:"Rapport");
                if (intervention == null)
                    throw new Exception("L'intervention spécifiée n'existe pas.");

                if(intervention.Rapport!=null && intervention.Rapport.IsValid)
                    throw new Exception("L'intervention a déjà un rapport valide, vous ne pouvez pas ajouter de pièces détachées.");

                if(intervention.Statut==StatutIntervention.Annulee)
                    throw new Exception("L'intervention est annulée, vous ne pouvez pas ajouter de pièces détachées.");

                var piece = await _pieceDetacheeRepository.GetByIdAsync(new object[] { interventionPieceDetachee.PieceDetacheeId });
                if (piece == null)
                    throw new Exception("La pièce détachée spécifiée n'existe pas.");

                if (interventionPieceDetachee.Quantite <= 0)
                    throw new Exception("La quantité doit être supérieure à 0.");

                if (piece.QuantiteStock < createDto.Quantite)
                    throw new Exception($"Quantite stock insuffisante pour la piece id {piece.Reference} la quantite disponible est {piece.QuantiteStock} ");

                var exist = await _repository.GetByIdAsync(
                    new object[] { interventionPieceDetachee.InterventionId, interventionPieceDetachee.PieceDetacheeId },
                    asNoTrack: true
                );

                if (exist != null)
                    throw new Exception("La pièce détachée est déjà utilisée dans cette intervention.");

                var created = await _repository.CreateAsync(interventionPieceDetachee) ?? throw new Exception("Erreur lors de l'affectation de pièce détachée.");
                await _cache.RemoveByPrefixAsync($"GMAO_intervention_{interventionPieceDetachee.InterventionId}");

                await _auditService.CreateAuditAsync(
                    actionEffectuee: $"Affectation de la pièce détachée (Réf: {piece.Reference}) , Quantité: {created.Quantite}",
                    type: ActionType.Modification,
                    entityName: "Intervention",
                    entityId: intervention.Id.ToString()
                );

                await _auditService.CreateAuditAsync(
                     actionEffectuee: $"Retrait de {created.Quantite} du stock pour l’intervention ID {created.InterventionId}",
                     type: ActionType.Modification,
                     entityName: "PieceDetachee",
                     entityId: piece.Id.ToString()
                 );

                piece.QuantiteStock -= interventionPieceDetachee.Quantite;
                await _pieceDetacheeRepository.UpdateAsync(piece);

                await transaction.CommitAsync();

                string interventionCacheKey = $"intervention_{intervention.Id}";
                string pieceCacheKey = $"pieceDetachee_{piece.Id}";
                _cache.SetData(pieceCacheKey, piece.Adapt<PieceDetacheeDto>());
                _cache.RemoveData(interventionCacheKey);
                await _cache.RemoveByPrefixAsync("GMAO_piecesDetachees_");
                if (intervention.Rapport != null)
                    _cache.RemoveData($"rapport_{intervention.Rapport.Id}");

                InterventionPieceDetacheeDto dto = created.Adapt<InterventionPieceDetacheeDto>();

                return dto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<InterventionPieceDetacheeDto> UpdateDtoAsync(int interventionId, int pieceDetacheeId, InterventionPieceDetacheeUpdateDto interventionPieceDetachee)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (interventionPieceDetachee == null)
                    throw new ArgumentNullException(nameof(interventionPieceDetachee));

                var intervention = await _interventionRepository.GetByIdAsync(new object[] { interventionId },includeProperties:"Rapport");

                if (intervention == null)
                    throw new Exception("L'intervention spécifiée n'existe pas.");

                if (intervention.Rapport!=null && intervention.Rapport.IsValid)
                    throw new Exception("L'intervention a déjà un rapport valide, vous ne pouvez pas modifier les pièces détachées.");

                var piece = await _pieceDetacheeRepository.GetByIdAsync(new object[] { pieceDetacheeId });
                if (piece == null)
                    throw new Exception("La pièce détachée spécifiée n'existe pas.");

                var oldInterventionPieceDetachee = await _repository.GetByIdAsync(new object[] { interventionId, pieceDetacheeId });
                if (oldInterventionPieceDetachee == null)
                    throw new Exception("Affectation introuvable.");

                if (interventionPieceDetachee.Quantite <= 0)
                    throw new Exception("La quantité doit être supérieure à 0.");

                if (piece.QuantiteStock + oldInterventionPieceDetachee.Quantite < interventionPieceDetachee.Quantite)
                    throw new Exception("Stock insuffisant");

                piece.QuantiteStock += oldInterventionPieceDetachee.Quantite - interventionPieceDetachee.Quantite;

                await _pieceDetacheeRepository.UpdateAsync(piece);

                int ancienneQuantite = oldInterventionPieceDetachee.Quantite;
                int nouvelleQuantite = interventionPieceDetachee.Quantite;
                int difference = nouvelleQuantite - ancienneQuantite;

                oldInterventionPieceDetachee.Quantite = interventionPieceDetachee.Quantite;

                string actionMessage = difference < 0
                ? $"Récupération de {Math.Abs(difference)} au stock depuis l’intervention ID {interventionId}"
                : $"Retrait de {difference} du stock pour l’intervention ID {interventionId}";

                await _auditService.CreateAuditAsync(
                    actionEffectuee: actionMessage,
                    type: ActionType.Modification,
                    entityName: "PieceDetachee",
                    entityId: pieceDetacheeId.ToString()
                );

                var updated = await _repository.UpdateAsync(oldInterventionPieceDetachee);
                if (updated == null)
                    throw new Exception("InterventionPieceDetachee non trouvée ou conflit de mise à jour.");

                await _auditService.CreateAuditAsync(
                   actionEffectuee: $"Modification de la quantité de la pièce détachée '{piece.Reference}' : {ancienneQuantite} ➜ {updated.Quantite}. Nouveau stock: {piece.QuantiteStock}",
                   type: ActionType.Modification,
                   entityName: "Intervention",
                   entityId: interventionId.ToString()
               );

                var dto = updated.Adapt<InterventionPieceDetacheeDto>();

                await transaction.CommitAsync();

                string cacheKey = $"pieceDetachee_{piece.Id}";
                var pieceDto = piece.Adapt<PieceDetacheeDto>();
                _cache.SetData(cacheKey, pieceDto);
                await _cache.RemoveByPrefixAsync("GMAO_piecesDetachees_");
                await _cache.RemoveByPrefixAsync($"GMAO_intervention_{interventionId}");
                if (intervention.Rapport != null)
                    _cache.RemoveData($"rapport_{intervention.Rapport.Id}");
                return dto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(object[] ids)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {               
                var interventionPieceDetachee = await _repository.GetByIdAsync(ids, "Intervention,PieceDetachee");
                if (interventionPieceDetachee == null)
                    throw new Exception("Affectation non trouvée.");

                var intervention = await _interventionRepository.GetByIdAsync(new object[] { interventionPieceDetachee.InterventionId }, includeProperties: "Rapport");

                if (intervention == null)
                    throw new Exception("L'intervention spécifiée n'existe pas.");

                if (intervention.Rapport!=null && intervention.Rapport.IsValid)
                    throw new Exception("L'intervention a déjà un rapport valide, vous ne pouvez pas supprimer les pièces détachées.");

                var piece = await _pieceDetacheeRepository.GetByIdAsync(new object[] { interventionPieceDetachee.PieceDetacheeId });
                if (piece == null)
                    throw new Exception("La pièce détachée spécifiée n'existe pas.");

                var result = await _repository.DeleteAsync(interventionPieceDetachee);
                if (!result)
                    throw new Exception("La suppression a échoué.");

                if (piece != null)
                {
                    int quantiteRecuperee = interventionPieceDetachee.Quantite;
                    piece.QuantiteStock += quantiteRecuperee;

                    await _pieceDetacheeService.UpdatePieceDetacheeAsync(
                        piece.Id,
                        piece.Adapt<PieceDetacheeUpdateDto>()
                    );

                    await _auditService.CreateAuditAsync(
                        actionEffectuee: $"Récupération de {quantiteRecuperee} au stock depuis l’intervention ID {interventionPieceDetachee.InterventionId}",
                        type: ActionType.Modification,
                        entityName: "PieceDetachee",
                        entityId: piece.Id.ToString()
                    );
                }

                await _auditService.CreateAuditAsync(
                        actionEffectuee: $"Retrait de la pièce détachée (Réf: {piece?.Reference})",
                        type: ActionType.Modification,
                        entityName: "Intervention",
                        entityId: interventionPieceDetachee.InterventionId.ToString()
                 );
                 _cache.RemoveData($"intervention_{interventionPieceDetachee.InterventionId}");
                await _cache.RemoveByPrefixAsync("GMAO_piecesDetachees_");

                if (intervention.Rapport != null)
                    _cache.RemoveData($"rapport_{intervention.Rapport.Id}");

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}