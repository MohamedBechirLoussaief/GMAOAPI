using GMAOAPI.Data;
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
using System.Linq.Expressions;

namespace GMAOAPI.Services.implementation
{
    public class PieceDetacheeService : IPieceDetacheeService
    {
        private readonly IGenericRepository<PieceDetachee> _pieceDetacheeRepository;
        private readonly IGenericRepository<Fournisseur> _fournisseurRepository;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        private readonly IAuditService _auditService;

        public PieceDetacheeService(
            IGenericRepository<PieceDetachee> pieceDetacheeRepository,
            IGenericRepository<Fournisseur> fournisseurRepository,
            IRedisCacheService cache,
            ISerilogService serilogService,
            GmaoDbContext dbContext,
            IAuditService auditService
            )
        {
            _pieceDetacheeRepository = pieceDetacheeRepository;
            _fournisseurRepository = fournisseurRepository;
            _cache = cache;
            _serilogService = serilogService;
            _auditService = auditService;
        }

        public async Task<List<PieceDetacheeDto>> GetAllPieceDetacheeDtosAsync(
        int? pageNumber = null,
        int? pageSize = null,
        int? id = null,
        string? nom = null,
        string? reference = null,
        int? fournisseurId = null,
            bool? isArchived = false)
        {
            string cacheKey = $"piecesDetachees_{pageNumber}_{pageSize}_{id}_{nom}_{reference}_{fournisseurId}_{isArchived}";
            var cached = _cache.GetData<List<PieceDetacheeDto>>(cacheKey);
            if (cached != null)
                return cached;
            Expression<Func<PieceDetachee, bool>> filter = p =>
                (!id.HasValue || p.Id == id.Value) &&
                (!isArchived.HasValue || p.IsArchived == isArchived.Value) &&
                (string.IsNullOrEmpty(nom) || p.Nom.Contains(nom)) &&
                (string.IsNullOrEmpty(reference) || p.Reference.Contains(reference)) &&
                (!fournisseurId.HasValue || p.FournisseurId == fournisseurId.Value);


            var pieces = await _pieceDetacheeRepository.FindAllAsync(
                filter,
                pageNumber: pageNumber,
                pageSize: pageSize,
                includeProperties: "Fournisseur"
            );

            var dtos = pieces.Select(p => p.Adapt<PieceDetacheeDto>()).ToList();

            _cache.SetData(cacheKey, dtos);
            _serilogService.LogAudit("Get All Pieces Detachees",
                $"PageNumber: {pageNumber}, PageSize: {pageSize}, Filters: nom:{nom}, reference:{reference}, fournisseurId:{fournisseurId}, archive:{isArchived}");

            return dtos;
        }


        public async Task<PieceDetacheeDto> GetPieceDetacheeDtoByIdAsync(int id)
        {
            string cacheKey = $"pieceDetachee_{id}";
            var cached = _cache.GetData<PieceDetacheeDto>(cacheKey);
            if (cached != null)
                return cached;

            var piece = await _pieceDetacheeRepository.GetByIdAsync(new object[] { id }, "Fournisseur");
            if (piece == null)
                throw new Exception("Pièce détachée non trouvée.");

            var dto = piece.Adapt<PieceDetacheeDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Get Piece Detachee by Id", $"PieceDetacheeId: {id}");

            return dto;
        }

        public async Task<PieceDetacheeDto> CreatePieceDetacheeAsync(PieceDetacheeCreateDto createDto)
        {
            var piece = createDto.Adapt<PieceDetachee>();
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));

            int existingCount = await _pieceDetacheeRepository.CountAsync(e =>
                e.Reference.ToLower() == createDto.Reference.ToLower());

            if (existingCount > 0)
                throw new Exception("Une pièce détachée avec cette référence existe déjà.");

            if (piece.QuantiteStock < 0)
                throw new Exception("La quantité en stock ne peut pas être négative.");

            if (piece.Cout < 0)
                throw new Exception("Le coût ne peut pas être négatif.");

            var fournisseur = await _fournisseurRepository.GetByIdAsync(new object[] { piece.FournisseurId });
            if (fournisseur == null)
                throw new Exception("Le fournisseur spécifié n'existe pas.");

