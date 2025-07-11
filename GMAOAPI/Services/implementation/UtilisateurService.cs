using Azure.Core;
using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.DTOs.UpdateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Repository;
using GMAOAPI.Services.Caching;
using GMAOAPI.Services.EmailService;
using GMAOAPI.Services.Interfaces;
using GMAOAPI.Services.SeriLog;
using GMAOAPI.Services.Token;
using Mapster;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using System.Linq.Expressions;

using System.Text;
using IEmailSender = GMAOAPI.Services.EmailService.IEmailSender;

namespace GMAOAPI.Services.implementation
{
    public class UtilisateurService : IUtilisateurService
    {
        private readonly UserManager<Utilisateur> _userManager;
        private readonly SignInManager<Utilisateur> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IRedisCacheService _cache;
        private readonly ISerilogService _serilogService;
        private readonly IEmailSender _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericRepository<InterventionTechnicien> _interventionTechnicienRepository;
        private readonly IGenericRepository<Planification> _planificationRepository;
        private readonly IAuditService _auditService;



        public UtilisateurService(
            UserManager<Utilisateur> userManager,
            SignInManager<Utilisateur> signInManager,
            RoleManager<IdentityRole> roleManager,
            IRedisCacheService cache,
            IEmailSender emailService,
            ISerilogService serilogService,
            IHttpContextAccessor httpContextAccessor,
            IGenericRepository<InterventionTechnicien> interventionTechnicienRepository,
            IGenericRepository<Planification> planificationRepository,
            IAuditService auditService

        )
        {
            _emailService = emailService;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _cache = cache;
            _serilogService = serilogService;
            _httpContextAccessor = httpContextAccessor;
            _interventionTechnicienRepository = interventionTechnicienRepository;
            _planificationRepository = planificationRepository;
            _auditService = auditService;
        }

        public async Task<Utilisateur> RegisterAsync(RegisterDto registerDto)
        {
            Utilisateur user = registerDto.Role switch
            {
                RoleUtilisateur.Technicien => new Technicien
                {
                    UserName = registerDto.Username,
                    Email = registerDto.Email,
                    Nom = registerDto.Nom,
                    Prenom = registerDto.Prenom,
                    RoleUtilisateur = RoleUtilisateur.Technicien,
                    Specialite = Enum.Parse<SpecialiteTechnicien>(registerDto.Specialite.ToString())
                },
                RoleUtilisateur.Admin => new Admin
                {
                    UserName = registerDto.Username,
                    Email = registerDto.Email,
                    Nom = registerDto.Nom,
                    Prenom = registerDto.Prenom,
                    RoleUtilisateur = RoleUtilisateur.Admin
                },
                RoleUtilisateur.Responsable => new Responsable
                {
                    UserName = registerDto.Username,
                    Email = registerDto.Email,
                    Nom = registerDto.Nom,
                    Prenom = registerDto.Prenom,
                    RoleUtilisateur = RoleUtilisateur.Responsable
                },
                _ => throw new Exception("Rôle spécifié invalide.")
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                throw new Exception("Échec de la création de l’utilisateur : " + string.Join("; ", result.Errors.Select(e => e.Description)));

            string roleName = user.RoleUtilisateur.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
                throw new Exception("Rôle spécifié invalide.");

            if (!Enum.IsDefined(typeof(SpecialiteTechnicien), registerDto.Specialite))
                throw new Exception("La spécialité spécifiée est invalide.");

            var roleAssignResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleAssignResult.Succeeded)
                throw new Exception("échec de l’attribution du rôle : " + string.Join("; ", roleAssignResult.Errors.Select(e => e.Description)));

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var clientUrl = "http://localhost:5173";
            var url = $"{clientUrl}/activate?userId={user.Id}&token={encodedToken}";

            var html =
                $"<p>Bonjour {user.Prenom},</p>" +
                "<p>Un administrateur a créé votre compte GMAO.</p>" +
                "<div style=\"text-align:center;margin:30px 0;\">" +
                "  <a href=\"" + url + "\" " +
                "     style=\"display:inline-block;padding:12px 24px;" +
                "            background-color:#28a745;color:#fff;" +
                "            text-decoration:none;border-radius:4px;font-size:16px;\">" +
                "    Activer mon compte" +
                "  </a>" +
                "</div>";

            await _emailService.SendEmailAsync(user.Email, "Activation de votre compte GMAO", html);

            await _cache.RemoveByPrefixAsync("GMAO_users_");

            await _auditService.CreateAuditAsync(
                            actionEffectuee: "Creation d'une utilisateur",
                            type: ActionType.Creation,
                            entityName: "Utilisateur",
                            entityId: user.Id.ToString()
            );
            return user;
        }

