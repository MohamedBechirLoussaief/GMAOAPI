using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class Planification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        [Required]
        public FrequencePlanification Frequence { get; set; }

        [Required]
        public int InterventionId { get; set; }

        public Intervention? Intervention { get; set; }

        public DateTime? ProchaineGeneration { get; set; }
        public bool IsRecurring { get; set; } = true;

        public bool IsArchived { get; set; } = false;
        public ArchiveReason ArchiveReason { get; set; } = ArchiveReason.None;

    }
}