            piece.Fournisseur = fournisseur;

            var created = await _pieceDetacheeRepository.CreateAsync(piece);
            await _cache.RemoveByPrefixAsync("GMAO_piecesDetachees_");

            string cacheKey = $"pieceDetachee_{created.Id}";
            var dto = created.Adapt<PieceDetacheeDto>();
            _cache.SetData(cacheKey, dto);

            await _auditService.CreateAuditAsync(
                            actionEffectuee: "Création de la pièce détachée",
                            type: ActionType.Creation,
                            entityName: "PieceDetachee",
                            entityId: created.Id.ToString());
            return dto;
        }


        public async Task<PieceDetacheeDto> UpdatePieceDetacheeAsync(int id, PieceDetacheeUpdateDto pieceDetachee)
        {
            var oldPiece = await _pieceDetacheeRepository.GetByIdAsync(new object[] { id });
            if (oldPiece == null)
                throw new Exception("Pièce détachée non trouvée .");

            oldPiece.Nom = pieceDetachee.Nom;

            var updated = await _pieceDetacheeRepository.UpdateAsync(oldPiece);
            if (updated == null)
                throw new Exception("Pièce détachée non trouvée ou conflit de mise à jour.");

            string cacheKey = $"pieceDetachee_{id}";
            var dto = updated.Adapt<PieceDetacheeDto>();
            _cache.SetData(cacheKey, dto);

            await _cache.RemoveByPrefixAsync("GMAO_piecesDetachees_");

            await _auditService.CreateAuditAsync(
               actionEffectuee: $"Mise à jour de la pièce détachée  : Nom: {pieceDetachee.Nom} ➜ {dto.Nom}",
               type: ActionType.Modification,
               entityName: "PieceDetachee",
               entityId: updated.Id.ToString()
           );

            return dto;
        }

        public async Task DeletePieceDetacheeAsync(int id)
        {
            var existingPiece = await _pieceDetacheeRepository.GetByIdAsync(new object[] { id });
            if (existingPiece == null)
                throw new Exception("Pièce détachée non trouvée.");

            existingPiece.IsArchived = true;

            var result = await _pieceDetacheeRepository.UpdateAsync(existingPiece);
            if (result == null)
                throw new Exception("La suppression a échoué.");

            string cacheKey = $"pieceDetachee_{id}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_piecesDetachees_");

            await _auditService.CreateAuditAsync(
                          actionEffectuee: $"Suppression de la pièce détachée ",
                          type: ActionType.Suppression,
                          entityName: "PieceDetachee",
                          entityId: id.ToString()
                      );
        }
        public async Task UnarchivePieceDetacheeAsync(int id)
        {
            var existingPiece = await _pieceDetacheeRepository.GetByIdAsync(new object[] { id });
            if (existingPiece == null)
                throw new Exception("Pièce détachée non trouvée.");

            existingPiece.IsArchived = false;

            var result = await _pieceDetacheeRepository.UpdateAsync(existingPiece);
            if (result == null)
                throw new Exception($"Échec de la désarchivage de la piece detachee {id}.");

            string cacheKey = $"pieceDetachee_{id}";
            _cache.RemoveData(cacheKey);
            await _cache.RemoveByPrefixAsync("GMAO_piecesDetachees_");

            _serilogService.LogAudit("unarchive Piece Detachee", $"PieceDetacheeId: {id}");
        }
        public async Task<int> CountAsync(
            int? id = null,
            string? nom = "",
            string? reference = "",
            int? fournisseurId = null,
            bool? isArchived = false)
        {
            Expression<Func<PieceDetachee, bool>> filter = p =>
                (!id.HasValue || p.Id == id.Value) &&
                (!isArchived.HasValue || p.IsArchived == isArchived.Value) &&
                (string.IsNullOrEmpty(nom) || p.Nom.Contains(nom)) &&
                (string.IsNullOrEmpty(reference) || p.Reference.Contains(reference)) &&
                (!fournisseurId.HasValue || p.FournisseurId == fournisseurId.Value);

            return await _pieceDetacheeRepository.CountAsync(filter);
        }



    }
}
