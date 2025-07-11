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
using System.Linq.Expressions;
using System.Threading.Channels;

namespace GMAOAPI.Services.implementation
{
    public class FournisseurService : IFournisseurService
    {
        private readonly IGenericRepository<Fournisseur> _repository;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        private readonly GmaoDbContext _dbContext;
        private readonly IAuditService _auditService;

        public FournisseurService(
            IGenericRepository<Fournisseur> repository,
            IRedisCacheService cache,
            ISerilogService serilogService,
            GmaoDbContext dbContext,
            IAuditService auditService)
        {
            _repository = repository;
            _cache = cache;
            _serilogService = serilogService;
            _dbContext = dbContext;
            _auditService = auditService;
        }

        public async Task<List<FournisseurDto>> GetAllFournisseurDtosAsync(
    int? pageNumber = null,
    int? pageSize = null,
    int? id = null,
    string? nom = null,
    string? adresse = null,
    string? contact = null,
    bool? isArchived = false)
        {
            string cacheKey = $"fournisseurs_{pageNumber}_{pageSize}_{id}_{nom}_{adresse}_{contact}_{isArchived}";
            var cached = _cache.GetData<List<FournisseurDto>>(cacheKey);
            if (cached != null)
                return cached;

            Expression<Func<Fournisseur, bool>> filter = f =>
                (!id.HasValue || f.Id == id.Value) &&
                (!isArchived.HasValue || f.IsArchived == isArchived.Value) &&
                (string.IsNullOrEmpty(nom) || f.Nom.Contains(nom)) &&
                (string.IsNullOrEmpty(adresse) || f.Adresse.Contains(adresse)) &&
                (string.IsNullOrEmpty(contact) || f.Contact.Contains(contact));

            var fournisseurs = await _repository.FindAllAsync(filter, pageNumber: pageNumber, pageSize: pageSize);
            var dtos = fournisseurs.Select(f => f.Adapt<FournisseurDto>()).ToList();

            _cache.SetData(cacheKey, dtos);
            _serilogService.LogAudit("Get All fournisseurs", $"Page: {pageNumber}, Size: {pageSize}, Filters: nom:{nom}, adresse:{adresse}, contact:{contact}, archive:{(isArchived == true ? "Oui" : "Non")}");

            return dtos;
        }



        public async Task<FournisseurDto> GetFournisseurDtoByIdAsync(int id)
        {
            string cacheKey = $"fournisseur_{id}";
            var cached = _cache.GetData<FournisseurDto>(cacheKey);
            if (cached != null)
                return cached;

            var fournisseur = await _repository.GetByIdAsync(new object[] { id });
            if (fournisseur == null)
                throw new Exception("Fournisseur non trouvé.");

            var dto = fournisseur.Adapt<FournisseurDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Get Fournisseur by id", $"FournisseurId: {id}");

            return dto;
        }

        public async Task<FournisseurDto> CreateFournisseurAsync(FournisseurCreateDto createDto)
        {
            var fournisseur = createDto.Adapt<Fournisseur>();
            if (fournisseur == null)
                throw new ArgumentNullException(nameof(fournisseur));
            var created = await _repository.CreateAsync(fournisseur);

            await _cache.RemoveByPrefixAsync("GMAO_fournisseurs_");

            string cacheKey = $"fournisseur_{created.Id}";
            var dto = created.Adapt<FournisseurDto>();
            _cache.SetData(cacheKey, dto);

            await _auditService.CreateAuditAsync(
                actionEffectuee: "Creation d'une fournisseur",
                type: ActionType.Creation,
                entityName: "Fournisseur",
                entityId: created.Id.ToString()
);
            return dto;
        }

