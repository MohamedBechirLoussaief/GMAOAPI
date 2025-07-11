using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class InterventionTechnicien
    {
        [Required]

        public int InterventionId { get; set; }

        public Intervention? Intervention { get; set; }
        [Required]

        public string TechnicienId { get; set; }

        public Technicien? Technicien { get; set; }

    }
}
