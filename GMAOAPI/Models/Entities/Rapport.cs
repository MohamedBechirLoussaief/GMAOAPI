using GMAOAPI.Models.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class Rapport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Titre { get; set; }

        [Required]
        public string Contenu { get; set; }

        [Required]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [Required]
        public int InterventionId { get; set; }
        public Intervention Intervention { get; set; }

        [Required]
        public ResultatIntervention Resultat { get; set; }

        [Required]
        public string CreateurId { get; set; }
        public Utilisateur Createur { get; set; }
        public bool IsValid { get; set; } = false;
        public string? ValideurId { get; set; }
        public Utilisateur? Valideur { get; set; }
        public bool IsArchived { get; set; } = false;

    }
}
