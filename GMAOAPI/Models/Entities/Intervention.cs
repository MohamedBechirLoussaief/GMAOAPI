using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class Intervention
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Description { get; set; }

        public DateTime? DateDebut { get; set; } =null;

        public DateTime? DateFin { get; set; } = null;

        [Required]
        public StatutIntervention Statut { get; set; }

        public AnnulationReason AnnulationReason { get; set; } = AnnulationReason.None;

        [Required]
        public TypeIntervention Type { get; set; }

        [Required]
        public string CreateurId { get; set; }
        public Utilisateur? Createur { get; set; }

        [Required]
        public int EquipementId { get; set; }
        public Equipement? Equipement { get; set; }
        public Planification? Planification { get; set; }

        public int? RapportId { get; set; } = null;
        public Rapport? Rapport { get; set; }
        public bool IsArchived { get; set; } = false;
        public ArchiveReason ArchiveReason { get; set; } = ArchiveReason.None;

        public ICollection<InterventionTechnicien> InterventionTechniciens { get; set; } = new List<InterventionTechnicien>();
        public ICollection<InterventionPieceDetachee> InterventionPieceDetachees { get; set; } = new List<InterventionPieceDetachee>();
    }

}