        public async Task ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("Utilisateur invalide.");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                throw new Exception("Échec de la confirmation de l’email.");
        }

        public async Task<UtilisateurDto> LoginAsync(LoginDto loginDto, ITokenService tokenService)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == loginDto.Username);
            if (user == null)
                throw new Exception("Nom d’utilisateur ou mot de passe invalide.");

            if(user.IsArchived)
                throw new Exception("Ce compte est désactivé. Veuillez contacter l’administrateur.");

            //if (!await _userManager.IsEmailConfirmedAsync(user))
            //    throw new Exception("Email non confirmé. Veuillez vérifier votre boîte de réception.");

            var signInResult = await _signInManager.PasswordSignInAsync(user, loginDto.Password, false, false);
            if (!signInResult.Succeeded)
                throw new Exception("Nom d’utilisateur ou mot de passe invalide.");

            var dto = user.Adapt<UtilisateurDto>();
            dto.Token = tokenService.CreateToken(user);


            return dto;
        }

        public async Task<Utilisateur> UpdateUserAsync(UpdateUserDto updateDto)
        {
            var user = await _userManager.FindByIdAsync(updateDto.Id);
            if (user == null)
                throw new Exception("Utilisateur introuvable.");

            updateDto.Adapt(user);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception("Échec de la mise à jour de l’utilisateur : " + string.Join("; ", result.Errors.Select(e => e.Description)));

            _cache.RemoveData($"user_{user.Id}");
            await _cache.RemoveByPrefixAsync("GMAO_users_");
            _serilogService.LogAudit("Update user", user.UserName);
            return user;
        }

        public async Task DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                throw new Exception("Utilisateur introuvable.");

            user.IsArchived = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception("Échec de la suppression de l’utilisateur : " + string.Join("; ", result.Errors.Select(e => e.Description)));

            _cache.RemoveData($"user_{id}");
            await _cache.RemoveByPrefixAsync("GMAO_users_");
        }

        public async Task UnarchiveUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                throw new Exception("Utilisateur introuvable.");

            user.IsArchived = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception("Échec de la désarchivage de l’utilisateur : " + string.Join("; ", result.Errors.Select(e => e.Description)));

            _cache.RemoveData($"user_{id}");
            await _cache.RemoveByPrefixAsync("GMAO_users_");
        }

        public async Task<UtilisateurDto> GetUserDtoByIdAsync(string id)
        {
            string cacheKey = $"user_{id}";
            var cached = _cache.GetData<UtilisateurDto>(cacheKey);
            if (cached != null)
                return cached;

            var user = await _userManager.Users
               .FirstOrDefaultAsync(u => u.Id == id);
           

            if (user == null)
                throw new Exception("Utilisateur introuvable.");

            var dto = user.Adapt<UtilisateurDto>();



            _cache.SetData(cacheKey, dto);
            return dto;
        }





        public async Task<List<UtilisateurDto>> GetAllUserDtosAsync<T>(int? pageNumber = null, int? pageSize = null, string? nom = null, string? prenom = null, string? username = null, string? email = null, string? role = null, string? specialite = null, bool? isArchived = false) where T : Utilisateur
        {
            string cacheKey = $"users_{typeof(T).Name}_{pageNumber}_{pageSize}_{nom}_{prenom}_{username}_{email}_{role}_{specialite}_{isArchived}";
            var cached = _cache.GetData<List<UtilisateurDto>>(cacheKey);
            if (cached != null)
                return cached;

            var currentUser = _httpContextAccessor.HttpContext.User;
            bool isResp = currentUser.IsInRole("Responsable");

            var query = _userManager.Users.OfType<T>();
            if (isResp)
            {
                query = query.Where(u => u is Technicien);
            }

            query = query.Where(u =>
                 (string.IsNullOrEmpty(nom) || u.Nom.Contains(nom)) &&
                 (string.IsNullOrEmpty(prenom) || u.Prenom.Contains(prenom)) &&
                 (string.IsNullOrEmpty(username) || u.UserName.Contains(username)) &&
                 (string.IsNullOrEmpty(email) || u.Email.Contains(email)) &&
                 (string.IsNullOrEmpty(role) || u.RoleUtilisateur.ToString() == role) &&
                 (!isArchived.HasValue || u.IsArchived == isArchived.Value));

            if (!string.IsNullOrEmpty(role) && role.Equals("technicien", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(specialite))
            {
                query = query.Where(u => u as Technicien != null && ((Technicien)(object)u).Specialite.ToString() == specialite);
            }
            if (pageNumber.HasValue && pageSize.HasValue)
            {
                query = query
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value);
            }

            var users = await query
                .OrderBy(u => u.Nom)
                .ToListAsync();

            var dtos = users.Adapt<List<UtilisateurDto>>();

            _cache.SetData(cacheKey, dtos);
            return dtos;
        }

        public async Task<UtilisateurDto> GetUserDtoByIdAsync<T>(string id) where T : Utilisateur
        {
            string cacheKey = $"{typeof(T).Name}_user_{id}";
            var cached = _cache.GetData<UtilisateurDto>(cacheKey);
            if (cached != null)
                return cached;

            var user = await _userManager.Users.OfType<T>().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                throw new Exception($"{typeof(T).Name} introuvable.");

            var dto = user.Adapt<UtilisateurDto>();
            _cache.SetData(cacheKey, dto);
            _serilogService.LogAudit("Get user by ID", user.UserName);
            return dto;
        }

        public async Task<int> CountUsersAsync<T>(string? nom = "", string? prenom = "", string? username = "", string? email = "", string? role = "", string? specialite = "", bool? isArchived = false) where T : Utilisateur
        {
            var query = _userManager.Users.OfType<T>()
                .Where(u =>
                    (!isArchived.HasValue || u.IsArchived == isArchived.Value) &&
                    (string.IsNullOrEmpty(nom) || u.Nom.Contains(nom)) &&
                    (string.IsNullOrEmpty(prenom) || u.Prenom.Contains(prenom)) &&
                    (string.IsNullOrEmpty(username) || u.UserName.Contains(username)) &&
                    (string.IsNullOrEmpty(email) || u.Email.Contains(email)) &&
                    (string.IsNullOrEmpty(role) || u.RoleUtilisateur.ToString() == role));

            if (!string.IsNullOrEmpty(role) && role.Equals("technicien", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(specialite))
            {
                query = query.Where(u => u as Technicien != null && ((Technicien)(object)u).Specialite.ToString() == specialite);
            }

            return await query.CountAsync();
        }

        public async Task ForgotPassword(string email, string requestHeader)
        {

            var user = _userManager.FindByEmailAsync(email);
            if (user.Result == null)
                throw new Exception("Email introuvable.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user.Result);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink = $"{requestHeader}/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

            await _emailService.SendEmailAsync(
                email,
                "Reset your password",
                $"Click here to reset your password: <a href=\"{resetLink}\">Reset password</a>");



        }

        public async Task ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
                throw new Exception("Email introuvable.");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetPasswordDto.Token));

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.Password);

            if (!result.Succeeded)
                throw new Exception("Échec de la réinitialisation du mot de passe : " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public async Task<List<UtilisateurDto>> GetAvailableTechniciensAsync(
    DateTime from,
    DateTime? to = null
)
        {
            DateTime end = to ?? from;

            var busyOnInterv = await _interventionTechnicienRepository.FindAllAsync(
                filter: it =>
                    it.Intervention.Statut == StatutIntervention.EnCours
                    || it.Intervention.DateDebut < end && it.Intervention.DateFin > from,
                includeProperties: "Intervention"
            );
            var busyIds1 = busyOnInterv.Select(it => it.TechnicienId);

            var busyPlanifs = await _planificationRepository.FindAllAsync(
                filter: p => p.DateDebut < end && p.DateFin > from,
                includeProperties: "Intervention,Intervention.InterventionTechniciens"
            );
            var busyIds2 = busyPlanifs
                .SelectMany(p => p.Intervention.InterventionTechniciens.Select(it => it.TechnicienId));

            var busyTechIds = busyIds1.Union(busyIds2).ToList();

            var techUsers = _userManager.Users
                .Where(u => u.RoleUtilisateur == RoleUtilisateur.Technicien && !u.IsArchived);

            var availableUsers = await techUsers
                .Where(u => !busyTechIds.Contains(u.Id))
                .ToListAsync();

            return availableUsers.Adapt<List<UtilisateurDto>>();
        }

    }
}