        public async Task<FournisseurDto> UpdateFournisseurAsync(int id, FournisseurUpdateDto fournisseur)
        {

            if (fournisseur == null)
                throw new ArgumentNullException(nameof(fournisseur));
            var oldFournisseur = await _repository.GetByIdAsync(new object[] { id });
            if (oldFournisseur == null)
                throw new Exception("Fournisseur non trouvé.");

            var changes = new List<string>();

            if (oldFournisseur.Nom != fournisseur.Nom)
                changes.Add($"Nom: '{oldFournisseur.Nom}' ➜ '{fournisseur.Nom}'");

            if (oldFournisseur.Adresse != fournisseur.Adresse)
                changes.Add($"Adresse: '{oldFournisseur.Adresse}' ➜ '{fournisseur.Adresse}'");

            if (oldFournisseur.Contact != fournisseur.Contact)
                changes.Add($"Contact: '{oldFournisseur.Contact}' ➜ '{fournisseur.Contact}'");

            oldFournisseur.Nom = fournisseur.Nom;
            oldFournisseur.Adresse = fournisseur.Adresse;
            oldFournisseur.Contact = fournisseur.Contact;


            var updated = await _repository.UpdateAsync(oldFournisseur);
            if (updated == null)
                throw new Exception("Fournisseur non trouvé ou conflit de mise à jour.");

            string cacheKey = $"fournisseur_{id}";
            var dto = updated.Adapt<FournisseurDto>();
            _cache.SetData(cacheKey, dto);
            await _cache.RemoveByPrefixAsync("GMAO_fournisseurs_");

            var description = changes.Count > 0
                    ? string.Join(" | ", changes)
                    : "Aucune modification détectée.";

            await _auditService.CreateAuditAsync(
                actionEffectuee: $"Mise à jour du fournisseur : {description}",
                type: ActionType.Modification,
                entityName: "Fournisseur",
                entityId: updated.Id.ToString()
            );
            return dto;
        }

        public async Task DeleteFournisseurAsync(int id)
        {


            var fournisseur = await _repository.GetByIdAsync(new object[] { id });
            if (fournisseur == null)
                throw new Exception("Fournisseur non trouvé.");

            fournisseur.IsArchived = true;

            var result = await _repository.UpdateAsync(fournisseur);
            if (result == null)
                throw new Exception("La suppression a échoué.");

            string cacheKey = $"fournisseur_{id}";
            _cache.RemoveData(cacheKey);

            await _cache.RemoveByPrefixAsync("GMAO_fournisseurs_");

            await _auditService.CreateAuditAsync(
                           actionEffectuee: $"Suppression de fournisseur ",
                           type: ActionType.Suppression,
                           entityName: "Fournisseur",
                           entityId: id.ToString()
                       );
        }
        public async Task UnarchiveFournisseurAsync(int id)
        {


            var fournisseur = await _repository.GetByIdAsync(new object[] { id });
            if (fournisseur == null)
                throw new Exception("Fournisseur non trouvé.");

            fournisseur.IsArchived = false;

            var result = await _repository.UpdateAsync(fournisseur);
            if (result == null)
                throw new Exception("Échec de la désarchivage de la fournisseur.");

            string cacheKey = $"fournisseur_{id}";
            _cache.RemoveData(cacheKey);

            await _cache.RemoveByPrefixAsync("GMAO_fournisseurs_");

            _serilogService.LogAudit("Unarchive fournisseur", $"FournisseurId: {id}");
        }

        public async Task<int> CountAsync(
         int? id = null,
         string? nom = "",
         string? adresse = "",
         string? contact = "",
         bool? isArchived = false)
        {
            Expression<Func<Fournisseur, bool>> filter = f =>
                (!id.HasValue || f.Id == id.Value) &&
                (!isArchived.HasValue || f.IsArchived == isArchived.Value) &&
                (string.IsNullOrEmpty(nom) || f.Nom.Contains(nom)) &&
                (string.IsNullOrEmpty(adresse) || f.Adresse.Contains(adresse)) &&
                (string.IsNullOrEmpty(contact) || f.Contact.Contains(contact));

            return await _repository.CountAsync(filter);
        }


    }
}
