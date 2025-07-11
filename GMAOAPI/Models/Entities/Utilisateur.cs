using GMAOAPI.Models.Enumerations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public abstract class Utilisateur : IdentityUser
    {
       
        [Required]
        public string Nom { get; set; }

        [Required]
        public string Prenom { get; set; }

        [Required]
        public RoleUtilisateur RoleUtilisateur { get; set; }
        public bool IsArchived { get; set; } = false;

        public ICollection<Intervention>? Interventions { get; set; }
        public ICollection<Notification>? Notifications { get; set; }
        public ICollection<Rapport> RapportsCrees { get; set; } = new List<Rapport>();
        public ICollection<Rapport> RapportsValidees { get; set; } = new List<Rapport>();
        public ICollection<Audit> Audits { get; set; } = new List<Audit>();


    }
}
