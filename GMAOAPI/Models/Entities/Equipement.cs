using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class Equipement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; }

        [Required]
        public string Reference { get; set; }

        [Required]
        public string Localisation { get; set; }

        public DateTime? DateInstallation { get; set; }
        [Required]

        public EtatEquipement Etat { get; set; }

        [Required]
        public string Type { get; set; }

        public bool IsArchived { get; set; } = false;

        public ICollection<Intervention>? Interventions { get; set; }
    }
}
