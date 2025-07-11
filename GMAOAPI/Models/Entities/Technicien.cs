using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class Technicien : Utilisateur
    {

        [Required]
        public SpecialiteTechnicien Specialite { get; set; }
        public ICollection<InterventionTechnicien>? InterventionTechniciens { get; set; }
    }
}
